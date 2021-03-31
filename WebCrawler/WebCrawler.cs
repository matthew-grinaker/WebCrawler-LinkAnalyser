using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using HtmlAgilityPack;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


namespace WebCrawler
{
    public class Crawler
    {
        public bool showUniqueLinks { get; set; }

        public bool isSubDomainFound { get; set; }

        public bool robotsTXTfound { get; set; }
        public Uri startingURL { get; set; }
        public ConcurrentBag<string> uniqueURLs { get; set; }

        public int taskCount { get; set; }

        public int isBusyProcessing { get; set; }
        public ConcurrentDictionary<string, List<string>> taskCrawlResult { get; set; }

        readonly ConcurrentQueue<Uri> taskQueue = new ConcurrentQueue<Uri>();

        private static ConcurrentBag<string> RobotRules = new ConcurrentBag<string>();

        private ConcurrentBag<Uri> brokenLinks;
        public Crawler(bool Uniqueness = false)
        {
            showUniqueLinks = Uniqueness;
            taskCount = 0;
            uniqueURLs = new ConcurrentBag<string>();
            isSubDomainFound = false;
            taskCrawlResult = new ConcurrentDictionary<string, List<string>>();
            brokenLinks = new ConcurrentBag<Uri>();
        }

        public Crawler(string[] programSettings, bool Uniqueness = false)
        {
            brokenLinks = new ConcurrentBag<Uri>();
            showUniqueLinks = Uniqueness;
            taskCount = 0;
            uniqueURLs = new ConcurrentBag<string>();
            isSubDomainFound = false;
            taskCrawlResult = new ConcurrentDictionary<string, List<string>>();

            try
            {
                if (programSettings != null)
                {
                    //set the starting URL and host domain
                    if (programSettings[0].Length > 0)
                    {
                        //setting default domain 
                        if (Uri.TryCreate(programSettings[0], UriKind.RelativeOrAbsolute, out Uri uriStartingURL))
                        {
                            startingURL = uriStartingURL;

                            if (Convert.ToInt16(programSettings[3]) == 1)
                            {
                                try
                                {
                                    GetDisallowedUrls(uriStartingURL);
                                }
                                catch
                                {
                                    Console.WriteLine("Mo Robots.txt found");
                                }
                            }

                            Console.WriteLine("Starting URL: " + uriStartingURL.ToString());
                        }
                        else
                        {
                            Console.WriteLine("Invalid start URL. URL entered: " + programSettings[0]);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid start URL. URL entered: " + programSettings[0]);
                    }

                    //set the number of simultaneous tasks to crawl the domain.
                    if (programSettings[1] != null && int.TryParse(programSettings[1], out int inTaskCount))
                    {
                        taskCount = inTaskCount;
                        Console.WriteLine("Running crawler with " + programSettings[1] + " simultaneous tasks.");
                    }

                    //set flag for crawl task to return unique or all links
                    if (Convert.ToInt32(programSettings[2]) == 1)
                    {
                        showUniqueLinks = true;
                    }

                    //search for the robots.txt file on host domain and add rules to List
                }
                else
                {
                    Console.WriteLine("Error settings defaults for program.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error initiailising program. Error: " + e.Message);
            }
        }

        public ConcurrentBag<string> StartCrawl()
        {
            if (startingURL != null)
            {
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(15);
                uniqueURLs.Add(startingURL.ToString());
                if (CrawlURL(startingURL, httpClient))
                {
                    //create new tasks to crawl URLs
                    if (taskCount > 0 && uniqueURLs.Count > 0)
                    {
                        isBusyProcessing++;
                        //List of type Task to hold dynamic tasks
                        List<Task> tasksToWait = new List<Task>();
                        for (int i = 0; i <= taskCount; i++)
                        {
                            if (i < taskCount)
                            {
                                Task crawlTask = new Task(() =>
                                {
                                    DoWork(i);
                                }, TaskCreationOptions.LongRunning);
                                tasksToWait.Add(crawlTask);
                                crawlTask.Start();
                            }
                            else
                            {
                                Task.WaitAll(tasksToWait.ToArray());
                                tasksToWait.Clear();
                            }
                        }
                        isBusyProcessing--;
                    }
                }
            }
            return uniqueURLs;

        }

        /// <summary>
        /// Funtion that processes each URL added to the task queue.
        /// </summary>
        private bool DoWork(int i)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            while (isBusyProcessing > 0)
            {
                while (taskQueue.TryDequeue(out Uri resultURL))
                {
                    if (resultURL != null)
                    {
                        isBusyProcessing++;
                        CrawlURL(resultURL, httpClient);
                        isBusyProcessing--;
                    }
                }
                if(isBusyProcessing == 1 && taskQueue.Count == 0)
                {
                    isBusyProcessing--;
                }
               
                /*
                                if (subdomainTaskQueue.TryDequeue(out Uri subDomainURL))
                                {
                                    if (subDomainURL != null)
                                    {
                                        CrawlURL(subDomainURL, httpClient);
                                    }
                                }*/
            }
           // Console.WriteLine("Task Complete - Task No: " + i.ToString());
            return true;
        }
        /// <summary>
        /// Function to crawl a URl.
        /// </summary>
        /// <returns>This function returns a bool based on the resuklt of the crawl.</returns>
        /// <param name="aURL">The string array containing the inoput settings for the program.</param> 
        /// <param name="hostDomain">The string array containing the inoput settings for the program.</param> 
        public bool CrawlURL(Uri aURL, HttpClient clientConn)
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            try
            {
                var htmlString = clientConn.GetStringAsync(aURL);
                var myHtmlString = htmlString.GetAwaiter().GetResult();
                htmlDocument.LoadHtml(myHtmlString);
                List<string> htmlloadOutput;
                if (showUniqueLinks)
                {
                    htmlloadOutput = new List<string>(htmlDocument.DocumentNode
                               .Descendants("a")
                               .Select(a => a.GetAttributeValue("href", null))
                               .Where(u => !string.IsNullOrEmpty(u))
                               .Distinct()
                               .ToList());
                }
                else
                {
                    htmlloadOutput = new List<string>(htmlDocument.DocumentNode
                                                  .Descendants("a")
                                                  .Select(a => a.GetAttributeValue("href", null))
                                                  .Where(u => !string.IsNullOrEmpty(u))
                                                  .ToList());
                }

                if (htmlloadOutput != null)
                {
                    //Add all filtered links to the concurrent dictionairy
                    if (taskCrawlResult.TryAdd(aURL.ToString(), htmlloadOutput.ToList()))
                    {
                        //filter links and return result
                        FilterURL(aURL, htmlloadOutput, startingURL.Host);
                    }
                    return true;
                }
                else
                {

                    return false;

                }

            }
            catch (Exception e)
            {
                if (!brokenLinks.Contains(aURL))
                    brokenLinks.Add(aURL);

                //Console.WriteLine("Error: " + e.Message + "Link: " + aURL);
                return false;
            }
        }


