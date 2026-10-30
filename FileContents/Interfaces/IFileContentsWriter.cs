using FileContents.Dto;
using FileContents.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileContents.Interfaces;

/// <summary>
/// Writes the contents of a file to the database: encrypts the protected ones and dates every edit.
/// </summary>
public interface IFileContentsWriter
{
	#region Methods
	/// <summary>
	/// Writes the contents of a file, encrypted with the session key of the keeper when one is given and they are not
	/// empty; the caller keeps <paramref name="contents" /> and wipes them.
	/// </summary>
	Task<FileContentsWriting> WriteAsync(
		Guid fileId,
		Guid? keeperId,
		byte[] contents,
		FileContentsChange change,
		CancellationToken token = default);
	#endregion
}
