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

namespace CEXIOLABS.CommunitySoft.Notifier.Lib.Service
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	using Data;
	using Model;

	/**
	 * ManagementService root management component
	 */
	public class ManagementService
	{
		public ManagementService(
			ILogger<ManagementService> logger,
			IStorageFactory storageFactory,
			WorkerService workerService
		)
		{
			this._logger = logger;
			this._storageFactory = storageFactory;
			this._workerService = workerService;

		}

		public async Task Initialize(CancellationToken cancellationToken)
		{
			List<(Topic, Publisher)> registeredPublishers = new List<(Topic, Publisher)>();
			List<(Topic, Subscriber)> registeredSubscribers = new List<(Topic, Subscriber)>();

			try
			{
				List<Topic> topics = await this.ListTopics(cancellationToken);
				foreach (Topic topic in topics)
				{
					List<Publisher> publishers = await this.ListPublishers(cancellationToken, topic);
					List<Subscriber> subscribers = await this.ListSubscribers(cancellationToken, topic);

					foreach (Publisher publisher in publishers)
					{
						await this._workerService.RegisterPublisher(cancellationToken, topic, publisher);
						registeredPublishers.Add((topic, publisher));
					}

					foreach (Subscriber subscriber in subscribers)
					{
						await this._workerService.RegisterSubscriber(cancellationToken, topic, subscriber);
						registeredSubscribers.Add((topic, subscriber));
					}
				}
			}
			catch (Exception ex)
			{
				if (this._logger.IsEnabled(LogLevel.Error)) { this._logger.LogError("Initialize failure. ${0}", ex.Message); }
				this._logger.LogTrace(ex, "Initialize failure.");

				try
				{
					await this._workerService.UnRegisterAll(cancellationToken);
				}
				catch (Exception ex2)
				{
					if (this._logger.IsEnabled(LogLevel.Error)) { this._logger.LogError("UnRegisterAll failure. ${0}", ex2.Message); }
					this._logger.LogTrace(ex2, "UnRegisterAll failure.");
				}

				throw;
			}
		}

		public async Task<PublisherHttp> CreatePublisher(CancellationToken cancellationToken, Topic.Id topicId, PublisherHttp.Options publisherOptions)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			Topic topic = await storage.GetTopic(cancellationToken, topicId);

			PublisherHttp publisher = await storage.CreatePublisher(cancellationToken, topicId, publisherOptions);

			await this._workerService.RegisterPublisher(cancellationToken, topic, publisher);

			try
			{
				await storage.SaveChanges();
			}
			catch
			{
				await this._workerService.UnRegisterPublisher(cancellationToken, topic, publisher.Uuid);
				throw;
			}

			return publisher;
		}
		public async Task<SubscriberConsole> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberConsole.Options subscriberOptions)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			Topic topic = await storage.GetTopic(cancellationToken, topicId);

			SubscriberConsole subscriber = await storage.CreateSubscriber(cancellationToken, topicId, subscriberOptions);

			await this._workerService.RegisterSubscriber(cancellationToken, topic, subscriber);
			try
			{
				await storage.SaveChanges();
			}
			catch
			{
				await this._workerService.UnRegisterSubscriber(cancellationToken, topic, subscriber.Uuid);
				throw;
			}

			return subscriber;
		}

		public async Task<SubscriberTelegram> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberTelegram.Options subscriberOptions)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			Topic topic = await storage.GetTopic(cancellationToken, topicId);

			SubscriberTelegram subscriber = await storage.CreateSubscriber(cancellationToken, topicId, subscriberOptions);

			await this._workerService.RegisterSubscriber(cancellationToken, topic, subscriber);
			try
			{
				await storage.SaveChanges();
			}
			catch
			{
				await this._workerService.UnRegisterSubscriber(cancellationToken, topic, subscriber.Uuid);
				throw;
			}

			return subscriber;
		}

		public async Task<SubscriberWebSocket> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberWebSocket.Options subscriberOptions)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			Topic topic = await storage.GetTopic(cancellationToken, topicId);

			SubscriberWebSocket subscriber = await storage.CreateSubscriber(cancellationToken, topicId, subscriberOptions);

			await this._workerService.RegisterSubscriber(cancellationToken, topic, subscriber);
			try
			{
				await storage.SaveChanges();
			}
			catch
			{
				await this._workerService.UnRegisterSubscriber(cancellationToken, topic, subscriber.Uuid);
				throw;
			}

			return subscriber;
		}

		public async Task<SubscriberWebhook> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberWebhook.Options subscriberOptions)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			Topic topic = await storage.GetTopic(cancellationToken, topicId);

			SubscriberWebhook subscriber = await storage.CreateSubscriber(cancellationToken, topicId, subscriberOptions);

			await this._workerService.RegisterSubscriber(cancellationToken, topic, subscriber);
			try
			{
				await storage.SaveChanges();
			}
			catch
			{
				await this._workerService.UnRegisterSubscriber(cancellationToken, topic, subscriber.Uuid);
				throw;
			}

			return subscriber;
		}

		public async Task<Topic> CreateTopic(CancellationToken cancellationToken, Topic.Options topicOptions)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			Topic topic = await storage.CreateTopic(cancellationToken, topicOptions);

			await storage.SaveChanges();

			return topic;
		}

		public async Task<DateTime> DestroyPublisher(CancellationToken cancellationToken, Topic.Id topicId, Publisher.Id publisherId)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			DateTime destroyDate = DateTime.Now;

			await storage.DestroyPublisher(cancellationToken, topicId, publisherId, destroyDate);

			await storage.SaveChanges();

			return destroyDate;
		}

		public async Task<DateTime> DestroySubscriber(CancellationToken cancellationToken, Topic.Id topicId, Subscriber.Id subscriberId)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			DateTime destroyDate = DateTime.Now;

			await storage.DestroySubscriber(cancellationToken, topicId, subscriberId, destroyDate);

			await storage.SaveChanges();

			return destroyDate;
		}

		public async Task<DateTime> DestroyTopic(CancellationToken cancellationToken, Topic.Id topicId)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			DateTime destroyDate = DateTime.Now;

			await storage.DestroyTopic(cancellationToken, topicId, destroyDate);

			await storage.SaveChanges();

			return destroyDate;
		}

		public async Task<List<Publisher>> ListPublishers(CancellationToken cancellationToken, Topic.Id topicId)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			List<Publisher> publishers = (await storage.ListPublishers(cancellationToken, topicId)).ToList();

			return publishers;
		}

		public async Task<List<Subscriber>> ListSubscribers(CancellationToken cancellationToken, Topic.Id topicId)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			List<Subscriber> subscribers = (await storage.ListSubscribers(cancellationToken, topicId)).ToList();

			return subscribers;
		}

		public async Task<List<Topic>> ListTopics(CancellationToken cancellationToken)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			List<Topic> topics = (await storage.ListTopics(cancellationToken)).ToList();

			return topics;
		}

		public async Task<List<Topic>> ListTopics(CancellationToken cancellationToken, string domain)
		{
			using IStorage storage = await this._storageFactory.CreateStorage(cancellationToken);

			List<Topic> topics = (await storage.ListTopics(cancellationToken, domain)).ToList();

			return topics;
		}

		private readonly ILogger<ManagementService> _logger;
		private readonly IStorageFactory _storageFactory;
		private readonly WorkerService _workerService;
	}
}
