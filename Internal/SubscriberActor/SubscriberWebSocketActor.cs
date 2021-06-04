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
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net.WebSockets;
	using System.Security.Cryptography;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.AspNetCore.Http;
	using Microsoft.AspNetCore.Builder;
	using Microsoft.AspNetCore.WebSockets;
	using Microsoft.AspNetCore.Http.Features;
	using Microsoft.Net.Http.Headers;

	using Model;

	internal class SubscriberWebSocketActor : AbstractSubscriberActor<SubscriberWebSocket>, IDisposable
	{
		public SubscriberWebSocketActor(Topic topic, SubscriberWebSocket subscriber, Internal.Misc.WebServer webServer)
			: base(topic, subscriber)
		{
			this._webServerBinder = webServer.Bind("/" + subscriber.Path, this.OnWebRequest);
			this._webSockets = new HashSet<WebSocket>();
			this._disposeCancellationTokenSource = new CancellationTokenSource();
		}

		public void Dispose()
		{
			this._disposeCancellationTokenSource.Cancel();
			this._webServerBinder.Dispose();
		}

		private async Task OnWebRequest(IFeatureCollection context)
		{
			IHttpUpgradeFeature upgradeFeature = context.Get<IHttpUpgradeFeature>();
			IHttpRequestFeature request = context.Get<IHttpRequestFeature>();
			IHttpResponseFeature response = context.Get<IHttpResponseFeature>();

			WebSocketOptions wsOptions = new WebSocketOptions();
			UpgradeHandshake webSocketFeature = new UpgradeHandshake(request, response, upgradeFeature, wsOptions);
			context.Set<IHttpWebSocketFeature>(webSocketFeature);

			WebSocket webSocket = await webSocketFeature.AcceptAsync(new WebSocketAcceptContext() { SubProtocol = null });

			lock (this._webSockets) { this._webSockets.Add(webSocket); }
			try
			{
				byte[] buffer = new byte[1024 * 4];
				CancellationToken cancellationToken = this._disposeCancellationTokenSource.Token;
				while (webSocket.State == WebSocketState.Open)
				{
					await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
				}
			}
			finally
			{
				lock (this._webSockets) { this._webSockets.Remove(webSocket); }
			}

			await Task.CompletedTask;
		}

		protected override async Task OnMessage(CancellationToken cancellationToken, Message message)
		{
			WebSocket[] webSockets;
			lock (this._webSockets) { webSockets = this._webSockets.ToArray(); }

			byte[] buffer = message.Data;
			ArraySegment<byte> seg = new ArraySegment<byte>(buffer);
			foreach (WebSocket ws in webSockets)
			{
				if (ws.State == WebSocketState.Open)
				{
					await ws.SendAsync(seg, WebSocketMessageType.Text, true, cancellationToken);
				}
			}
		}

		private readonly IDisposable _webServerBinder;
		private readonly HashSet<WebSocket> _webSockets;
		private readonly CancellationTokenSource _disposeCancellationTokenSource;
	}


	internal class UpgradeHandshake : IHttpWebSocketFeature
	{
		private readonly IHttpRequestFeature _request;
		private readonly IHttpResponseFeature _response;
		private readonly IHttpUpgradeFeature _upgradeFeature;
		private readonly WebSocketOptions _options;
		private bool? _isWebSocketRequest;

		public UpgradeHandshake(IHttpRequestFeature request, IHttpResponseFeature response, IHttpUpgradeFeature upgradeFeature, WebSocketOptions options)
		{
			_request = request;
			_response = response;
			_upgradeFeature = upgradeFeature;
			_options = options;
		}

		public bool IsWebSocketRequest
		{
			get
			{
				if (_isWebSocketRequest == null)
				{
					if (!_upgradeFeature.IsUpgradableRequest)
					{
						_isWebSocketRequest = false;
					}
					else
					{
						var requestHeaders = _request.Headers;
						var interestingHeaders = new List<KeyValuePair<string, string>>();
						foreach (var headerName in HandshakeHelpers.NeededHeaders)
						{
							foreach (var value in requestHeaders.GetCommaSeparatedValues(headerName))
							{
								interestingHeaders.Add(new KeyValuePair<string, string>(headerName, value));
							}
						}
						_isWebSocketRequest = HandshakeHelpers.CheckSupportedWebSocketRequest(_request.Method, interestingHeaders, requestHeaders);
					}
				}
				return _isWebSocketRequest.Value;
			}
		}

		public async Task<WebSocket> AcceptAsync(WebSocketAcceptContext acceptContext)
		{
			if (!IsWebSocketRequest)
			{
				throw new InvalidOperationException("Not a WebSocket request."); // TODO: LOC
			}

			string? subProtocol = null;
			if (acceptContext != null)
			{
				subProtocol = acceptContext.SubProtocol;
			}

			TimeSpan keepAliveInterval = _options.KeepAliveInterval;
			var advancedAcceptContext = acceptContext as ExtendedWebSocketAcceptContext;
			if (advancedAcceptContext != null)
			{
				if (advancedAcceptContext.KeepAliveInterval.HasValue)
				{
					keepAliveInterval = advancedAcceptContext.KeepAliveInterval.Value;
				}
			}

			string key = _request.Headers[HeaderNames.SecWebSocketKey];

			HandshakeHelpers.GenerateResponseHeaders(key, subProtocol, _response.Headers);

			Stream opaqueTransport = await _upgradeFeature.UpgradeAsync(); // Sets status code to 101

			return WebSocket.CreateFromStream(opaqueTransport, isServer: true, subProtocol: subProtocol, keepAliveInterval: keepAliveInterval);
		}
	}

	internal static class HandshakeHelpers
	{
		/// <summary>
		/// Gets request headers needed process the handshake on the server.
		/// </summary>
		public static readonly string[] NeededHeaders = new[]
		{
			HeaderNames.Upgrade,
			HeaderNames.Connection,
			HeaderNames.SecWebSocketKey,
			HeaderNames.SecWebSocketVersion
		};

		// "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
		// This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
		private static ReadOnlySpan<byte> EncodedWebSocketKey => new byte[]
		{
			(byte)'2', (byte)'5', (byte)'8', (byte)'E', (byte)'A', (byte)'F', (byte)'A', (byte)'5', (byte)'-',
			(byte)'E', (byte)'9', (byte)'1', (byte)'4', (byte)'-', (byte)'4', (byte)'7', (byte)'D', (byte)'A',
			(byte)'-', (byte)'9', (byte)'5', (byte)'C', (byte)'A', (byte)'-', (byte)'C', (byte)'5', (byte)'A',
			(byte)'B', (byte)'0', (byte)'D', (byte)'C', (byte)'8', (byte)'5', (byte)'B', (byte)'1', (byte)'1'
		};

		// Verify Method, Upgrade, Connection, version,  key, etc..
		public static bool CheckSupportedWebSocketRequest(string method, List<KeyValuePair<string, string>> interestingHeaders, IHeaderDictionary requestHeaders)
		{
			bool validUpgrade = false, validConnection = false, validKey = false, validVersion = false;

			if (!string.Equals("GET", method, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			foreach (var pair in interestingHeaders)
			{
				if (string.Equals(HeaderNames.Connection, pair.Key, StringComparison.OrdinalIgnoreCase))
				{
					if (string.Equals(HeaderNames.Upgrade, pair.Value, StringComparison.OrdinalIgnoreCase))
					{
						validConnection = true;
					}
				}
				else if (string.Equals(HeaderNames.Upgrade, pair.Key, StringComparison.OrdinalIgnoreCase))
				{
					if (string.Equals(Constants.Headers.UpgradeWebSocket, pair.Value, StringComparison.OrdinalIgnoreCase))
					{
						validUpgrade = true;
					}
				}
				else if (string.Equals(HeaderNames.SecWebSocketVersion, pair.Key, StringComparison.OrdinalIgnoreCase))
				{
					if (string.Equals(Constants.Headers.SupportedVersion, pair.Value, StringComparison.OrdinalIgnoreCase))
					{
						validVersion = true;
					}
				}
				else if (string.Equals(HeaderNames.SecWebSocketKey, pair.Key, StringComparison.OrdinalIgnoreCase))
				{
					validKey = IsRequestKeyValid(pair.Value);
				}
			}

			// WebSockets are long lived; so if the header values are valid we switch them out for the interned versions.
			if (validConnection && requestHeaders[HeaderNames.Connection].Count == 1)
			{
				requestHeaders[HeaderNames.Connection] = HeaderNames.Upgrade;
			}
			if (validUpgrade && requestHeaders[HeaderNames.Upgrade].Count == 1)
			{
				requestHeaders[HeaderNames.Upgrade] = Constants.Headers.UpgradeWebSocket;
			}
			if (validVersion && requestHeaders[HeaderNames.SecWebSocketVersion].Count == 1)
			{
				requestHeaders[HeaderNames.SecWebSocketVersion] = Constants.Headers.SupportedVersion;
			}

			return validConnection && validUpgrade && validVersion && validKey;
		}

		public static void GenerateResponseHeaders(string key, string? subProtocol, IHeaderDictionary headers)
		{
			headers[HeaderNames.Connection] = HeaderNames.Upgrade;
			headers[HeaderNames.Upgrade] = Constants.Headers.UpgradeWebSocket;
			headers[HeaderNames.SecWebSocketAccept] = CreateResponseKey(key);
			if (!string.IsNullOrWhiteSpace(subProtocol))
			{
				headers[HeaderNames.SecWebSocketProtocol] = subProtocol;
			}
		}

		/// <summary>
		/// Validates the Sec-WebSocket-Key request header
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool IsRequestKeyValid(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			Span<byte> temp = stackalloc byte[16];
			var success = Convert.TryFromBase64String(value, temp, out var written);
			return success && written == 16;
		}

		public static string CreateResponseKey(string requestKey)
		{
			// "The value of this header field is constructed by concatenating /key/, defined above in step 4
			// in Section 4.2.2, with the string "258EAFA5-E914-47DA-95CA-C5AB0DC85B11", taking the SHA-1 hash of
			// this concatenated value to obtain a 20-byte value and base64-encoding"
			// https://tools.ietf.org/html/rfc6455#section-4.2.2

			// requestKey is already verified to be small (24 bytes) by 'IsRequestKeyValid()' and everything is 1:1 mapping to UTF8 bytes
			// so this can be hardcoded to 60 bytes for the requestKey + static websocket string
			Span<byte> mergedBytes = stackalloc byte[60];
			Encoding.UTF8.GetBytes(requestKey, mergedBytes);
			EncodedWebSocketKey.CopyTo(mergedBytes.Slice(24));

			Span<byte> hashedBytes = stackalloc byte[20];
			var written = SHA1.HashData(mergedBytes, hashedBytes);
			if (written != 20)
			{
				throw new InvalidOperationException("Could not compute the hash for the 'Sec-WebSocket-Accept' header.");
			}

			return Convert.ToBase64String(hashedBytes);
		}
	}

	internal static class Constants
	{
		public static class Headers
		{
			public const string UpgradeWebSocket = "websocket";
			public const string SupportedVersion = "13";
		}
	}
}
