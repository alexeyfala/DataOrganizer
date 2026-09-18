using System;
using System.Net.Http;

namespace TestSupport.Http;

/// <summary>
/// Data of a request received by <see cref="StubHttpMessageHandler" />.
/// </summary>
public sealed class StubHttpRequest
{
	#region Properties
	/// <summary>
	/// Body of the request.
	/// </summary>
	public required string? Content { get; init; }

	/// <summary>
	/// Method of the request.
	/// </summary>
	public required HttpMethod Method { get; init; }

	/// <summary>
	/// Address of the request.
	/// </summary>
	public required Uri? Uri { get; init; }
	#endregion
}
