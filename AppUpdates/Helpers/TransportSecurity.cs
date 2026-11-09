using System;

namespace Shared.Helpers;

/// <summary>
/// Transport-security checks for outgoing connections.
/// </summary>
public static class TransportSecurity
{
	/// <summary>
	/// <c>True</c> when the destination is safe to carry credentials:
	/// HTTPS, or an HTTP loopback address (localhost, 127.0.0.1, ::1).
	/// </summary>
	public static bool IsSecureOrLoopback(Uri? uri)
	{
		return uri is not null
			&& (uri.Scheme == Uri.UriSchemeHttps
				|| (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback));
	}
}
