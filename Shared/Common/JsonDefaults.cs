using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Common;

/// <summary>
/// Defaults applied to Json serialization across the application.
/// </summary>
public static class JsonDefaults
{
	#region Properties
	/// <summary>
	/// Json serialization options.
	/// </summary>
	[ExcludeFromCodeCoverage(Justification = "Settings of the format, which a test would only copy")]
	public static JsonSerializerOptions Options { get; } = new()
	{
		WriteIndented = true,
		ReferenceHandler = ReferenceHandler.IgnoreCycles
	};
	#endregion
}
