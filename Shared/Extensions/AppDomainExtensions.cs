using System;
using System.Linq;
using System.Reflection;

namespace Shared.Extensions;

public static class AppDomainExtensions
{
	#region Methods
	/// <summary>
	/// Searches an assembly in domain.
	/// </summary>
	public static Assembly? GetAssemblyByName(this AppDomain target, string name)
	{
		return target
			.GetAssemblies()
			.SingleOrDefault(assembly => assembly.GetName().Name == name);
	}

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
