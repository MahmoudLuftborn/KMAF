using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WI_Share.Configurations;
using WI_Share.DB;
using WI_Share.Models;

namespace WI_Share.Controllers
{
	public class HomeController : Controller
	{
		private IMqttClient _client;
		private IMqttClientOptions _options;
		private readonly MQTTConfigurations _mqttConfig;

		public HomeController(IOptions<MQTTConfigurations> mqttConfig)
		{
			_mqttConfig = mqttConfig.Value;

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
			var settingsModel = new SettingModel();

			if (Credentials.cred.ContainsKey("cred"))
			{
				settingsModel = SplitData(Credentials.cred["cred"]);
			}

			return View(settingsModel);
		}


		[HttpGet]
		public IActionResult Setting()
		{
			var settingsModel = new SettingModel();

			if (Credentials.cred.ContainsKey("cred"))
			{
				settingsModel = SplitData(Credentials.cred["cred"]);
			}

			return View("Setting", settingsModel);
		}

		[HttpPost]
		public async Task<IActionResult> Setting(SettingModel settingModel)
		{
			string data = settingModel.ssid + ';' + settingModel.password + ';' + settingModel.ipAdress;
			await PublishToQueue(_mqttConfig.SendTopicName, data);
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
			if (str.Length > 0)
			{
				string[] tokens;

				if (str.Contains(':'))
				{
					var tokensWithoutHeader = str.Split(':');

					if (tokensWithoutHeader.Length <= 1)
					{
						return new SettingModel();
					}
					tokens = tokensWithoutHeader[1].Split(';');
				}
				else
				{
					tokens = str.Split(';');
				}

				if (tokens.Length <= 0)
				{
					return new SettingModel();
				}

				return new SettingModel
				{
					ssid = tokens[0],
					password = tokens[1],
					ipAdress = tokens[2]
				};
			}
			else
			{
				return new SettingModel();
			}
		}

		[HttpGet]
		public IActionResult Reading()
		{
			var readingModel = new ReadingModel()
			{
				Data = new double[0],
				Labels = new string[0]
			};

			return View(readingModel);
		}


		[HttpGet]
		public IActionResult GetLatest()
		{
			var db = new DBCalls();
			var latest = db.GetLatest().ToList();

			return new JsonResult(latest);
		}
	}
}
