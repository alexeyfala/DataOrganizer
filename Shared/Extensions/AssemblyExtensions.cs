using System.Reflection;

namespace Shared.Extensions;

public static class AssemblyExtensions
{
	#region Methods
	/// <summary>
	/// Returns the assembly version, including an optional suffix.
	/// </summary>
	public static string? GetVersionWithSuffix(this Assembly? target)
	{
		return target?
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion;
	}
	#endregion
}
