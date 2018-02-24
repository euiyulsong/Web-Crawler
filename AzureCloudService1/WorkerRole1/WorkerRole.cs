using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary1;
using HtmlAgilityPack;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
        public static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        public static CloudQueue linkQueue = queueClient.GetQueueReference("linkqueue");
        public static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        public static CloudQueue commandQueue = queueClient.GetQueueReference("commandqueue");
        public static CloudTable linkTable = tableClient.GetTableReference("linktable");
        public static CloudTable dashboardTable = tableClient.GetTableReference("dashboardtable");
        private string state = "Idle";
        private Queue<string> xmlQueue = new Queue<string>();
        private Queue<string> htmlQueue = new Queue<string>();
        private HashSet<string> disallowedQueue = new HashSet<string>();
        private FixedSizedQueue<string> errorQueue = new FixedSizedQueue<string>();
        private FixedSizedQueue<int> errorNumberQueue = new FixedSizedQueue<int>();
        private string error;

        private ConcurrentDictionary<string, string> urlSiteMapDictionary = new ConcurrentDictionary<string, string>();
        public string GetState()
        {
            return this.state;
        }

        public void Publish()
        {

        }

        public void Load()
        {
            this.state = "Load";
        }

        public void Crawl()
        {
            this.state = "Crawl";
        }

        public void Idle()
        {
            this.state = "Idle";
        }

        public void Crawling(string url)
        {
            if (state.Equals("Load") || state.Equals("Crawl"))
            {
                if (url.EndsWith("robots.txt"))
                {
                    GetRobot(url);
                }
                else
                {
                    Crawl();
                    //GetPage(url);
                }
            }
        }

        public void GetRobot(string url)
        {
            Uri uri = new Uri(url);

            WebRequest request = WebRequest.Create(uri.AbsoluteUri);
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            while (!reader.EndOfStream)
            {
                string temp = reader.ReadLine();
                string[] temp2 = temp.Split(' ');
                if (temp2[0].Equals("Sitemap:"))
                {
                    if (this.state.Equals("Load") || this.state.Equals("Crawl"))
                    {
                        if (uri.Authority.Equals("bleacherreport.com"))
                        {
                            if (temp2[1].Contains("nba"))
                            {
                                this.xmlQueue.Enqueue(temp2[1]);
                                Process(temp2[1]);
                            }
                        }
                        else
                        {
                            this.xmlQueue.Enqueue(temp2[1]);
                            Process(temp2[1]);
                        }
                    }
                }
                else if (temp2[0].Equals("Disallow:"))
                {
                    this.disallowedQueue.Add("http://" + uri.Authority + temp2[1]);
                }
            }
        }

        private void Process(string url)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            httpWebRequest.KeepAlive = false;
            httpWebRequest.ProtocolVersion = HttpVersion.Version10;
            httpWebRequest.ServicePoint.ConnectionLimit = 1;
            httpWebRequest.Proxy = null;
            httpWebRequest.UserAgent = "A Web Crawler";
            httpWebRequest.Timeout = 3600000;
            HtmlDocument htmlDocument = null;
            try
            {
                WebResponse webResponse = httpWebRequest.GetResponse();
                Stream stream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(stream);
                string html = "";
                try
                {
                    html = streamReader.ReadToEnd();
                }
                catch (Exception e)
                {

                }
                htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
            }
            catch (WebException e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                errorQueue.Enqueue(url + " | Error code: " + e);

            }
            if (htmlDocument != null)
            {
                if (url.Contains("-index"))
                {
                    HtmlNodeCollection sitemaps = htmlDocument.DocumentNode.SelectNodes("//sitemap");
                    if (sitemaps != null)
                    {
                        foreach (HtmlNode sitemap in sitemaps)
                        {
                            HtmlNode loc = sitemap.SelectSingleNode("//loc");
                            string link = loc.InnerText;
                            if (link.Contains("www.cnn.com"))
                            {
                                try
                                {
                                    DateTime sitemapDate = Convert.ToDateTime(sitemap.LastChild.InnerText);
                                    int difference = ((DateTime.Now.Year - sitemapDate.Year) * 12) + DateTime.Now.Month - sitemapDate.Month;
                                    if (difference <= 2)
                                    {
                                        Process(link);
                                    }
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine(e);
                                    error = e.ToString();
                                    //Publish();
                                };
                            }
                            else if (link.Contains("bleacherreport.com"))
                            {
                                if (link.Contains("nba"))
                                {
                                    Process(link);
                                }
                            }
                        }

                    }
                    // If it is not index
                    else
                    {
                        HtmlNodeCollection htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes("//url");
                        if (htmlNodeCollection != null)
                        {
                            foreach (HtmlNode link in htmlNodeCollection)
                            {
                                HtmlNode loc = link.SelectSingleNode("//loc");
                                if (!urlSiteMapDictionary.ContainsKey(loc.InnerText))
                                {
                                    urlSiteMapDictionary.TryAdd(loc.InnerText, "");
                                    linkQueue.AddMessage(new CloudQueueMessage(loc.InnerText));
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
                linkQueue.CreateIfNotExists();
                commandQueue.CreateIfNotExists();

                CloudQueueMessage commandQueueMessage = commandQueue.GetMessage();
                if (commandQueueMessage != null)
                {
                    if (commandQueueMessage.AsString == "Load")
                    {
                        Load();
                    }
                    else if (commandQueueMessage.AsString == "Crawl")
                    {
                        Crawl();
                    }
                    else if (commandQueueMessage.AsString == "Idle")
                    {
                        Idle();
                        Publish();
                    }
                    commandQueue.DeleteMessage(commandQueueMessage);
                }
                if (this.state.Equals("Load") || this.state.Equals("Crawl"))
                {
                    CloudQueueMessage link = linkQueue.GetMessage();
                    if (link != null)
                    {
                        Crawling(link.AsString);
                        //crawler.Crawling(link.AsString);
                        linkQueue.DeleteMessage(link);

                        //crawler.Publish();
                    }
                }

            }
        }
    }
}
