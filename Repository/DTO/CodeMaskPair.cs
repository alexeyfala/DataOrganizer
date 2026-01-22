using SharpHook.Data;
using System.Diagnostics;

namespace Repository.DTO;

[DebuggerDisplay($"{nameof(Code)} = {{{nameof(Code)}}}, {nameof(Mask)} = {{{nameof(Mask)}}}")]
public readonly record struct CodeMaskPair
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
