using System;
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
	/// Identifier.
	/// </summary>
	public Guid Id { get; init; }

	/// <summary>
	/// <c>True</c> when <see cref="Contents" /> is valid.
	/// </summary>
	public bool IsValid { get; init; }
	#endregion
}
