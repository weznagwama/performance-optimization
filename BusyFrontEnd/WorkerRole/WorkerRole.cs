﻿using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using ServiceBusQueueHandling;

namespace WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private const string ServiceBusConnectionStringKey = "Microsoft.ServiceBus.ConnectionString";
        private const string ServiceBusQueueNameKey = "Microsoft.ServiceBus.QueueName";

        private QueueClient _queueClient;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _completedEvent = new ManualResetEvent(false);

        public override void Run()
        {
            try
            {
                Trace.WriteLine("Starting processing of messages");
                this.RunAsync(this._cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this._completedEvent.Set();
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // Initiates the message pump and callback is invoked for each message that is received, calling close on the client will stop the pump.
            this._queueClient.OnMessageAsync(
                async (receivedMessage) =>
                {
                    try
                    {
                        // Process the message
                        Trace.WriteLine("Processing Service Bus message: " + receivedMessage.SequenceNumber.ToString());

                        //Simulate processing of message
                        Thread.SpinWait(10000);

                        await receivedMessage.CompleteAsync();
                    }
                    catch
                    {
                        receivedMessage.Abandon();
                    }
                });

            this._completedEvent.WaitOne();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Setup the reader
            var serviceBusConnectionString = CloudConfigurationManager.GetSetting(ServiceBusConnectionStringKey);
            var queueName = CloudConfigurationManager.GetSetting(ServiceBusQueueNameKey);
            var serviceBusQueueHandler = new ServiceBusQueueHandler(serviceBusConnectionString);
            this._queueClient = serviceBusQueueHandler.GetQueueClientAsync(queueName).Result;

            return base.OnStart();
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue
            this._queueClient.Close();
            this._completedEvent.Set();
            base.OnStop();
        }
    }
}
