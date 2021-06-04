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
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	using Model;
	using MessageBus;

	using Internal.Misc;
	using Intrenal.PublisherActor;
	using Intrenal.SubscriberActor;

	/**
	* WorkerService
	*/
	public class WorkerService : IDisposable
	{
		public WorkerService(
			ILoggerFactory loggerFactory,
			ILogger<WorkerService> logger,
			IMessageBus messageBus
		)
		{
			this._logger = logger;
			this._messageBus = messageBus;
			this._topicControllers = new Dictionary<Topic, TopicController>();
			this._webServer = new WebServer(loggerFactory);
		}

		public void Dispose()
		{
			this._webServer.Dispose();
		}

		public Task RegisterPublisher(CancellationToken cancellationToken, Topic topic, Publisher publisher)
		{
			TopicController topicController = this.GetOrCreateTopicController(topic);

			AbstractPublisherActor publisherActor;

			if (publisher is PublisherHttp publisherHttp)
			{
				publisherActor = new PublisherHttpActor(topic, publisherHttp, this._webServer);
			}
			else
			{
				throw new NotSupportedException($"An publisher type '${publisher.GetType().FullName}' not supported yet.");
			}

			topicController.AddPublisherActor(publisherActor);

			return Task.CompletedTask;
		}

		public Task RegisterSubscriber(CancellationToken cancellationToken, Topic topic, Subscriber subscriber)
		{
			TopicController topicController = this.GetOrCreateTopicController(topic);

			AbstractSubscriberActor subscriberActor;

			if (subscriber is SubscriberConsole subscriberConsole)
			{
				subscriberActor = new SubscriberConsoleActor(topic, subscriberConsole);
			}
			else if (subscriber is SubscriberTelegram subscriberTelegram)
			{
				throw new NotImplementedException();
			}
			else if (subscriber is SubscriberWebSocket subscriberWebSocket)
			{
				subscriberActor = new SubscriberWebSocketActor(topic, subscriberWebSocket, this._webServer);
			}
			else if (subscriber is SubscriberWebhook subscriberWebhook)
			{
				throw new NotImplementedException();
			}
			else
			{
				throw new NotSupportedException($"An publisher type '${subscriber.GetType().FullName}' not supported yet.");
			}

			topicController.AddSubsriberActor(subscriberActor);

			return Task.CompletedTask;
		}

		public async Task UnRegisterAll(CancellationToken cancellationToken)
		{
			List<Exception> exs = new List<Exception>();

			foreach (var topicController in this._topicControllers)
			{
				Topic topic = topicController.Key;
				TopicController bundle = topicController.Value;

				foreach (var publisher in bundle.Publishers)
				{
					if (cancellationToken.IsCancellationRequested) { break; }

					try { await this.UnRegisterPublisher(cancellationToken, topic, publisher.Publisher.Uuid); } catch (Exception ex) { exs.Add(ex); }
				}

				foreach (var subscriber in bundle.Subscribers)
				{
					if (cancellationToken.IsCancellationRequested) { break; }

					try { await this.UnRegisterSubscriber(cancellationToken, topic, subscriber.Subscriber.Uuid); } catch (Exception ex) { exs.Add(ex); }
				}
			}

			if (exs.Count > 0)
			{
				throw new AggregateException(exs);
			}

			cancellationToken.ThrowIfCancellationRequested();
		}

		public Task UnRegisterPublisher(CancellationToken cancellationToken, Topic topic, Guid publisherUuid)
		{
			TopicController topicController = this.GetOrCreateTopicController(topic);

			AbstractPublisherActor publisherActor = topicController.GetPublisherActor(publisherUuid);

			if (publisherActor is IDisposable disposablePublisherActor)
			{
				disposablePublisherActor.Dispose();
			}

			topicController.RemovePublisherActor(publisherUuid);

			return Task.CompletedTask;
		}

		public Task UnRegisterSubscriber(CancellationToken cancellationToken, Topic topic, Guid subscriberUuid)
		{
			TopicController topicController = this.GetOrCreateTopicController(topic);

			AbstractSubscriberActor subscriberActor = topicController.GetSubscriberActor(subscriberUuid);

			if (subscriberActor is IDisposable disposableSubscriberActor)
			{
				disposableSubscriberActor.Dispose();
			}

			topicController.RemoveSubscriberActor(subscriberUuid);

			return Task.CompletedTask;
		}

		private TopicController GetOrCreateTopicController(Topic topic)
		{
			TopicController topicController;
			if (this._topicControllers.ContainsKey(topic))
			{
				topicController = this._topicControllers[topic];
			}
			else
			{
				topicController = new TopicController(this._messageBus, topic);
				this._topicControllers.Add(topic, topicController);
			}

			return topicController;
		}

		private sealed class TopicController : IDisposable
		{
			public TopicController(IMessageBus messageBus, Topic topic)
			{
				this._messageBus = messageBus;
				this.topic = topic;
				this._publishers = new Dictionary<Guid, AbstractPublisherActor>();
				this._subscribers = new Dictionary<Guid, AbstractSubscriberActor>();
				this._messageChannel = null;
				this._disposeCancellationTokenSource = new CancellationTokenSource();
				this._syncRoot = new object();
				this._watchdogTimer = null;
			}

			public IEnumerable<AbstractPublisherActor> Publishers => this._publishers.Values;
			public IEnumerable<AbstractSubscriberActor> Subscribers => this._subscribers.Values;

			public void AddPublisherActor(AbstractPublisherActor publisherActor)
			{
				Guid publisherUuid = publisherActor.Publisher.Uuid;

				lock (this._syncRoot)
				{
					if (this._publishers.ContainsKey(publisherUuid))
					{
						throw new InvalidOperationException($"Cannot register publisher '${publisherUuid}' twice.");
					}

					this._publishers.Add(publisherUuid, publisherActor);
				}

				publisherActor.AddHandler(this.PublishMessage);
			}

			public void AddSubsriberActor(AbstractSubscriberActor subscriberActor)
			{
				Guid subscriberUuid = subscriberActor.Subscriber.Uuid;

				lock (this._syncRoot)
				{
					if (this._subscribers.ContainsKey(subscriberUuid))
					{
						throw new InvalidOperationException($"Cannot register subscriber '${subscriberUuid}' twice.");
					}

					this._subscribers.Add(subscriberUuid, subscriberActor);
				}

				this.SetupChannelWatchdogTimer();
			}

			public void Dispose()
			{
				this._disposeCancellationTokenSource.Cancel();
			}

			public AbstractPublisherActor GetPublisherActor(Guid publisherUuid)
			{
				return this._publishers[publisherUuid];
			}

			public AbstractSubscriberActor GetSubscriberActor(Guid subscriberUuid)
			{
				return this._subscribers[subscriberUuid];
			}

			public void RemoveSubscriberActor(Guid subscriberUuid)
			{
				AbstractSubscriberActor subscriberActor = this._subscribers[subscriberUuid];
				this._subscribers.Remove(subscriberUuid);

				this.SetupChannelWatchdogTimer();
			}

			public void RemovePublisherActor(Guid publisherUuid)
			{
				AbstractPublisherActor publisherActor = this._publishers[publisherUuid];

				publisherActor.RemoveHandler(this.PublishMessage);
				this._publishers.Remove(publisherUuid);
			}

			public readonly Topic topic;

			private async Task PublishMessage(CancellationToken cancellationToken, ChannelEvent<Message> channelEvent)
			{
				if (channelEvent is ChannelEventMessage<Message> messageEvent)
				{
					await this._messageBus.Publish(cancellationToken, this.topic.QualifiedName, messageEvent.Message);
				}

				this.SetupChannelWatchdogTimer();

				await Task.CompletedTask;
			}

			private void SetupChannelWatchdogTimer(int dueTime = 1)
			{
				lock (this._syncRoot)
				{
					if (this._watchdogTimer == null)
					{
						this._watchdogTimer = new Timer(this.ChannelWatchdogTimerHandler, dueTime, dueTime, Timeout.Infinite);
					}
				}
			}

			private void ChannelWatchdogTimerHandler(object? state)
			{
				int dueTime = state != null && state is int ? (int)state : 1024;

				try
				{
					CancellationToken cancellationToken = this._disposeCancellationTokenSource.Token;

					IChannel<Message>? messageChannel;
					int subscribersCount;
					lock (this._syncRoot)
					{
						messageChannel = this._messageChannel;
						subscribersCount = this._subscribers.Count;
					}

					if (messageChannel == null)
					{
						if (subscribersCount > 0)
						{
							if (!cancellationToken.IsCancellationRequested)
							{
								Task<IChannel<Message>> messageChannelRetainTask = this._messageBus.RetainChannel(cancellationToken, topic.QualifiedName);
								messageChannelRetainTask.Wait();
								messageChannel = messageChannelRetainTask.Result;
								lock (this._syncRoot)
								{
									System.Diagnostics.Debug.Assert(this._messageChannel == null);
									this._messageChannel = messageChannel;
									foreach (AbstractSubscriberActor subscriberActor in this._subscribers.Values)
									{
										subscriberActor.MessageChannel = messageChannel;
									}
								}
							}
							else
							{
								foreach (AbstractSubscriberActor subscriberActor in this._subscribers.Values)
								{
									subscriberActor.MessageChannel = null;
								}
							}
						}
					}
					else
					{
						if (subscribersCount == 0 || cancellationToken.IsCancellationRequested)
						{
							lock (this._syncRoot)
							{
								System.Diagnostics.Debug.Assert(this._messageChannel != null);
								this._messageChannel = null;
								foreach (AbstractSubscriberActor subscriberActor in this._subscribers.Values)
								{
									subscriberActor.MessageChannel = null;
								}
							}
							messageChannel.Dispose();
						}
						else
						{
							foreach (AbstractSubscriberActor subscriberActor in this._subscribers.Values)
							{
								subscriberActor.MessageChannel = messageChannel;
							}
						}
					}

					lock (this._syncRoot)
					{
						this._watchdogTimer = null;
					}
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex);
					lock (this._syncRoot)
					{
						System.Diagnostics.Debug.Assert(this._watchdogTimer != null);
						this._watchdogTimer = null;
					}
					this.SetupChannelWatchdogTimer((int)(dueTime * 1.41));
				}
			}

			private readonly CancellationTokenSource _disposeCancellationTokenSource;
			private IChannel<Message>? _messageChannel;
			private readonly IMessageBus _messageBus;
			private readonly Dictionary<Guid, AbstractPublisherActor> _publishers;
			private readonly Dictionary<Guid, AbstractSubscriberActor> _subscribers;
			private readonly object _syncRoot;
			private Timer? _watchdogTimer;
		}

		private readonly ILogger<WorkerService> _logger;
		private readonly IMessageBus _messageBus;
		private readonly Dictionary<Topic, TopicController> _topicControllers;
		private readonly WebServer _webServer;
	}

}
