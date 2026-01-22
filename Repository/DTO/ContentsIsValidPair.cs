using System.Diagnostics;

namespace Repository.DTO;

/// <summary>
/// The pair of <see cref="Contents" /> and <see cref="IsValid" /> values.
/// </summary>
[DebuggerDisplay($"{nameof(IsValid)} = {{{nameof(IsValid)}}}")]
public sealed class ContentsIsValidPair
{
	#region Properties
	/// <summary>
	/// Contents.
	/// </summary>
	public byte[] Contents { get; init; } = [];

	/// <summary>
	/// Returns <c>True</c> if <see cref="Contents" /> is valid.
	/// </summary>
	public bool IsValid { get; init; }
	#endregion
}
