using SharpHook.Data;
using System.Diagnostics;

namespace Repository.Dto;

/// <summary>
/// A single key press together with the modifiers held down with it.
/// </summary>
[DebuggerDisplay($"{nameof(Code)} = {{{nameof(Code)}}}, {nameof(Mask)} = {{{nameof(Mask)}}}")]
public readonly record struct KeyStroke
{
	#region Properties
	/// <inheritdoc cref="KeyCode" />
	public required KeyCode Code { get; init; }

	/// <inheritdoc cref="EventMask" />
	public required EventMask Mask { get; init; }
	#endregion Properties

	#region Methods
	/// <summary>
	/// Converts a key code to its string representation.
	/// </summary>
	public string ConvertToKey()
	{
		string value = Code.ToString();

		return value.StartsWith("Vc") ? value[2..] : value;
	}
	#endregion
}
