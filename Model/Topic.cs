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

	public class Topic : Topic.Options, Topic.Id
	{
		public Topic(Guid topicUuid, Options options, DateTime createAt, DateTime? deleteAt)
		{
			this.Uuid = topicUuid;
			this.Name = options.Name;
			this.Domain = options.Domain;
			this.Description = options.Description;
			this.MediaType = options.MediaType;
			this.CreateAt = createAt;
			this.DeleteAt = deleteAt;
		}

		public Guid Uuid { get; }

		public string QualifiedName => this.Domain != null ? string.Concat(this.Domain, ".", this.Name) : this.Name;

		public string Name { get; }

		public string? Domain { get; }

		public string Description { get; }

		public string MediaType { get; }

		DateTime CreateAt { get; }

		DateTime? DeleteAt { get; }


		public interface Id { Guid Uuid { get; } }

		public interface Options
		{
			/**
			* Human readable name defines a `Topic`'s purpose
			*/
			string Name { get; }

			/**
			 * Used for domain owned topics
			 */
			string? Domain { get; }

			/**
			* Human readable (long) description defines a `Topic`'s purpose
			*/
			string Description { get; }

			/**
			 * Message media type
			 * https://en.wikipedia.org/wiki/Media_type
			 */
			string MediaType { get; }
		}

		public sealed class OptionsBuilder
		{
			public Options Build()
			{
				string? name = this._name;
				string? description = this._description;
				string? mediaType = this._mediaType;
				string? domain = this._domain;

				if (name == null) { throw new InvalidOperationException($"Cannot build topic options due 'name' was not set. Try to call {nameof(WithName)}() before build."); }
				if (description == null) { throw new InvalidOperationException($"Cannot build topic options due 'name' was not set. Try to call {nameof(WithDescription)}() before build."); }
				if (mediaType == null) { throw new InvalidOperationException($"Cannot build topic options due 'name' was not set. Try to call {nameof(WithMediaType)}() before build."); }

				return new _Options(name, description, mediaType, domain);
			}

			public OptionsBuilder WithName(string value)
			{
				this._name = value;
				return this;
			}

			public OptionsBuilder WithDescription(string value)
			{
				this._description = value;
				return this;
			}

			public OptionsBuilder WithMediaType(string value)
			{
				this._mediaType = value;
				return this;
			}

			public OptionsBuilder WithMediaTypeTextPlain()
			{
				this._mediaType = "text/plain";
				return this;
			}

			public OptionsBuilder WithMediaTypeApplicationJson()
			{
				this._mediaType = "application/json";
				return this;
			}

			public OptionsBuilder WithDomain(string value)
			{
				this._domain = value;
				return this;
			}

			private sealed class _Options : Options
			{
				public _Options(string name, string description, string mediaType, string? domain)
				{
					this.Name = name;
					this.Description = description;
					this.MediaType = mediaType;
					this.Domain = domain;
				}

				public string Name { get; }

				public string? Domain { get; }

				public string Description { get; }

				public string MediaType { get; }
			}

			private string? _name;
			private string? _description;
			private string? _mediaType;
			private string? _domain;
		}
	}
}