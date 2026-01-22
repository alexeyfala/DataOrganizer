using System;
using System.Linq;

namespace Shared.Extensions;

public static class AppDomainExtensions
{
	#region Methods
	/// <summary>
	/// Determines whether code is executed during NUnit unit tests.
	/// </summary>
	public static bool IsRunningFromNUnit(this AppDomain target)
	{
		return target
			.GetAssemblies()
			.Any(a => a.FullName?.StartsWith("nunit.framework", StringComparison.InvariantCultureIgnoreCase) == true);
	}
	#endregion Methods
}
