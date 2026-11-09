using AwesomeAssertions;
using Shared.Helpers;
using System;

namespace Shared.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(TransportSecurity)}"" type")]
internal class TransportSecurityTests
{
	#region Methods
	/// <summary>
	/// <see cref="TransportSecurity.IsSecureOrLoopback" />: verifies only HTTPS and an HTTP loopback address pass.
	/// </summary>
	[TestCase("https://host.com/api", ExpectedResult = true)]
	[TestCase("https://host.com:8443/api", ExpectedResult = true)]
	[TestCase("http://localhost:5000/callback", ExpectedResult = true)]
	[TestCase("http://127.0.0.1/callback", ExpectedResult = true)]
	[TestCase("http://[::1]/callback", ExpectedResult = true)]
	[TestCase("http://host.com/api", ExpectedResult = false)]
	[TestCase("ftp://localhost/file", ExpectedResult = false)]
	[TestCase("file:///C:/Windows/System32/calc.exe", ExpectedResult = false)]
	public bool IsSecureOrLoopback_Accepts_Https_And_Http_Loopback_Only(string url)
	{
		// Act
		return TransportSecurity.IsSecureOrLoopback(new Uri(url));
	}

	/// <summary>
	/// <see cref="TransportSecurity.IsSecureOrLoopback" />: verifies a missing address is rejected.
	/// </summary>
	[Test]
	public void IsSecureOrLoopback_Rejects_Missing_Address()
	{
		// Act
		bool result = TransportSecurity.IsSecureOrLoopback(null);

		// Assert
		result
			.Should()
			.BeFalse();
	}
	#endregion
}
