using ClassLibrary;
using ClassLibrary1;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ScriptService]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        //For Crawler
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
        private int urlsCrawled = 0;

        //For Trie
        public static trie queryList = new trie();
        private string filePath = System.IO.Path.GetTempPath() + "\\titles.txt";

        //Cache
        private static object _lock = new Object();
        private static MemoryCache _cache = new MemoryCache("searchCache");

        //WebCrawler for table, queue methods
        private WebCrawler wc = new WebCrawler(storageAccount);

        // Tells the worker to start crawling
        [WebMethod]
        public void StartCrawling()
        {
            // Adding websites to Queue
            wc.EnqueueMessage("sitemapqueue", "http://www.cnn.com");
            wc.EnqueueMessage("sitemapqueue", "http://www.bleacherreport.com");

            // Start worker message 
            wc.EnqueueMessage("workeractivate", "true");
        }

        // Tells the worker to stop crawling
        [WebMethod]
        public void StopCrawling()
        {
            // Stop worker message 
            wc.EnqueueMessage("workeractivate", "false");
        }

        // Clears the site queue as well as the table of data 
        [WebMethod]
        public void ClearIndex()
        {
            // Adds current number of URLs in table to total number crawled
            urlsCrawled += TableIndexCount();

            // Sends worker a message to wait as the table is being deleted before reactivation
            wc.EnqueueMessage("workeractivate", "wait");

        }

        // Checks if the current url exists in the websites table and returns the title
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetPageTitle(string url)
        {
            CloudTable table = wc.GetTable("sitesData");
            List<string> title = new List<string>();
            // Only allow query if table exists, otherwise return no data to avoid concurrency issues incase index was cleared recently.
            if (table.Exists())
            {
                Website currentPage = new Website(url);
                var query = new TableQuery<Website>().Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "website"),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, currentPage.CalculateMD5Hash(url))
                    ));

                var result = table.ExecuteQuery(query);

                foreach(Website entity in result)
                {
                    title.Add(entity.title.Replace(",", ""));
                    title.Add(entity.date);
                    title.Add(entity.url);
                    return new JavaScriptSerializer().Serialize(title);
                }
            }
            title.Add("No Data Exists");
            return new JavaScriptSerializer().Serialize(title);
        }

        // Gets all Stats
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetAllStats()
        {
            List<string> stats = new List<string>();
            // Worker Status
            stats.Add(GetWorkerStatus());
            // Crawler Performance
            foreach (string stat in CrawlerStats())
            {
                stats.Add(stat);
            }
            // Total URLs crawled
            stats.Add(GetTotalUrlsCrawled().ToString());
            // Last Ten URLs crawled
            foreach (string site in GetLastTen())
            {
                stats.Add(site);
            }
            // Urls still in Queue
            stats.Add(URLsLeft().ToString());
            // Table Index
            stats.Add(TableIndexCount().ToString());
            // Getting the last hour of stats
            stats.Add(hourPerf());
            // Getting Last Title
            stats.Add(queryList.lastLine.Replace("_"," "));
            // Getting number of titles added
            stats.Add(queryList.count.ToString());
            // Any Errors
            foreach (string error in GetErrors())
            {
                stats.Add(error);
            }
            return new JavaScriptSerializer().Serialize(stats.Take(100));
        }

        // Gets all relevant articles to a search
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetRelevantArticles(string searchQuery)
        {
            lock (_lock)
            {
                var item = _cache.Get(DeleteCharacters(searchQuery.ToLower())) as string;
                CloudTable table = wc.GetTable("sitesData");
                List<Website> articles = new List<Website>();
                var keywords = DeleteCharacters(searchQuery.ToLower().Replace(".", " ").Replace("-", " ")).Split(null);
                // Only allow query if table exists, otherwise return no data to avoid concurrency issues incase index was cleared recently.
                if (table.Exists() && item == null)
                {
                    CacheItemPolicy policy = new CacheItemPolicy();
                    policy.AbsoluteExpiration =
                        DateTimeOffset.Now.AddMinutes(10.0);

                    foreach (string keyword in keywords)
                    {
                        var query = new TableQuery<Website>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, keyword));
                        var result = table.ExecuteQuery(query);

                        foreach (Website entity in result)
                        {
                            articles.Add(entity);
                        }
                    }
                    var results = articles.OrderByDescending(x => CountOccurences(x.title.ToLower(), keywords))
                                          .GroupBy(x => x.url)
                                          .Select(y => y.First());

                    item = new JavaScriptSerializer().Serialize(results.Take(20));
                    _cache.Set(DeleteCharacters(searchQuery.ToLower()), item, policy);
                    return item;
                }
                else if (item != null)
                {
                    return item;
                }
                return new JavaScriptSerializer().Serialize(articles);
            }
        }

        //******Trie Functions******
        //This method downloads the wiki file to a temporary folder
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string downloadWiki()
        {
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("pa2");
            if (container.Exists())
            {
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = container.GetBlockBlobReference("wiki");
                        using (var fileStream = System.IO.File.OpenWrite(filePath))
                        {
                            blob.DownloadToStream(fileStream);
                            return "Success";
                        }
                    }
                }
            }
            return "Failed";
        }

        // This method builds the trie
        [WebMethod]
        public string buildTrie()
        {
            int count = 0;
            Process currentProcess = Process.GetCurrentProcess();
            string line;
            string result = "Entirely Completed";
            System.IO.StreamReader file = new System.IO.StreamReader(filePath);
            while ((line = file.ReadLine()) != null)
            {
                queryList.AddTitle(line.ToLower());
                count++;
                if (count % 1000 == 0)
                {
                    long mem = GC.GetTotalMemory(false);
                    if (mem / 1000000 >= 950)
                    {
                        result = "Completed with count " + count + " and the last line was " + line;
                        break;
                    }
                }
            }
            file.Close();
            return result;
        }

        // This method returns a JSON object of the results
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchTrie(string title)
        {
            List<string> results = new List<string>();
            if (queryList != null)
            {
                results = queryList.SearchForPrefix(title.ToLower().Replace(" ", "_"));
            }
            else
            {
                results.Add("No Results");
            }
            return new JavaScriptSerializer().Serialize(results);
        }

        // End of Trie Functions ******

        // Gets worker status
        private string GetWorkerStatus()
        {
            CloudTable table = wc.GetTable("sitesData");
            if (table.Exists())
            {
                TableQuery<Status> rangeQuery = new TableQuery<Status>().Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "workerStatusPK"),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "workerStatusRK")
                    ));

                foreach (Status entity in table.ExecuteQuery(rangeQuery))
                {
                    if (entity.status.Contains("true"))
                    {
                        return "Active";
                    } else if (entity.status.Contains("false"))
                    {
                        return "Idle";
                    } else
                    {
                        return "Clearing";
                    }
                }
            }
            return "Clearing";
        }

        // Gets all the Errors
        private List<string> GetErrors()
        {
            CloudTable table = wc.GetTable("errorData");
            List<string> errorList = new List<string>();
            // Only allow query if table exists, otherwise return no data to avoid concurrency issues incase index was cleared recently.
            if (table.Exists())
            {
                var query = new TableQuery<Website>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "errorSite"));

                var result = table.ExecuteQuery(query);

                foreach (Website entity in result)
                {
                    errorList.Add(entity.url + "|||" + entity.error.Replace(",", ""));
                }

                return errorList;
            } else
            {
                errorList.Add("No ||| Data");
                return errorList;
            }
        }

        // Gets last 10 urls
        private List<string> GetLastTen()
        {
            CloudTable table = wc.GetTable("sitesData");
            List<string> lastTen = new List<string>();
            // Only allow query if table exists, otherwise return no data to avoid concurrency issues incase index was cleared recently.
            if (table.Exists())
            {
                Website currentPage = new Website();
                var query = new TableQuery<Website>().Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "websiteList"),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "siteList")
                    ));
                var result = table.ExecuteQuery(query);
                foreach (Website entity in result)
                {
                    var links = entity.url.Split(',');
                    foreach (string link in links)
                    {
                        lastTen.Add(link);
                    }
                }
            }
            // In order to maintain formatting for a getting stats
            for (int i = lastTen.Count; i < 10; i++)
            {
                lastTen.Add("No Data");
            }
            return lastTen;
        }

        // Gets performance for the last hour
        private string hourPerf()
        {
            CloudTable table = wc.GetTable("sitesData");
            // Only allow query if table exists, otherwise return no data to avoid concurrency issues incase index was cleared recently.
            if (table.Exists())
            {
                var query = new TableQuery<Stats>().Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "statCounter"),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "statPerf")
                    ));
                var result = table.ExecuteQuery(query);
                foreach (Stats entity in result)
                {
                    return entity.perf;
                }
            }
            return "0 0|";
        }

        // Returns the number of URLs currently crawled
        private int TableIndexCount()
        {
            CloudTable table = wc.GetTable("sitesData");
            if (table.Exists())
            {
                TableQuery<Counter> rangeQuery = new TableQuery<Counter>().Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "counter"),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "CounterRK")
                    ));

                foreach (Counter entity in table.ExecuteQuery(rangeQuery))
                {
                    if (entity.count > 0)
                    {
                        return entity.count;
                    } else
                    {
                        return 0;
                    }
                }
            }
            return 0;
        }

        private int GetTotalUrlsCrawled()
        {
            return urlsCrawled + TableIndexCount();
        }

        // Returns the current performance stats 
        private List<string> CrawlerStats()
        {
            List<string> stats = new List<string>();
            CloudTable table = wc.GetTable("sitesData");
            if (table.Exists())
            {
                TableQuery<Stats> rangeQuery = new TableQuery<Stats>().Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "statCounter"),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "stat")
                    ));
                foreach (Stats entity in table.ExecuteQuery(rangeQuery))
                {
                    stats.Add(entity.cpu.ToString());
                    stats.Add(entity.memory.ToString());
                }
                return stats;
            } else
            {
                stats.Add("No Data");
                stats.Add("No Data");
                return stats;
            }
        }

        // Returns the number of URLs still left in the pipeline to be crawled
        private int URLsLeft()
        {
            CloudQueue queue = wc.GetQueue("urlqueue");
            queue.FetchAttributes();
            if (queue.ApproximateMessageCount.HasValue)
            {
                return queue.ApproximateMessageCount.Value;
            }
            return 0;
        }

        // Delete all special characters excluding whitespace
        private string DeleteCharacters(string word)
        {
            char[] arr = word.ToCharArray();
            arr = Array.FindAll<char>(arr, (c => (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))));
            return new string(arr);
        }

        // Get the Count of Keyword Occurences in a Title 
        private int CountOccurences (string title, string[] keywords)
        {
            int count = 0;
            title = DeleteCharacters(title);
            foreach (string keyword in keywords) {
                if (title.Contains(keyword))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
