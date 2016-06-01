using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using ClassLibrary;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;

namespace WorkerRole1
{
    public class Worker : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private CloudStorageAccount storageAccount;
        private List<string> robotFile;

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
            // Instantiate storageAccount
            storageAccount = CloudStorageAccount.Parse(
                   ConfigurationManager.AppSettings["StorageConnectionString"]);

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

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
            }
        }

        //Add Crawled Data to Table
        private void AddToTable() {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("numbers");
            table.CreateIfNotExists();
            Website result = new Website();
            TableOperation insertOperation = TableOperation.Insert(result);
            table.Execute(insertOperation);
        }

        //Adds New URLs to Queue
        private void AddToQueue()
        {

        }

        //Checks Robots.txt
        private void getRobots()
        {
            WebClient wClient = new WebClient();
            Stream data = wClient.OpenRead("http://www.cnn.com/robots.txt");
            StreamReader read = new StreamReader(data);

            robotFile = new List<string>();

            string nextLine = read.ReadLine();
            while (nextLine != null)
            {
                robotFile.Add(nextLine);
                nextLine = read.ReadLine();
            }


        }
    }
}
