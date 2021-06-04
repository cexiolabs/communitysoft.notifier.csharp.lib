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
	using System.Threading;
	using System.Threading.Tasks;

	using Model;

	public interface IStorage : IDisposable
	{
		Task<PublisherHttp> CreatePublisher(CancellationToken cancellationToken, Topic.Id topicId, PublisherHttp.Options publisherOptions);

		Task<SubscriberConsole> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberConsole.Options subscriberOptions);

		Task<SubscriberTelegram> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberTelegram.Options subscriberOptions);

		Task<SubscriberWebSocket> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberWebSocket.Options subscriberOptions);

		Task<SubscriberWebhook> CreateSubscriber(CancellationToken cancellationToken, Topic.Id topicId, SubscriberWebhook.Options subscriberOptions);

		Task<Topic> CreateTopic(CancellationToken cancellationToken, Topic.Options topicOptions);

		Task DestroyPublisher(CancellationToken cancellationToken, Topic.Id topicId, Publisher.Id publisherId, DateTime destroyDate);

		Task DestroySubscriber(CancellationToken cancellationToken, Topic.Id topicId, Subscriber.Id subscriberId, DateTime destroyDate);

		Task DestroyTopic(CancellationToken cancellationToken, Topic.Id topicId, DateTime destroyDate);

		Task<Topic> GetTopic(CancellationToken cancellationToken, Topic.Id topicId);

		Task<IEnumerable<Publisher>> ListPublishers(CancellationToken cancellationToken, Topic.Id topicId);

		Task<IEnumerable<Subscriber>> ListSubscribers(CancellationToken cancellationToken, Topic.Id topicId);

		Task<IEnumerable<Topic>> ListTopics(CancellationToken cancellationToken);

		Task<IEnumerable<Topic>> ListTopics(CancellationToken cancellationToken, string domain);

		Task SaveChanges();
	}
}
