using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace BackgroundService3
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class BackgroundService3 : StatelessService
    {
        static ITopicClient recieveTopicClient;
        static string sbConnectionString = "Endpoint=sb://kragarw-sbns.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=vmHYogo8wBMCUxq9UgyiaUcprj1Co+1JwRGebKFCdMo=";
        static string recieveTopic = "confirmed-topic";
        static string recieveSub = "confirmed-sub";
        static ServiceBusProcessor processor;
        static ServiceBusClient client;

        public BackgroundService3(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        private static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            await args.CompleteMessageAsync(args.Message);
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

            recieveTopicClient = new TopicClient(sbConnectionString, recieveTopic);


            try
            {
                
                    cancellationToken.ThrowIfCancellationRequested();
                    processor.ProcessMessageAsync += MessageHandler;
                    processor.ProcessErrorAsync += ErrorHandler;


                    await processor.StartProcessingAsync();



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

            }
        }
    }
}
