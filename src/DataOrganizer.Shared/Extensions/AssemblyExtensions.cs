using System.Reflection;

namespace DataOrganizer.Shared.Extensions;

/// <summary>
/// Reads the version of an assembly together with its suffix.
/// </summary>
public static class AssemblyExtensions
{
	#region Methods
	/// <summary>
	/// Returns the assembly version, including an optional suffix.
	/// </summary>
	public static string? GetVersionWithSuffix(this Assembly? assembly)
	{
		return assembly?
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion;
	}
	#endregion
}
