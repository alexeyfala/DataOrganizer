using System;
using System.Diagnostics;

namespace Repository.Dto;

/// <summary>
/// The contents of a file together with the verdict on whether they are usable.
/// </summary>
[DebuggerDisplay($"{nameof(IsValid)} = {{{nameof(IsValid)}}}")]
public sealed class ValidatedContents
{
	#region Properties
	/// <summary>
	/// The bytes of the file; empty when they could not be read.
	/// </summary>
	public byte[] Contents { get; init; } = [];

	/// <summary>
	/// Identifier of the file the contents belong to.
	/// </summary>
	public Guid Id { get; init; }

	/// <summary>
	/// <c>True</c> when <see cref="Contents" /> is valid.
	/// </summary>
	public bool IsValid { get; init; }
	#endregion
}
