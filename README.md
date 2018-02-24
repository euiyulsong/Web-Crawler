# Web-Crawler

## URL Azure Instance
https://portal.azure.com/#dashboard/private/f287fad2-8a2c-425f-93bf-b85f2cc86011

## URL GitHub Repo
https://github.com/euiyulsong/Web-Crawler

In Programming Assignment 3, I built a web crawler with the c#. First, I included storage connection string in ServiceConfiguration.Cloud.cscfg, ServiceConfiguration.Local.cscfg, and ServiceDefinition.csdef.
After the inclusion of the storage connection string, I implemented worker role. I started off by implementing RunAsync using storage account. After making sure RunAsync is running properly, I implemented Crawling phase, which was most difficult. Later, I implemented my Dashboard. The result was successful.
