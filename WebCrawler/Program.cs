using HtmlAgilityPack;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

using System.Diagnostics;
using WebCrawler;
using System.Threading;


namespace WebCrawlerMain
{
    public class Program
    {

        /// <summary>
        /// The main function called at runtime.
        /// </summary>
        /// <param name="args">The string array containing the command line arguments(start URL, No. of Tasks, flag to display distinct URLs that have been identified during a crawl).</param>
        static void Main(string[] args)
        {
            ConcurrentBag<string> crawlResult;
            if (args[0] != null)
                Console.WriteLine("Starting Crawl @ " + args[0]);

            TextWriter restConsoleOutput = Console.Out;
            FileStream filestream = new FileStream("crawlResult.txt", FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);


            Crawler crawler = new Crawler(args, true);

            var timer = new Stopwatch();
            timer.Start();

         //   Console.WriteLine("");
 
            crawlResult =  crawler.StartCrawl();
            timer.Stop();
 
           // Console.WriteLine("");
            Console.WriteLine("\nTotal crawling time: " + (timer.ElapsedMilliseconds / 1000) + " s");
            //Console.WriteLine("");
            Console.WriteLine("\nTotal No. of Links Crawled: " + crawlResult.Count);
            //Console.WriteLine("");
            //Console.ReadKey();
            if(args[4] == "1" || args[4] == "3")
            { 
                crawler.PrintBrokenLinks();
            }
            Console.WriteLine("");

            if (args[4] == "2" || args[4] == "3")
            {
                crawler.PrintCrawledLinks();
            }
            Console.SetOut(restConsoleOutput);
            Console.WriteLine("Crawl completed - Check CrawlResult.txt file for output");
            Console.ReadLine();
            //Console.ReadKey();
            // PrintResult();
        }
    }
}

