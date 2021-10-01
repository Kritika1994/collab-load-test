using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {

        static ITopicClient topicClient;
        static string sbConnectionString = "Endpoint=sb://kragarw-sbns.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=vmHYogo8wBMCUxq9UgyiaUcprj1Co+1JwRGebKFCdMo=";
        static string sbTopic = "todelete";
        static string messageBody;
        static SecretClient secretClient;
        static int count = 0;
        // the client that owns the connection and can be used to create senders and receivers
        static ServiceBusClient client;
        // the processor that reads and processes messages from the subscription
        static ServiceBusProcessor processor;
        private WeatherForecast weatherForecast;
       

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        // handle any errors when receiving messages
        static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        [HttpGet]
        public WeatherForecast Get()
        {
            client = new ServiceBusClient(sbConnectionString);
            count = 0;
            // create a processor that we can use to process the messages
            processor = client.CreateProcessor(sbTopic, "todeleteSub", new ServiceBusProcessorOptions());
            processor.ProcessMessageAsync += MessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;
            return processAllMessages().GetAwaiter().GetResult();
            
        }
        private static async Task<WeatherForecast> processAllMessages()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
             count = 0;
            try
            {
                
                await processor.StartProcessingAsync();
                await Task.Delay(5000);
                await processor.StopProcessingAsync();
                stopwatch.Stop();

            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
                await processor.DisposeAsync();
                await client.DisposeAsync();
            }
            return new WeatherForecast
            {
                elapsedTime = stopwatch.ElapsedMilliseconds ,
                Summary = "Recieved messages " + count + " of times. Elapsed time is in ms. "
            };
            
        }
        private static async Task<WeatherForecast> MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            count++;
            await args.CompleteMessageAsync(args.Message);
            
            return new WeatherForecast
            {
                Summary = body+"   "+count
            };
        }

        public WeatherForecast Post([FromBody] string times) {
            messageBody = string.Empty;
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
            try
            {
                topicClient = new TopicClient(sbConnectionString, sbTopic);
                messageBody = "Smash Goals";
                topicClient = new TopicClient(sbConnectionString, sbTopic);
                weatherForecast= SendMessageAndCallKeyVault(int.Parse(times));
               /* var message = new Message(Encoding.UTF8.GetBytes(messageBody));               
                topicClient.SendAsync(message);*/

            }
            catch (Exception ex)
            {
                weatherForecast =  new WeatherForecast
                {
                    Summary = ex.Message
                };
            }
            finally
            {
              
                topicClient.CloseAsync();
            }
            return weatherForecast;
        }


        private static WeatherForecast SendMessageAndCallKeyVault(int times)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < times; i++)
            {
                var message = new Message(Encoding.UTF8.GetBytes(messageBody));
                topicClient.SendAsync(message);
            }
            CallAZKV();
            stopwatch.Stop();
            return new WeatherForecast
            {
                elapsedTime = stopwatch.ElapsedMilliseconds,
                Summary = "Sent message "+times+" of times. Elapsed time is in ms."
            };
            
        }
        private static string CallAZKV()
        {
            KeyVaultSecret secret = secretClient.GetSecret("deletewhenyouseethis");
            string secretValue = secret.Value;
            return secretValue;
        }
    }
}