        /// <summary>
        /// Funtion to check if the URL is valid as per the rovot.txt rules. 
        /// </summary>
        /// <returns>This function returns a bool.</returns>
        /// <param name="aURL">The string URL that was found.</param> 
        private bool urlAllowed(string aURL)
        {
            foreach (string robotRule in RobotRules)
            {
                if (aURL.Contains(robotRule))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Funtion to filter the URLs that were found during a crawl. 
        /// </summary>
        /// <param name="scannedURL">The URL that was crawled.</param> 
        /// <param name="listFoundLinks">The list of URLS that were found.</param> 
        /// <param name="hostDomain">The host domain.</param> 
        private void FilterURL(Uri scannedURL, IEnumerable<string> listFoundLinks, string hostDomain)
        {
            foreach (string URL in listFoundLinks)
            {
                string finalURL = StandardiseURL(URL);

                if (finalURL != null && urlAllowed(finalURL)  && !finalURL.Contains("blog"))
                {
                    //Nested try catch to handle error and continue in for loop
                    try
                    {
                        if (URL.StartsWith("/"))
                        {
                            Uri foundURL = new Uri(scannedURL, finalURL);
                            Uri finalURLURI = new Uri(scannedURL, StandardiseURL(foundURL.LocalPath));

                            if (!uniqueURLs.Contains(finalURLURI.LocalPath))
                            {
                                lock (uniqueURLs)
                                    uniqueURLs.Add(finalURLURI.LocalPath);
                                lock (taskQueue)
                                    taskQueue.Enqueue(finalURLURI);
                            }
                        }
                        else if (Uri.TryCreate(finalURL, UriKind.Absolute, out Uri uriResult))
                        {
                           if (uriResult.Host == hostDomain && !uniqueURLs.Contains(uriResult.LocalPath))
                            {
                                Uri absoluteURL = new Uri(uriResult, StandardiseURL(uriResult.LocalPath));

                                if (!uniqueURLs.Contains(absoluteURL.LocalPath))
                                {
                                    lock (uniqueURLs)
                                        uniqueURLs.Add(absoluteURL.LocalPath);
                                    lock (taskQueue)
                                        taskQueue.Enqueue(absoluteURL);
                                }
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Error: prcoessing link: " + scannedURL);
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// standardise the input string
        /// </summary>
        /// <returns>This function returns a formatted string.</returns>
        /// <param name="preProcessedURL">The string array containing the command line arguments(start URL, No. of Tasks, flag to display distinct URLs that have been identified during a crawl).</param>
        private string StandardiseURL(string preProcessedURL)
        {
            if (preProcessedURL.Length > 1 && preProcessedURL.EndsWith("/"))
            {
                return preProcessedURL.TrimEnd('/').ToLower();
            }
            else
            {
                return preProcessedURL.ToLower();
            }
        }


        /// <summary>
        /// Prints the crawl result.
        /// </summary>
        public void PrintResult()
        {
            foreach (string key in taskCrawlResult.Keys)
            {
                Console.WriteLine("\n==============SCANNED URL==================");
                Console.WriteLine(key);
                Console.WriteLine("===========================================");
                Console.WriteLine("\n==============LINKS FOUND==================");
                if (taskCrawlResult[key] != null)
                {
                    foreach (string sList in taskCrawlResult[key])
                    {
                        Console.WriteLine(sList);
                    }
                }
                else
                {
                    Console.WriteLine("No Links Found");
                }
                Console.WriteLine("\n===========================================");
            }
        }

        /// <summary>
        /// Prints the list of links on a web page.
        /// </summary>
        /// <param name="aURL">The page URL.</param>
        public void PrintURILinks(string aURL)
        {
            Console.WriteLine("\n===========================================");
            if (taskCrawlResult[aURL] != null)
            {
                foreach (string sList in taskCrawlResult[aURL])
                {
                    Console.WriteLine(sList);
                }
            }

            Console.WriteLine("\n===========================================");
        }

        /// <summary>
        /// Prints the list of links that were crawled.
        /// </summary>
        public void PrintCrawledLinks()
        {
            Console.WriteLine("\n==============LINKS CRAWLED==================");
            foreach (string item in uniqueURLs)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine("\n==============LINKS CRAWLED==================");
        }

        /// <summary>
        /// Prints the list of links that were crawled.
        /// </summary>
        public void PrintBrokenLinks()
        {
            Console.WriteLine("\n==============Broken Links Found (" + brokenLinks.Count.ToString() + ") ==================\n");
            foreach (Uri item in brokenLinks)
            {
                Console.WriteLine(item.ToString());
            }
            Console.WriteLine("\n======================================================");
        }

        public void GetDisallowedUrls(Uri startingURL)
        {
            string robotsURL = "http://www." + startingURL.Host + "/robots.txt";

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(robotsURL);
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();

            Stream resStream = res.GetResponseStream();

            StreamReader sr = new StreamReader(resStream);
            String strText = sr.ReadToEnd();

            string[] user_agents = Regex.Split(strText, "User-agent:");
            String userAgents = "";
            foreach (string agent in user_agents)
            {
                if (agent.Trim().StartsWith("*"))
                {
                    userAgents = agent.Trim().Substring(1);
                }
            }

            String[] disallow = Regex.Split(userAgents, "Disallow:");

            foreach (String item in disallow)
            {
                if (item != "\n")
                {
                    RobotRules.Add(item.Trim().ToLower());
                    robotsTXTfound = true;
                }
            }
        }
    }
}
