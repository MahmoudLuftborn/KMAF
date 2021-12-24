using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WI_Share.SignalR;
using WI_Share.Models;
using WI_Share.Configurations;
using Microsoft.Extensions.Options;

namespace WI_Share.Core.Services
{
	
	public  class HostedServiceBase : IHostedService {


		private IMqttClient _client;
		private IMqttClientOptions _options;
		private readonly IHubContext<NotificationHub> _hubContext;
		private readonly string _topicName = "esp32/data";
		private readonly MQTTConfigurations _mqttConfig;
		public HostedServiceBase(IHubContext<NotificationHub> hubContext,
			IOptions<MQTTConfigurations> mqttConfig)
		{
			_hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
			_mqttConfig = mqttConfig.Value;
			InitMqtt();
		}

		private void InitMqtt()
		{
			var factory = new MqttFactory();
			_client = factory.CreateMqttClient();

			//configure options
			_options = new MqttClientOptionsBuilder()
				.WithClientId(Guid.NewGuid().ToString())
				.WithTcpServer("51.15.236.147", 1883)
				//.WithCredentials("bud", "%spencer%")
				.WithCleanSession(false)
				.Build();

			_client.UseConnectedHandler(e =>
			{
				Console.WriteLine("Connected successfully with MQTT Brokers.");

				//Subscribe to topic
				_client
				.SubscribeAsync(new TopicFilterBuilder()
				.WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.WithTopic(_topicName).Build())
				.Wait();
			});
			_client.UseDisconnectedHandler(e =>
			{
				Console.WriteLine("Disconnected from MQTT Brokers.");
			});
			_client.UseApplicationMessageReceivedHandler(e =>
			{
				if (e.ApplicationMessage.Topic == _mqttConfig.RecieveTopicName)
				{
					Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
					Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
					var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
					Console.WriteLine($"+ Payload = {message}");
					Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
					Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
					Console.WriteLine();

					_hubContext.Clients.All.SendAsync("newMessage", Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
					if (Credentials.cred.ContainsKey("cred"))
					{
						Credentials.cred["cred"] = message;
					}
					else
					{
						Credentials.cred.Add("cred", message);
					} 
				}
			});
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await _client.ConnectAsync(_options);
		}

		public async Task StopAsync(CancellationToken stoppingToken)
		{
			await _client.DisconnectAsync();
		}

		//private void RegisterOnMessageHandlerAndReceiveMessages()
		//{
		//	var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
		//	{
		//		MaxConcurrentCalls = 4,
		//		AutoComplete = false
		//	};
		//	QueueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
		//}

		//protected virtual async Task ProcessMessagesAsync(Message message, CancellationToken cancellationToken)
		//{
		//	try
		//	{
		//		_logger.LogInformation($"Starting process message");

		//		await HandleMessageAsync(message, cancellationToken);
		//		await QueueClient.CompleteAsync(message.SystemProperties.LockToken);

		//		_logger.LogInformation($"Message has been proccesed successfully.");
		//	}
		//	catch (Exception exception)
		//	{
		//		_logger.LogError(exception, "Failed to process message");
		//		await QueueClient.AbandonAsync(message.SystemProperties.LockToken);
		//	}
		//}

		//private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs eventArgs)
		//{
		//	var context = eventArgs.ExceptionReceivedContext;
		//	_logger.LogError(eventArgs.Exception, $"Service bus exception context for troubleshooting: Endpoint: {context.Endpoint} - Entity Path: {context.EntityPath} - Executing Action: {context.Action}");
		//	return Task.CompletedTask;
		//}

		//public void Dispose()
		//{
		//	_logger.LogInformation($"Disposing hosted service...");
		//}
	}
}