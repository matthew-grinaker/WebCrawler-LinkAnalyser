# WebCrawler-LinkAnalyser

WebCrawler is a .NET console web crawler application written in C# that crawls a website (URL) and returns all the links (web pages) on the entire website/domain, 
not just on a single web page, as well as any broken links that were found on the web page.

To run the program pass the following:
1) string - The website URL.
2) int - The number of threads or similtaneous tasks to run.
3) 1 - Return unique URLs only, 0 - return all URLs.
4) 1 - Try find and use robots.txt file, 0 - don't use the robots.txt file.
5) 1 - Return broken links only, 2 - Return a list of the links crawled on the website, 3 - return both.

Example:

.\WebCrawler.exe "https://mywebsite.com" 10 1 0 3
