using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Xml;

namespace ClassLibrary
{
    public class WebCrawler
    {
        public WebCrawler(CloudStorageAccount storageAccount) {
            this.storageAccount = storageAccount;
        }

        private CloudStorageAccount storageAccount;

        public HashSet<string> parseSiteMaps(CloudQueueMessage msg, string workerState, HashSet<string> alreadyCrawled, List<string> disallowed)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(msg.AsString);
            using (XmlTextReader tr = new XmlTextReader(msg.AsString))
            {
                tr.Namespaces = false;
                doc.Load(tr);
            }

            var sitemaps = doc.SelectNodes("//sitemap");

            if (doc.SelectNodes("//sitemap").Count == 0)
            {
                sitemaps = doc.SelectNodes("//url");
            }
            foreach (XmlElement sitemap in sitemaps)
            {
                if (workerState != "true")
                {
                    break;
                }
                if (!alreadyCrawled.Contains(sitemap.SelectSingleNode("loc").InnerText))
                {
                    if (sitemap.SelectSingleNode("lastmod") != null)
                    {

                        if (DateTime.Parse(sitemap.SelectSingleNode("lastmod").InnerText) >= DateTime.Parse("03/01/2016"))
                        {
                            // Adds to sitemap queue if xml file type
                            if (sitemap.SelectSingleNode("loc").InnerText.Contains(".xml") && (sitemap.SelectSingleNode("loc").InnerText.Contains(".cnn.com")))
                            {
                                alreadyCrawled.Add(sitemap.SelectSingleNode("loc").InnerText);
                                EnqueueMessage("sitemapqueue", sitemap.SelectSingleNode("loc").InnerText);
                            }
                            // Adds to regular url queue if standard url
                            else if (sitemap.SelectSingleNode("loc").InnerText.Contains(".cnn.com"))
                            {
                                alreadyCrawled.Add(sitemap.SelectSingleNode("loc").InnerText);
                                EnqueueMessage("urlqueue", sitemap.SelectSingleNode("loc").InnerText);
                            }
                        }
                    }
                    else if ((sitemap.SelectSingleNode("lastmod") == null) && (sitemap.SelectSingleNode("loc").InnerText.Contains(".cnn.com") || sitemap.SelectSingleNode("loc").InnerText.Contains("bleacherreport.com")))
                    {
                        bool isAllowed = true;
                        foreach (string disallowedURL in disallowed)
                        {
                            if (sitemap.SelectSingleNode("loc").InnerText.Contains(disallowedURL) == true)
                            {
                                isAllowed = false;
                                break;
                            }
                        }
                        if (isAllowed)
                        {
                            alreadyCrawled.Add(sitemap.SelectSingleNode("loc").InnerText);
                            EnqueueMessage("urlqueue", sitemap.SelectSingleNode("loc").InnerText);
                        }
                    }
                }
            }

            return alreadyCrawled;
        }

        public HashSet<string> CrawlPage(CloudQueueMessage urlMsg, HashSet<string> alreadyCrawled, List<string> disallowed, HtmlNodeCollection allHrefNodes)
        {
            foreach (HtmlNode item in allHrefNodes)
            {
                string currentHref = item.GetAttributeValue("href", string.Empty);
                if (currentHref.StartsWith("/") && currentHref != "/")
                {
                    if (currentHref.StartsWith("//"))
                    {
                        currentHref = currentHref.Replace("//", "http://");
                    }
                    else
                    {
                        // Creates full url out of relative url
                        var url = urlMsg.AsString.Split(new string[] { ".com" }, StringSplitOptions.None);
                        currentHref = url[0] + ".com" + currentHref;
                    }
                }
                if (!currentHref.StartsWith("/") && !currentHref.StartsWith("http"))
                {
                    currentHref = "http://www." + currentHref;
                }
                if ((currentHref.Contains(".cnn.com") || currentHref.Contains("bleacherreport.com/articles")) && currentHref != "/")
                {
                    // Seperating the already crawled check in order to only check when the domain is correct 
                    if (!alreadyCrawled.Contains(currentHref))
                    {
                        bool isAllowed = true;
                        foreach (string disallowedURL in disallowed)
                        {
                            if (currentHref.Contains(disallowedURL) == true)
                            {
                                isAllowed = false;
                                break;
                            }
                        }
                        if (isAllowed)
                        {
                            EnqueueMessage("urlqueue", currentHref);
                        }
                        alreadyCrawled.Add(currentHref);
                    }
                }
            }

            return alreadyCrawled;
        }

        // Enqueues a message
        public void EnqueueMessage(string queueName, string qMessage)
        {
            CloudQueue queue = GetQueue(queueName);
            CloudQueueMessage message = new CloudQueueMessage(qMessage);
            queue.AddMessage(message);
        }

        // Create a queue
        public CloudQueue GetQueue(string queueName)
        {
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(queueName);
            queue.CreateIfNotExists();
            return queue;
        }

        // Create a table
        public CloudTable GetTable(string tableName)
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(tableName);
            return table;
        }

        // Delete all special characters
        public string DeleteCharacters(string word)
        {
            char[] arr = word.ToCharArray();
            arr = Array.FindAll<char>(arr, (c => (char.IsLetterOrDigit(c))));
            return new string(arr);
        }

    }
}

