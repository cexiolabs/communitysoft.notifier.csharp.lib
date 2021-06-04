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
	using System;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.AspNetCore.Http.Features;

	using Model;

	internal class PublisherHttpActor : AbstractPublisherActor<PublisherHttp>, IDisposable
	{
		public PublisherHttpActor(Topic topic, PublisherHttp publisher, Internal.Misc.WebServer webServer) : base(topic, publisher)
		{
			this._webServerBinder = webServer.Bind("/" + publisher.Path, this.OnWebRequest);
		}

		public override void Dispose()
		{
			this._webServerBinder.Dispose();
		}

		private async Task OnWebRequest(
			IHttpConnectionFeature connection,
			IHttpRequestLifetimeFeature requestLifetime,
			IHttpRequestFeature request,
			IHttpResponseFeature response,
			IHttpResponseBodyFeature responseBody
		)
		{
			CancellationToken cancellationToken = requestLifetime.RequestAborted;

			string mediaType = "application/octet-stream";
			if (request.Headers.ContainsKey("Content-Type"))
			{
				mediaType = request.Headers["Content-Type"];
			}

			using MemoryStream memoryStream = new MemoryStream();
			await request.Body.CopyToAsync(memoryStream);

			byte[] data = memoryStream.ToArray();

			Message message = new Message(mediaType, data);

			await base.OnMessage(cancellationToken, message);

			response.StatusCode = 200; //publisher.StatusCode;
			// response.Headers.Add("Content-Type", publisher.ContentType);

			// byte[] content = publisher.Content;
			// response.Headers.ContentLength = content.Length;
			// await responseBody.Stream.WriteAsync(content, 0, this.content.Length);

			await responseBody.CompleteAsync();
		}


		private readonly IDisposable _webServerBinder;
	}
}