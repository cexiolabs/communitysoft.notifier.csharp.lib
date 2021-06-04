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


namespace CEXIOLABS.CommunitySoft.Notifier.Lib.MessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Model;

	public class InMemoryMessageBus : IMessageBus, IDisposable
	{
		public InMemoryMessageBus()
		{
			this._channels = new HashSet<Channel>();
			this._queues = new Dictionary<string, LinkedList<Message>>();
			this._pumpWaitHandle = new ManualResetEvent(false);
			this._pumpThread = new Thread(this.PumpThreadEntryPoint);
			this._pumpThread.Start();
		}


		public void Dispose()
		{
			if (this._disposed) { return; }

			this._disposed = true;
			this._pumpWaitHandle.Set();
			this._pumpThread.Join();
		}

		public Task Publish(CancellationToken cancellationToken, string topicQualifiedName, Message message)
		{
			LinkedList<Message> queue = this.GetQueue(topicQualifiedName);

			queue.AddLast(message);

			this._pumpWaitHandle.Set();

			return Task.CompletedTask;
		}

		public Task<IChannel<Message>> RetainChannel(CancellationToken cancellationToken, string topicQualifiedName)
		{
			LinkedList<Message> queue = this.GetQueue(topicQualifiedName);

			Channel channel = new Channel(this, queue);

			lock (this._channels)
			{
				this._channels.Add(channel);
			}

			return Task.FromResult<IChannel<Message>>(channel);
		}

		private class Channel : IChannel<Message>
		{
			public Channel(InMemoryMessageBus owner, LinkedList<Message> queue)
			{
				this._owner = owner;
				this._queue = queue;
				this._handlers = new List<ChannelCallback<Message>>();
			}

			public void AddHandler(ChannelCallback<Message> cb)
			{
				this._handlers.Add(cb);
			}

			public void Dispose()
			{
				lock (this._owner._channels)
				{
					this._owner._channels.Remove(this);
				}
			}

			public async Task Pump(CancellationToken cancellationToken)
			{
				var handlers = this._handlers.ToArray();
				if (handlers.Length == 0) { return; }

				while (this._queue.Count > 0)
				{
					Message message;
					lock (this._queue)
					{
						var firstNode = this._queue.First;
						if (firstNode == null) { return; }

						message = firstNode.Value;
						this._queue.RemoveFirst();
					}

					try
					{
						ChannelEventMessage<Message> channelEvent = new ChannelEventMessage<Message>(this, message);
						foreach (var handler in handlers)
						{
							await handler(cancellationToken, channelEvent);
						}
					}
					catch
					{
						lock (this._queue)
						{
							this._queue.AddFirst(message);
						}
						throw;
					}
				}
			}

			public void RemoveHandler(ChannelCallback<Message> cb)
			{
				System.Diagnostics.Debug.Assert(this._handlers.Contains(cb));
				this._handlers.Remove(cb);
			}

			private readonly InMemoryMessageBus _owner;
			private readonly LinkedList<Message> _queue;
			private List<ChannelCallback<Message>> _handlers;
		}

		private LinkedList<Message> GetQueue(string topicQualifiedName)
		{
			if (!this._queues.ContainsKey(topicQualifiedName))
			{
				this._queues.Add(topicQualifiedName, new LinkedList<Message>());
			}

			return this._queues[topicQualifiedName];
		}

		private void PumpThreadEntryPoint()
		{
			while (true)
			{
				this._pumpWaitHandle.WaitOne();
				this._pumpWaitHandle.Reset();

				if (this._disposed) { return; }

				Channel[] channels;
				lock (this._channels)
				{
					channels = this._channels.ToArray();
				}

				foreach (Channel channel in channels)
				{
					try
					{
						channel.Pump(CancellationToken.None).Wait();
					}
					catch (Exception ex)
					{
						// TODO: Handle
						Console.Error.WriteLine(ex);
					}
				}
			}
		}

		private readonly HashSet<Channel> _channels;
		private readonly Dictionary<string, LinkedList<Message>> _queues;
		private readonly ManualResetEvent _pumpWaitHandle;
		private readonly Thread _pumpThread;
		private volatile bool _disposed;
	}
}
