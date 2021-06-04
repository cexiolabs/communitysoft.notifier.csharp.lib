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

namespace CEXIOLABS.CommunitySoft.Notifier.Lib.Model
{
	using System;
	using System.Linq;

	public abstract class Subscriber : Subscriber.Id, Subscriber.Options
	{
		public interface Id
		{
			Guid Uuid { get; }
		}
		public interface Options { }

		public Guid Uuid => this._uuid;

		public Subscriber(Guid subscriberUuid, Subscriber.Options options)
		{
			this._uuid = subscriberUuid;
		}

		private readonly Guid _uuid;
	}

	public class SubscriberConsole : Subscriber, SubscriberConsole.Options
	{
		public new interface Options : Subscriber.Options
		{
			ConsoleColor TextColor { get; }
		}

		public sealed class OptionsBuilder
		{
			public Options Build()
			{
				return new _Options(this._textColor);
			}

			public OptionsBuilder WithTextColor(ConsoleColor value)
			{
				this._textColor = value;
				return this;
			}

			private sealed class _Options : Options
			{
				public _Options(ConsoleColor? textColor)
				{
					this.TextColor = textColor ?? ConsoleColor.White;
				}
				public ConsoleColor TextColor { get; }
			}

			private ConsoleColor? _textColor;
		}

		public SubscriberConsole(Guid subscriberUuid, SubscriberConsole.Options options)
			: base(subscriberUuid, options)
		{
		}

		public ConsoleColor TextColor { get; }
	}

	public class SubscriberWebhook : Subscriber, SubscriberWebhook.Options
	{
		public new interface Options : Subscriber.Options { }

		public sealed class OptionsBuilder
		{
			public Options Build()
			{
				return new _Options();
			}

			private sealed class _Options : Options
			{

			}
		}

		public SubscriberWebhook(Guid subscriberId, SubscriberWebhook.Options options)
			: base(subscriberId, options) { }
	}

	public class SubscriberWebSocket : Subscriber, SubscriberWebSocket.Options
	{
		public new interface Options : Subscriber.Options
		{
			string Path { get; }
		}

		public sealed class OptionsBuilder
		{
			public OptionsBuilder()
			{
				this._path = "/";
			}

			public Options Build()
			{
				return new _Options(this._path);
			}

			public OptionsBuilder WithPathSegments(params string[] pathSegments)
			{
				this._path = String.Join('/', pathSegments.Select(Uri.EscapeUriString));
				return this;
			}

			private sealed class _Options : SubscriberWebSocket.Options
			{
				public _Options(string path)
				{
					this._path = path;
				}
				public string Path => this._path;


				private readonly string _path;
			}

			private string _path;
		}

		public SubscriberWebSocket(Guid subscriberId, SubscriberWebSocket.Options options)
			: base(subscriberId, options)
		{
			this.Path = options.Path;
		}

		public string Path { get; }
	}

	public class SubscriberTelegram : Subscriber, SubscriberTelegram.Options
	{
		public new interface Options : Subscriber.Options { }

		public sealed class OptionsBuilder
		{
			public Options Build()
			{
				return new _Options();
			}

			private sealed class _Options : Options
			{

			}
		}

		public SubscriberTelegram(Guid subscriberId, SubscriberTelegram.Options options)
			: base(subscriberId, options) { }
	}
}