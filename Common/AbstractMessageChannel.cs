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
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using Model;

	public abstract class AbstractMessageChannel : IChannel<Message>
	{
		public AbstractMessageChannel()
		{
			this._handlers = new List<ChannelCallback<Message>>();
		}

		public void AddHandler(ChannelCallback<Message> cb)
		{
			lock (this._handlers)
			{
				this._handlers.Add(cb);
			}
		}

		public abstract void Dispose();

		public void RemoveHandler(ChannelCallback<Message> cb)
		{
			lock (this._handlers)
			{
				System.Diagnostics.Debug.Assert(this._handlers.Contains(cb));
				this._handlers.Remove(cb);
			}
		}

		protected async Task OnMessage(CancellationToken cancellationToken, Message message)
		{
			ChannelEventMessage<Message> channelEvent = new ChannelEventMessage<Message>(this, message);

			List<Exception> exs = new List<Exception>();

			ChannelCallback<Message>[] handlers;
			lock (this._handlers) { handlers = this._handlers.ToArray(); }

			foreach (ChannelCallback<Message> handler in handlers)
			{
				if (cancellationToken.IsCancellationRequested) { break; }

				try
				{
					await handler(cancellationToken, channelEvent);
				}
				catch (Exception ex)
				{
					exs.Add(ex);
				}
			}

			if (exs.Count > 0)
			{
				throw new AggregateException(exs);
			}

			cancellationToken.ThrowIfCancellationRequested();
		}

		private readonly List<ChannelCallback<Message>> _handlers;
	}
}
