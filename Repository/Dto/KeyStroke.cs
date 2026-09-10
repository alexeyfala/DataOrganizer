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
	#endregion

	#region Methods
	/// <summary>
	/// The name of the key without the <c>Vc</c> prefix the library writes.
	/// </summary>
	public string GetKeyName()
	{
		string value = Code.ToString();

		return value.StartsWith("Vc") ? value[2..] : value;
	}
	#endregion
}
