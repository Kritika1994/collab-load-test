using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp4
{
    public class Program
    {
        private  const string URL = "http://kragarw-sf.eastus.cloudapp.azure.com/WeatherForecast";
        private static object contentValue = "500";
        static HttpClient client = null;
       
        static void Main(string[] args)
        {
            
            try
            {
                int requests = 0;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                client = new HttpClient();
                client.BaseAddress = new Uri(URL);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                /* Console.WriteLine("How many messages to post per call?");*/
                // contentValue = Console.ReadLine();
                int delay = 10;
                bool increase = false;
                while (true)
                {   
                    for (int i = 10; i >=0; i--)
                    {
                        CallMethod();
                        requests += 500;
                        Task.Delay(i*1).GetAwaiter().GetResult();
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        CallMethod();
                        requests += 500;
                        Task.Delay(i*1).GetAwaiter().GetResult();
                    }
                    if (stopwatch.ElapsedMilliseconds >= 1000)
                    {
                        Console.WriteLine("Requests: " + requests + " timeInSecs: " + stopwatch.ElapsedMilliseconds / 1000);
                        requests = 0;
                        stopwatch.Restart();
                    }
                }

            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                
            
            }
            finally{
                client.Dispose();
                Console.ReadLine();

            }
           
        }


        public async static void CallMethod() {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(contentValue), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(URL, content).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var dataObjects = JsonConvert.SerializeObject(response.Content.ReadAsStringAsync()); //Make sure to add a reference to System.Net.Http.Formatting.dll
                   // Console.WriteLine(dataObjects);
                }
                else
                {
                    Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }

            }
            catch(Exception ex)
            {
                Console.WriteLine("An exception happened here");
            }
           

        }
    }
}
