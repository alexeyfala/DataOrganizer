using System.Security.Cryptography;

namespace DataOrganizer.DTO.Execution;

public sealed class TrackChangesParameters : ExecuteFileParametersBase
{
	#region Properties
	/// <summary>
	/// Algorithm the initial hash and every later hash of the file are taken with.
	/// </summary>
	public static HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA256;

	/// <summary>
	/// File name.
	/// </summary>
	public required string FileName { get; init; }

	/// <summary>
	/// Path to the file.
	/// </summary>
	public required string FilePath { get; init; }

	/// <summary>
	/// Hash of the contents the file starts with, taken with <see cref="HashAlgorithm" />.
	/// </summary>
	public required byte[] PreviousHash { get; init; }
	#endregion
}
