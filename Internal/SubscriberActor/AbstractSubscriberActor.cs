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

namespace CEXIOLABS.CommunitySoft.Notifier.Lib.Intrenal.SubscriberActor
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Logging;

	using Model;

	internal abstract class AbstractSubscriberActor
	{
		public AbstractSubscriberActor(Topic topicModel, Subscriber subscriber)
		{
			this.Topic = topicModel;
			this.Subscriber = subscriber;
		}

		public Topic Topic { get; }
		public Subscriber Subscriber { get; }
		public IChannel<Message>? MessageChannel
		{
			get => this._messageChannel;
			set
			{
				if (this._messageChannel != null) { this._messageChannel.RemoveHandler(this.OnChannelEvent); }
				this._messageChannel = value;
				if (this._messageChannel != null) { this._messageChannel.AddHandler(this.OnChannelEvent); }
			}
		}

		protected abstract Task OnMessage(CancellationToken cancellationToken, Message message);

		private async Task OnChannelEvent(CancellationToken cancellationToken, ChannelEvent<Message> channelEvent)
		{
			IChannel<Message> source = channelEvent.Source;

			if (channelEvent is ChannelEventMessage<Message> messageEvent)
			{
				await this.OnMessage(cancellationToken, messageEvent.Message);
			}
			else if (channelEvent is ChannelEventError<Message> errorEvent)
			{
				Exception ex = errorEvent.Ex;
				Console.Error.WriteLine(ex); // TODO
				this.MessageChannel = null;
			}
		}

		private IChannel<Message>? _messageChannel;
	}

	internal abstract class AbstractSubscriberActor<TSubscriber> : AbstractSubscriberActor where TSubscriber : Subscriber
	{
		public AbstractSubscriberActor(Topic topic, TSubscriber subscriber)
			: base(topic, subscriber) { }

		public new TSubscriber Subscriber => (TSubscriber)base.Subscriber;
	}
}