using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.ServiceBus;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace BackgroundService1
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class BackgroundService1 : StatelessService
    {
        public BackgroundService1(StatelessServiceContext context)
            : base(context)
        { }
        static ITopicClient recieveTopicClient;
        static ITopicClient sendTopicClient;
        static string sbConnectionString = "Endpoint=sb://kragarw-sbns.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=vmHYogo8wBMCUxq9UgyiaUcprj1Co+1JwRGebKFCdMo=";
        static string recieveTopic = "todelete";
        static string sendTopic = "sent-topic";
        static string recieveSub = "todeleteSub";
        static string sendSub = "sent-subscription";
        static ServiceBusProcessor processor;
        static ServiceBusClient client;
        static SecretClient secretClient;

      

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            var message = new Message(Encoding.UTF8.GetBytes(body));
            await sendTopicClient.SendAsync(message);
            await args.CompleteMessageAsync(args.Message);
           // return Task.CompletedTask;
        }


        static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }
        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            long iterations = 0;
            client = new ServiceBusClient(sbConnectionString);
            // create a processor that we can use to process the messages
            processor = client.CreateProcessor(recieveTopic, recieveSub, new ServiceBusProcessorOptions());
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                 }
            };
            secretClient = new SecretClient(new Uri("https://kragarw-kv.vault.azure.net/"), new DefaultAzureCredential(), options);
            recieveTopicClient = new TopicClient(sbConnectionString, recieveTopic);
            sendTopicClient = new TopicClient(sbConnectionString, sendTopic);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                    processor.ProcessMessageAsync += MessageHandler;
                    processor.ProcessErrorAsync += ErrorHandler;


                    await processor.StartProcessingAsync();
                    //await Task.Delay(5000);


                    ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);
                    //await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
               
            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
                await processor.StopProcessingAsync();
                await processor.DisposeAsync();
                await client.DisposeAsync();
                await recieveTopicClient.CloseAsync();
                await sendTopicClient.CloseAsync();
            }
        }
    }
}
