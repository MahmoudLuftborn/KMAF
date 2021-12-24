using Microsoft.AspNetCore.Mvc;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WI_Share.Models;

namespace WI_Share.Controllers
{
    public class HomeController : Controller
    {
        private IMqttClient _client;
        private IMqttClientOptions _options;
        //private IDictionary _data = new Dictionary<string, string>();

        public HomeController()
        {
            //_data.Add("credentials", "");

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

            return View(SplitData(Credentials.cred["cred"]));
        }

        [HttpGet]
        public IActionResult Setting()
        {
            return View("Setting", SplitData(Credentials.cred["cred"].ToString()));
        }

        [HttpPost]
        public async Task<IActionResult> Setting(SettingModel settingModel)
        {
            char enableReading = settingModel.enableReading ? '1' : '0';
            string data = settingModel.ssid + ';' + settingModel.password + ';' + settingModel.ipAdress + ';' + enableReading;
            await PublishToQueue("anawaa5y", data);
            return RedirectToAction("Index", settingModel);
        }

        public async Task<IActionResult> PublishToQueue(string queueName, string data)
        {
            var testMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(queueName)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                    .WithPayload($"Payload: {data}")
                    .WithRetainFlag(false)
                    .Build();


            if (_client.IsConnected)
            {
                Console.WriteLine($"publishing at {DateTime.UtcNow}");
                await _client.PublishAsync(testMessage);
                Credentials.cred["cred"] = data;
            }
            return null;
            //Thread.Sleep(2000);
        }

        private SettingModel SplitData(string str)
        {
            string[] tokens = str.Split(';');
            return new SettingModel { ssid = tokens[0], 
                password = tokens[1], 
                ipAdress = tokens[2], 
                enableReading = tokens[3] == "1" ? true : false };
        }
    }
}
