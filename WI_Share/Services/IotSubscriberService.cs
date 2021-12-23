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

namespace WI_Share.Core.Services
{
	
	public  class HostedServiceBase : IHostedService {
		private IMqttClient _client;
		private IMqttClientOptions _options;
		private readonly IHubContext<NotificationHub> _hubContext;
		public HostedServiceBase(IHubContext<NotificationHub> hubContext)
		{
			_hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
			InitMqtt();
		}

		private void InitMqtt()
		{
			var factory = new MqttFactory();
			_client = factory.CreateMqttClient();

			//configure options
			_options = new MqttClientOptionsBuilder()
				.WithClientId("SubscriberId")
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
				.WithTopic("anawaa5y").Build())
				.Wait();
			});
			_client.UseDisconnectedHandler(e =>
			{
				Console.WriteLine("Disconnected from MQTT Brokers.");
			});
			_client.UseApplicationMessageReceivedHandler(e =>
			{
				Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
				Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
				Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
				Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
				Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
				Console.WriteLine();

				_hubContext.Clients.All.SendAsync("newMessage", Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
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