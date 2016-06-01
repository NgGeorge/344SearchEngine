Azure URL : http://webcrawler344.cloudapp.net/
GitHub Repro URL : https://github.com/NgGeorge/ProgrammingAssignment4

For this assignment, I created a basic search engine and web crawler centered around NBA Players and data. The application can be separated into three sections : A query suggestion engine, a web crawler, and a database query. 

Database Query :
To provide data for the NBA player stats, a sql database of player stats was hosted on Amazon RDS and managed remotely by a local PHPMyAdmin instance. The data was queried through a small PHP application hosted on an Amazon EC2 instance that took player names as input to return a JSONP string of that player's NBA stats. The input was taken from the text input box on the main index.html page.

Web Crawler :
In the backend, a web crawler was operated on an Azure Worker role, which crawled through CNN.com and BleacherReport for articles to store in an Azure Table database. It communicated with the admin web role, which contained the administrative web methods, by sending worker state messages such as "Start" or "Stop" through a "workerstate" queue. On a "Start" message, the root websites such as CNN and BleacherReport would also be sent through a sitemaps queue for the worker to parse sitemaps and robots.txt from. Any articles found in the sitemaps would be sent to a urlqueue where the worker would index and search through for more HREF links to other articles in the specified domains. Worker performance data and other various stats were also stored in the Azure Table in order to be read by the web role and displayed in the dashboard html page. 

Query Suggestion Engine + Front End :
The query suggestion engine was operated by storing a large text file of potential search terms and phrases downloaded from Wikipedia. The text file was hosted on Azure Blob, and then downloaded to the Azure Cloud App instance to be parsed and stored into a data structure. After downloading the text file, the wiki titles are then parsed by a "Build Trie" method in the web role which took each title and stored it by the character into a trie data structure for quick retrieval. The query suggestion is also kept alive by using the Uptime Robot service.

On the front end was a text input box that when typed in, it would search the trie for up to 10 potential terms starting with the inputted string. It would also simultaneously query the Web Crawler's data stored in Azure Tables for articles with keywords matching the words in the search box. Any relevant articles would be returned with the title, article URL, and date displayed with only the top 20 articles displayed at once (ranked through LINQ). Articles previously searched are also cached for 10 minutes for quick redisplay to improve user experience. Ads were also added to the index via Chitika.
