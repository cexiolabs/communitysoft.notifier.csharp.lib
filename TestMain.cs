//
// Copyright 2021 CEXIOLABS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
namespace CEXIOLABS.CommunitySoft.Notifier.Lib
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	using Data;
	using MessageBus;
	using Model;
	using Service;

	public class Program
	{
		public static void Main(string[] args)
		{
			Test().Wait();
		}

		private static async Task Test()
		{
			ILoggerFactory loggerFactory = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();

			IStorageFactory storageFactory = new InMemoryStorageFactory();
			IMessageBus messageBus = new InMemoryMessageBus();
			using WorkerService workerService = new WorkerService(loggerFactory, loggerFactory.CreateLogger<WorkerService>(), messageBus);
			ManagementService managementService = new ManagementService(loggerFactory.CreateLogger<ManagementService>(), storageFactory, workerService);


			Topic testTopic = await managementService.CreateTopic(
				CancellationToken.None,
				new Topic.OptionsBuilder()
					.WithName("MyTestTopic")
					.WithDescription("Just for test")
					.WithMediaTypeTextPlain()
					.Build()
			);

			// Register HTTP publisher. This will allow us to publish messages via
			//     curl --verbose --request POST --header 'Content-Type: text/plain' --data 'Hello, World!!!' http://127.0.0.1:8080/callback/crypto/deposit
			//
			await managementService.CreatePublisher(
				CancellationToken.None,
				testTopic,
				new PublisherHttp.OptionsBuilder()
					.WithPathSegments("callback", "crypto", "deposit")
					.Build()
			);

			// Register Console subscriber. This subscriber will print each message into console.
			await managementService.CreateSubscriber(
				CancellationToken.None,
				testTopic,
				new SubscriberConsole.OptionsBuilder()
					.WithTextColor(ConsoleColor.Yellow)
					.Build()
			);

			// // Register Telegram subscriber. This subscriber will send each message to Telegram user(s).
			// await managementService.CreateSubscriber(
			// 	CancellationToken.None,
			// 	testTopic, 
			// 	new SubscriberTelegram.OptionsBuilder()
			// 		.WithXXXXX()
			// 		.WithYYYYY()
			// 		.WithZZZZZ()
			// 		.Build()
			// );

			// Register WebSocket subscriber. This subscriber will send each message to clients connected as WebSocket client.
			// Just connect via `wscat --connect ws://127.0.0.1:8080/ws` and listen for messages
			await managementService.CreateSubscriber(
				CancellationToken.None,
				testTopic,
				new SubscriberWebSocket.OptionsBuilder()
					.WithPathSegments("ws")
					.Build()
			);

			// // Register Webhook subscriber. This subscriber will send each message as HTTP call to destination endpoint.
			// await managementService.CreateSubscriber(
			// 	CancellationToken.None,
			// 	testTopic,
			// 	new SubscriberWebhook.OptionsBuilder()
			// 		.Build()
			// );

			// Right now we are abble to push notification into HTTP publisher and receive it via WebSocket
			await Task.Delay(TimeSpan.FromMinutes(30));
		}
	}
}