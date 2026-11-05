using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using IFileSystem = System.IO.Abstractions.IFileSystem;

namespace Shared.Services;

/// <inheritdoc cref="IFileEraser" />
public sealed class FileEraser : IFileEraser
{
	#region Data
	/// <summary>
	/// Largest buffer the random values are written from.
	/// </summary>
	private const int BufferSize = 4096;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;
	#endregion

	#region Constructors
	public FileEraser(IFileSystem fileSystem) => _fileSystem = fileSystem;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void EraseAndDeleteDirectory(string directoryPath)
	{
		List<Exception> failures = [];

		foreach (string filePath in _fileSystem
			.Directory
			.EnumerateFiles(
				directoryPath,
				"*",
				SearchOption.AllDirectories))
		{
			try
			{
				_fileSystem.SetFileReadOnly(filePath, false);

				EraseFile(filePath);
			}
			catch (Exception ex)
			{
				failures.Add(ex);
			}
		}

		try
		{
			_fileSystem
				.Directory
				.Delete(directoryPath, recursive: true);
		}
		catch (Exception ex)
		{
			failures.Add(ex);
		}

		if (failures.Count > 0)
		{
			throw new AggregateException(failures);
		}
	}

	/// <inheritdoc />
	public void EraseAndDeleteFile(string filePath)
	{
		EraseFile(filePath);

		_fileSystem
			.File
			.Delete(filePath);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Overwrites the file contents with random values in a single pass.
	/// </summary>
	/// <remarks>
	/// Multiple passes are an HDD legacy: on an SSD with wear leveling a rewrite is not guaranteed to reach the same physical blocks anyway.
	/// </remarks>
	private void EraseFile(string filePath)
	{
		long fileLength = _fileSystem
			.FileInfo
			.New(filePath)
			.Length;

		// A buffer of the erase size is rented to keep it out of the large object heap.
		byte[] buffer = ArrayPool<byte>
			.Shared
			.Rent((int)Math.Min(fileLength, BufferSize));

		try
		{
			using FileSystemStream stream = _fileSystem
				.File
				.Open(
					filePath,
					FileMode.Open,
					FileAccess.Write);

			long position = 0;

			while (position < fileLength)
			{
				int count = (int)Math.Min(buffer.Length, fileLength - position);

				RandomNumberGenerator.Fill(buffer.AsSpan(0, count));

				stream.Write(
					buffer,
					0,
					count);

				position += count;
			}

			// Without this the pass may never reach the medium: deleting the file afterwards
			// lets the system drop the pending writes and leave the original content in place.
			stream.Flush(flushToDisk: true);
		}
		finally
		{
			ArrayPool<byte>
				.Shared
				.Return(buffer);
		}
	}
	#endregion
}
