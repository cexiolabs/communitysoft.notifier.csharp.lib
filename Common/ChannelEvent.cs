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

	public abstract class ChannelEvent<TMessage>
	{
		public ChannelEvent(IChannel<TMessage> source)
		{
			this.Source = source;
		}
		public IChannel<TMessage> Source { get; }
	}

	public sealed class ChannelEventMessage<TMessage> : ChannelEvent<TMessage>
	{
		public ChannelEventMessage(IChannel<TMessage> source, TMessage message) : base(source) { this.Message = message; }

		public TMessage Message { get; }
	}

	public sealed class ChannelEventError<TMessageData> : ChannelEvent<TMessageData>
	{
		public ChannelEventError(IChannel<TMessageData> source, Exception ex) : base(source) { this.Ex = ex; }

		public Exception Ex { get; }
	}
}
