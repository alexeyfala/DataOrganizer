using System.Text;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Defaults applied to text handling across the application.
/// </summary>
internal static class TextDefaults
{
	#region Data
	/// <inheritdoc cref="Encoding.UTF8" />
	public static Encoding Encoding { get; } = Encoding.UTF8;
	#endregion
}
