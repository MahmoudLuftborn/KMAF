using Microsoft.AspNetCore.Mvc;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Text;
using System.Threading;
using WI_Share.Models;

namespace WI_Share.Controllers
{
    public class HomeController : Controller
    {
        private IMqttClient _client;
        private IMqttClientOptions _options;

        public HomeController()
        {
            Console.WriteLine("Starting Publisher....");
            try
            {
                // Create a new MQTT client.
                var factory = new MqttFactory();
                _client = factory.CreateMqttClient();

                //configure options
                _options = new MqttClientOptionsBuilder()
                    .WithClientId("PublisherId")
                    .WithTcpServer("51.15.236.147", 1883)
                    //.WithCredentials("user", "kiro")
                    .WithCleanSession()
                    .Build();
                //handlers
                _client.UseConnectedHandler(e =>
                {
                    Console.WriteLine("Connected successfully with MQTT Brokers.");
                });
                _client.UseDisconnectedHandler(e =>
                {
                    Console.WriteLine("Disconnected from MQTT Brokers.");
                });
                _client.UseApplicationMessageReceivedHandler(e =>
                {
                    try
                    {
                        string topic = e.ApplicationMessage.Topic;
                        if (string.IsNullOrWhiteSpace(topic) == false)
                        {
                            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                            Console.WriteLine($"Topic: {topic}. Message Received: {payload}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message, ex);
                    }
                });


                //connect
                _client.ConnectAsync(_options).GetAwaiter().GetResult();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Index()
        {
            Console.WriteLine("Starting Publisher....");
            try
            {
                // Create a new MQTT client.
                var factory = new MqttFactory();
                _client = factory.CreateMqttClient();

                //configure options
                _options = new MqttClientOptionsBuilder()
                    .WithClientId("PublisherId")
                    .WithTcpServer("51.15.236.147", 1883)
                    //.WithCredentials("user", "kiro")
                    .WithCleanSession()
                    .Build();
                //handlers
                _client.UseConnectedHandler(e =>
                {
                    Console.WriteLine("Connected successfully with MQTT Brokers.");
                });
                _client.UseDisconnectedHandler(e =>
                {
                    Console.WriteLine("Disconnected from MQTT Brokers.");
                });
                _client.UseApplicationMessageReceivedHandler(e =>
                {
                    try
                    {
                        string topic = e.ApplicationMessage.Topic;
                        if (string.IsNullOrWhiteSpace(topic) == false)
                        {
                            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                            Console.WriteLine($"Topic: {topic}. Message Received: {payload}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message, ex);
                    }
                });


                //connect
                _client.ConnectAsync(_options).GetAwaiter().GetResult();

                Console.WriteLine("Press key to publish message.");
                Console.ReadLine();
                //simulating publish
                SimulatePublish();


                Console.WriteLine("Simulation ended! press any key to exit.");
                Console.ReadLine();

                //To keep the app running in container
                //https://stackoverflow.com/questions/38549006/docker-container-exits-immediately-even-with-console-readline-in-a-net-core-c
                //Task.Run(() => Thread.Sleep(Timeout.Infinite)).Wait();
                _client.DisconnectAsync().Wait();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return View();
        }

        [HttpGet]
        public IActionResult Setting()
        {

            return View();
        }

        [HttpPost]
        public IActionResult Setting(SettingModel settingModel)
        {

            return View();
        }

        void SimulatePublish()
        {

            var counter = 0;
            while (counter < 10)
            {
                counter++;
                var testMessage = new MqttApplicationMessageBuilder()
                    .WithTopic("test")
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                    .WithPayload($"Payload: {counter}")
                    .WithRetainFlag(false)
                    .Build();


                if (_client.IsConnected)
                {
                    Console.WriteLine($"publishing at {DateTime.UtcNow}");
                    _client.PublishAsync(testMessage);
                }
                Thread.Sleep(2000);
            }
        }
    }
}
