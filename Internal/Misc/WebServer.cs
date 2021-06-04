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

namespace CEXIOLABS.CommunitySoft.Notifier.Lib.Internal.Misc
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.AspNetCore.Hosting.Server;
	using Microsoft.AspNetCore.Http.Features;
	using Microsoft.AspNetCore.Server.Kestrel.Core;
	using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.Options;

	internal class WebServer : IHttpApplication<IFeatureCollection>, IDisposable
	{
		public delegate Task FeatureHandler(IFeatureCollection context);
		public delegate Task RequestHandler(
		IHttpConnectionFeature connection,
		IHttpRequestLifetimeFeature requestLifetime,
		IHttpRequestFeature request,
		IHttpResponseFeature response,
		IHttpResponseBodyFeature responseBody
	);

		public WebServer(ILoggerFactory loggerFactory)
		{
			var kestrelServer = new KestrelServer(new ConfigureKestrelServerOptions(), new SocketTransportFactory(new ConfigureSocketTransportOptions(), loggerFactory), loggerFactory);
			kestrelServer.Options.ListenLocalhost(8080);
			kestrelServer.StartAsync(this, CancellationToken.None).GetAwaiter().GetResult();
			this._kestrelServer = kestrelServer;
			this._binders = new List<Binder>();
		}

		public IDisposable Bind(string path, WebServer.FeatureHandler handler)
		{
			Binder binder = new FeatureBinder(this, path, handler);
			this._binders.Add(binder);
			return binder;
		}
		public IDisposable Bind(string path, WebServer.RequestHandler handler)
		{
			Binder binder = new RequestBinder(this, path, handler);
			this._binders.Add(binder);
			return binder;
		}

		public void Dispose()
		{
			this._kestrelServer.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
		}

		IFeatureCollection IHttpApplication<IFeatureCollection>.CreateContext(IFeatureCollection contextFeatures)
		{
			return contextFeatures;
		}

		async Task IHttpApplication<IFeatureCollection>.ProcessRequestAsync(IFeatureCollection context)
		{
			IHttpConnectionFeature connection = (IHttpConnectionFeature)context[typeof(IHttpConnectionFeature)]!;
			IHttpRequestFeature request = (IHttpRequestFeature)context[typeof(IHttpRequestFeature)]!;
			IHttpResponseFeature response = (IHttpResponseFeature)context[typeof(IHttpResponseFeature)]!;
			IHttpResponseBodyFeature responseBody = (IHttpResponseBodyFeature)context[typeof(IHttpResponseBodyFeature)]!;
			IHttpRequestLifetimeFeature requestLifetime = (IHttpRequestLifetimeFeature)context[typeof(IHttpRequestLifetimeFeature)]!;

			string requestPath = request.Path;
			foreach (Binder binder in this._binders)
			{
				if (binder is FeatureBinder featureBinder)
				{
					string binderPath = featureBinder.Path;
					if (requestPath == binderPath)
					{
						await featureBinder.Handler(context);
						return;
					}
				}
				else if (binder is RequestBinder reuqestBinder)
				{
					string binderPath = reuqestBinder.Path;
					if (requestPath == binderPath)
					{
						await reuqestBinder.Handler(connection, requestLifetime, request, response, responseBody);
						return;
					}
				}
			}

			response.StatusCode = 404;
			await responseBody.CompleteAsync();
		}

		void IHttpApplication<IFeatureCollection>.DisposeContext(IFeatureCollection context, Exception exception)
		{
			// NOP
		}

		private sealed class ConfigureKestrelServerOptions : IOptions<KestrelServerOptions>
		{
			public ConfigureKestrelServerOptions()
			{
				this.Value = new KestrelServerOptions()
				{
				};
			}

			public KestrelServerOptions Value { get; }
		}

		public sealed class ConfigureSocketTransportOptions : IOptions<SocketTransportOptions>
		{
			public ConfigureSocketTransportOptions()
			{
				this.Value = new SocketTransportOptions()
				{
				};
			}

			public SocketTransportOptions Value { get; }
		}

		public abstract class Binder : IDisposable
		{
			public abstract void Dispose();
		}
		public class FeatureBinder : Binder
		{
			public FeatureBinder(WebServer owner, string path, FeatureHandler handler)
			{
				this.Path = path;
				this.Handler = handler;
				this._owner = owner;
			}

			public string Path { get; }
			public FeatureHandler Handler { get; }

			public override void Dispose()
			{
				System.Diagnostics.Debug.Assert(this._owner._binders.Contains(this));
				this._owner._binders.Remove(this);
			}

			private readonly WebServer _owner;

		}
		public class RequestBinder : Binder
		{
			public RequestBinder(WebServer owner, string path, RequestHandler handler)
			{
				this.Path = path;
				this.Handler = handler;
				this._owner = owner;
			}

			public string Path { get; }
			public RequestHandler Handler { get; }

			public override void Dispose()
			{
				System.Diagnostics.Debug.Assert(this._owner._binders.Contains(this));
				this._owner._binders.Remove(this);
			}

			private readonly WebServer _owner;

		}

		private readonly KestrelServer _kestrelServer;
		private readonly List<Binder> _binders;
	}

}