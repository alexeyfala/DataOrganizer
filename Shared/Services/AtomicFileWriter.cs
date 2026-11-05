using Shared.Interfaces;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using IFileSystem = System.IO.Abstractions.IFileSystem;

namespace Shared.Services;

/// <inheritdoc cref="IAtomicFileWriter" />
public sealed class AtomicFileWriter : IAtomicFileWriter
{
	#region Data
	/// <summary>
	/// Size of the buffer of the temporary file.
	/// </summary>
	private const int BufferSize = 4096;

	/// <summary>
	/// Extension of the file an atomic write is prepared in.
	/// </summary>
	private const string TemporaryFileExtension = ".tmp";

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;
	#endregion

	#region Constructors
	public AtomicFileWriter(IFileSystem fileSystem) => _fileSystem = fileSystem;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void WriteAllBytes(string filePath, byte[] bytes)
	{
		string temporaryFilePath = filePath + TemporaryFileExtension;

		try
		{
			// The bytes reach the disk before the swap: a rename that outruns them publishes an empty file.
			using (FileSystemStream stream = CreateTemporaryFile(temporaryFilePath, FileOptions.WriteThrough))
			{
				stream.Write(bytes);
			}

			MoveIntoPlace(temporaryFilePath, filePath);
		}
		catch
		{
			TryDeleteTemporaryFile(temporaryFilePath);

			throw;
		}
	}

	/// <inheritdoc />
	public async Task WriteAllBytesAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default)
	{
		string temporaryFilePath = filePath + TemporaryFileExtension;

		try
		{
			// The bytes reach the disk before the swap: a rename that outruns them publishes an empty file.
			await using (FileSystemStream stream = CreateTemporaryFile(
				temporaryFilePath,
				FileOptions.Asynchronous | FileOptions.WriteThrough))
			{
				await stream
					.WriteAsync(bytes, token)
					.ConfigureAwait(false);
			}

			MoveIntoPlace(temporaryFilePath, filePath);
		}
		catch
		{
			TryDeleteTemporaryFile(temporaryFilePath);

			throw;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates or overwrites the file an atomic write is prepared in.
	/// </summary>
	private FileSystemStream CreateTemporaryFile(string temporaryFilePath, FileOptions options)
	{
		return _fileSystem
			.FileStream
			.New(
				temporaryFilePath,
				FileMode.Create,
				FileAccess.Write,
				FileShare.None,
				BufferSize,
				options);
	}

	/// <summary>
	/// Puts the prepared file in the place of <paramref name="filePath" />.
	/// </summary>
	private void MoveIntoPlace(string temporaryFilePath, string filePath)
	{
		_fileSystem
			.File
			.Move(
				temporaryFilePath,
				filePath,
				overwrite: true);
	}

	/// <summary>
	/// Removes the file an atomic write was prepared in.
	/// </summary>
	private void TryDeleteTemporaryFile(string temporaryFilePath)
	{
		try
		{
			_fileSystem
				.File
				.Delete(temporaryFilePath);
		}
		catch
		{
			// The write is reported as it failed; a leftover file must not replace that reason.
		}
	}
	#endregion
}
