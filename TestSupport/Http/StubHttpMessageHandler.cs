using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TestSupport.Http;

/// <summary>
/// <see cref="HttpMessageHandler" /> answering requests from the test instead of the network.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
	#region Properties
	/// <summary>
	/// Requests received by the handler in the order they were sent.
	/// </summary>
	public List<StubHttpRequest> Requests { get; } = [];
	#endregion

	#region Constructors
	public StubHttpMessageHandler(Func<StubHttpRequest, HttpResponseMessage> responder) => _responder = responder;
	#endregion

	#region Data
	/// <summary>
	/// Produces the answer to a received request.
	/// </summary>
	private readonly Func<StubHttpRequest, HttpResponseMessage> _responder;
	#endregion

	#region Methods
	/// <summary>
	/// Creates a <see cref="HttpResponseMessage" /> with the given status code and content.
	/// </summary>
	public static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content = "") => new(statusCode)
	{
		Content = new StringContent(content)
	};

	/// <summary>
	/// Creates a handler answering every request with the given status code and content.
	/// </summary>
	public static StubHttpMessageHandler FromStatusCode(HttpStatusCode statusCode, string content = "")
	{
		return new(_ => CreateResponse(statusCode, content));
	}
	#endregion

	#region Helpers
	/// <inheritdoc />
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
	{
		StubHttpRequest received = new()
		{
			// The content is read here because the caller disposes it right after the request is sent.
			Content = request.Content is { } content
				? await content.ReadAsStringAsync(token).ConfigureAwait(false)
				: null,
			Method = request.Method,
			Uri = request.RequestUri
		};

		Requests.Add(received);

		HttpResponseMessage response = _responder(received);

		response.RequestMessage = request;

		return response;
	}
	#endregion
}
