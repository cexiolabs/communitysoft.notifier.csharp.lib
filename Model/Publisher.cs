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

	public abstract class Publisher : Publisher.Id, Publisher.Options
	{
		public Guid Uuid => this._uuid;

		public Publisher(Guid publisherUuid, Options options)
		{
			this._uuid = publisherUuid;
		}

		public interface Id { Guid Uuid { get; } }
		public interface Options { }

		private readonly Guid _uuid;
	}

	public class PublisherHttp : Publisher, PublisherHttp.Options
	{
		public PublisherHttp(Guid publisherId, PublisherHttp.Options options)
			: base(publisherId, options)
		{
			this.Path = options.Path;
		}

		public string Path { get; }

		public new interface Options : Publisher.Options
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

			private sealed class _Options : PublisherHttp.Options
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
	}
}