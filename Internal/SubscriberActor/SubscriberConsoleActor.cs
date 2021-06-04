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

	using Model;

	internal class SubscriberConsoleActor : AbstractSubscriberActor<SubscriberConsole>
	{
		public SubscriberConsoleActor(Topic topic, SubscriberConsole subscriber)
			: base(topic, subscriber) { }

		protected override Task OnMessage(CancellationToken cancellationToken, Message message)
		{
			ConsoleColor colorBackup = Console.ForegroundColor;
			try
			{
				Console.ForegroundColor = this.Subscriber.TextColor;

				if (message.MediaType == "text/plain")
				{
					string text = System.Text.Encoding.UTF8.GetString(message.Data);
					Console.WriteLine("Handle message with media type: {0}, data: {1}", message.MediaType, text);
				}
				else
				{
					Console.WriteLine("Handle message with media type: {0}, data length: {1}", message.MediaType, message.Data.Length);
				}
			}
			finally
			{
				Console.ForegroundColor = colorBackup;
			}

			return Task.CompletedTask;
		}
	}
}



