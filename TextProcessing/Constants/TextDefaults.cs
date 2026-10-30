using System.Text;

namespace TextProcessing.Constants;

/// <summary>
/// Defaults applied to text handling across the application.
/// </summary>
public static class TextDefaults
{
	#region Data
	/// <inheritdoc cref="Encoding.UTF8" />
	public static Encoding Encoding { get; } = Encoding.UTF8;
	#endregion
}
