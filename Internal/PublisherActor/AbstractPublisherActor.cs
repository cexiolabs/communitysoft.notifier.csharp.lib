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


namespace CEXIOLABS.CommunitySoft.Notifier.Lib.Intrenal.PublisherActor
{
	using Microsoft.Extensions.Logging;

	using Model;

	internal abstract class AbstractPublisherActor : AbstractMessageChannel
	{
		public AbstractPublisherActor(Topic topicModel, Publisher publisher)
		{
			this.Topic = topicModel;
			this.Publisher = publisher;
		}

		public Topic Topic { get; }
		public Publisher Publisher { get; }
	}

	internal abstract class AbstractPublisherActor<TPublisher> : AbstractPublisherActor where TPublisher : Publisher
	{
		public AbstractPublisherActor(Topic topic, TPublisher publisher)
			: base(topic, publisher) { }

		public new TPublisher Publisher => (TPublisher)base.Publisher;
	}
}