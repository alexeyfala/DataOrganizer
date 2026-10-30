using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Shared.Extensions;

[ExcludeFromCodeCoverage(Justification = "Detects the test host itself")]
public static class AppDomainExtensions
{
	#region Methods
	/// <summary>
	/// Determines whether code is executed during NUnit unit tests.
	/// </summary>
	public static bool IsRunningFromNUnit(this AppDomain appDomain)
	{
		return appDomain
			.GetAssemblies()
			.Any(x => x.FullName?.StartsWith("nunit.framework", StringComparison.InvariantCultureIgnoreCase) == true);
	}
	#endregion
}
