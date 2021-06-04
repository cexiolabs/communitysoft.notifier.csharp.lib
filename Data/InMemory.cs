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

namespace CEXIOLABS.CommunitySoft.Notifier.Lib.Data
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;



	using CEXIOLABS.CommunitySoft.Notifier.Lib.Model;

	public class InMemoryStorageFactory : IStorageFactory
	{
		public InMemoryStorageFactory()
		{
			this._entries = new List<TopicEntry>();
		}

		public Task<IStorage> CreateStorage(CancellationToken cancellationToken)
		{
			return Task.FromResult<IStorage>(new InMemoryStorage(this));
		}

		private class InMemoryStorage : IStorage
		{
			public InMemoryStorage(InMemoryStorageFactory owner)
			{
				this._owner = owner;
				this._addedTopics = new List<Topic>();
				this._addedPublishers = new List<(Topic.Id, Publisher)>();
				this._addedSubscribers = new List<(Topic.Id, Subscriber)>();
			}

			public Task<PublisherHttp> CreatePublisher(CancellationToken cancellationToken, Topic.Id topicId, PublisherHttp.Options publisherOptions)
			{
				Guid publisherId = Guid.NewGuid();
				PublisherHttp publisher = new PublisherHttp(publisherId, publisherOptions);

				this._addedPublishers.Add((topicId, publisher));

				return Task.FromResult(publisher);
			}

			public Task<SubscriberConsole> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberConsole.Options subscriberOptions)
			{
				Guid subscriberId = Guid.NewGuid();
				SubscriberConsole subscriber = new SubscriberConsole(subscriberId, subscriberOptions);

				this._addedSubscribers.Add((topicId, subscriber));

				return Task.FromResult(subscriber);
			}

			public Task<SubscriberTelegram> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberTelegram.Options subscriberOptions)
			{
				Guid subscriberId = Guid.NewGuid();
				SubscriberTelegram subscriber = new SubscriberTelegram(subscriberId, subscriberOptions);

				this._addedSubscribers.Add((topicId, subscriber));

				return Task.FromResult(subscriber);
			}

			public Task<SubscriberWebSocket> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberWebSocket.Options subscriberOptions)
			{
				Guid subscriberId = Guid.NewGuid();
				SubscriberWebSocket subscriber = new SubscriberWebSocket(subscriberId, subscriberOptions);

				this._addedSubscribers.Add((topicId, subscriber));

				return Task.FromResult(subscriber);
			}

			public Task<SubscriberWebhook> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberWebhook.Options subscriberOptions)
			{
				Guid subscriberId = Guid.NewGuid();
				SubscriberWebhook subscriber = new SubscriberWebhook(subscriberId, subscriberOptions);

				this._addedSubscribers.Add((topicId, subscriber));

				return Task.FromResult(subscriber);
			}

			public Task<Topic> CreateTopic(CancellationToken cancellationToken, Topic.Options topicOptions)
			{
				Guid subscriberId = Guid.NewGuid();
				Topic topic = new Topic(subscriberId, topicOptions, DateTime.Now, null);

				this._addedTopics.Add(topic);

				return Task.FromResult(topic);
			}

			public Task DestroyPublisher(CancellationToken cancellationToken, Topic.Id topicId, Publisher.Id publisherId, DateTime destroyDate)
			{
				throw new NotImplementedException();
			}

			public Task DestroySubscriber(CancellationToken cancellationToken, Topic.Id topicId, Subscriber.Id subscriberId, DateTime destroyDate)
			{
				throw new NotImplementedException();
			}

			public Task DestroyTopic(CancellationToken cancellationToken, Topic.Id topicId, DateTime destroyDate)
			{
				throw new NotImplementedException();
			}

			public void Dispose()
			{
				// NOP
			}

			public Task<Topic> GetTopic(CancellationToken cancellationToken, Topic.Id topicId)
			{
				Topic? topic = this._addedTopics.SingleOrDefault(w => w.Domain == null);
				if (topic == null)
				{
					topic = this._owner._entries.Single(w => w.Topic.Uuid == topicId.Uuid).Topic;
				}

				return Task.FromResult(topic);
			}

			public Task<IEnumerable<Publisher>> ListPublishers(CancellationToken cancellationToken, Topic.Id topicId)
			{
				List<Publisher> publishers = new List<Publisher>();

				TopicEntry? topicEntry = this._owner._entries.SingleOrDefault(topicEntry => topicEntry.Topic.Uuid == topicId.Uuid);
				if (topicEntry != null)
				{
					publishers.AddRange(topicEntry.Publishers);
				}

				publishers.AddRange(this._addedPublishers.Where(w => w.Item1.Uuid == topicId.Uuid).Select(s => s.Item2));

				return Task.FromResult<IEnumerable<Publisher>>(publishers);
			}

			public Task<IEnumerable<Subscriber>> ListSubscribers(CancellationToken cancellationToken, Topic.Id topicId)
			{
				List<Subscriber> subscribers = new List<Subscriber>();

				TopicEntry? topicEntry = this._owner._entries.SingleOrDefault(topicEntry => topicEntry.Topic.Uuid == topicId.Uuid);
				if (topicEntry != null)
				{
					subscribers.AddRange(topicEntry.Subscribers);
				}

				subscribers.AddRange(this._addedSubscribers.Where(w => w.Item1.Uuid == topicId.Uuid).Select(s => s.Item2));

				return Task.FromResult<IEnumerable<Subscriber>>(subscribers);
			}

			public Task<IEnumerable<Topic>> ListTopics(CancellationToken cancellationToken)
			{
				List<Topic> topics = this._owner._entries.Where(w => w.Topic.Domain == null).Select(s => s.Topic).ToList();

				topics.AddRange(this._addedTopics.Where(w => w.Domain == null));

				return Task.FromResult<IEnumerable<Topic>>(topics);
			}

			public Task<IEnumerable<Topic>> ListTopics(CancellationToken cancellationToken, string domain)
			{
				List<Topic> topics = this._owner._entries.Where(w => w.Topic.Domain == domain).Select(s => s.Topic).ToList();

				topics.AddRange(this._addedTopics.Where(w => w.Domain == domain));

				return Task.FromResult<IEnumerable<Topic>>(topics);
			}

			public Task SaveChanges()
			{
				this._owner._entries.AddRange(this._addedTopics.Select(s => new TopicEntry(s)));

				foreach (var publisherTuple in this._addedPublishers)
				{
					Topic.Id topicId = publisherTuple.Item1;
					Publisher publisher = publisherTuple.Item2;

					TopicEntry topicEntry = this._owner._entries.Single(w => w.Topic.Uuid == topicId.Uuid);

					topicEntry.Publishers.Add(publisher);
				}


				foreach (var subscriberTuple in this._addedSubscribers)
				{
					Topic.Id topicId = subscriberTuple.Item1;
					Subscriber subscriber = subscriberTuple.Item2;

					TopicEntry topicEntry = this._owner._entries.Single(w => w.Topic.Uuid == topicId.Uuid);

					topicEntry.Subscribers.Add(subscriber);
				}

				return Task.CompletedTask;
			}

			private readonly InMemoryStorageFactory _owner;
			public readonly List<Topic> _addedTopics;
			public readonly List<(Topic.Id, Publisher)> _addedPublishers;
			public readonly List<(Topic.Id, Subscriber)> _addedSubscribers;
		}


		private class TopicEntry
		{
			public TopicEntry(Topic topic)
			{
				this.Topic = topic;
				this.Publishers = new HashSet<Publisher>();
				this.Subscribers = new HashSet<Subscriber>();
			}

			public readonly Topic Topic;
			public readonly HashSet<Publisher> Publishers;
			public readonly HashSet<Subscriber> Subscribers;
		}
		private readonly List<TopicEntry> _entries;
	}
}

