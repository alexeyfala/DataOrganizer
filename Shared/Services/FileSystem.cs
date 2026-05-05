using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Services;

public sealed class FileSystem : IFileSystem
{
	#region Data
	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;
	#endregion

	#region Constructors
	public FileSystem(IJsonSerializerWrapper jsonSerializer) => _jsonSerializer = jsonSerializer;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void CreateDirectory(string directoryPath) => Directory.CreateDirectory(directoryPath);

	/// <inheritdoc />
	public void DeleteDirectory(string directoryPath, bool recursive = true)
	{
		Directory.Delete(directoryPath, recursive);
	}

	/// <inheritdoc />
	public void DeleteDirectoryRecursively(
		string directoryPath,
		bool removeFileReadOnlySign = false)
	{
		if (removeFileReadOnlySign)
		{
			Directory
				.EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories)
				.ForEach(x => SetFileReadOnly(x, false));
		}

		Directory.Delete(directoryPath, recursive: true);
	}

	/// <inheritdoc />
	public void EraseAndDeleteFile(
		string filePath,
		in int bufferSize = IFileSystem.DefaultBufferSize,
		in int passes = IFileSystem.DefaultPassCount)
	{
		EraseFile(filePath, bufferSize, passes);

		File.Delete(filePath);
	}

	/// <inheritdoc />
	public void EraseFile(
		string filePath,
		in int bufferSize = IFileSystem.DefaultBufferSize,
		in int passes = IFileSystem.DefaultPassCount)
	{
		using RandomNumberGenerator generator = RandomNumberGenerator.Create();

		byte[] buffer = new byte[bufferSize];

		long fileLength = new FileInfo(filePath).Length;

		for (int i = 0; i < passes; i++)
		{
			using FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Write);

			long position = 0;

			while (position < fileLength)
			{
				generator.GetBytes(buffer);

				stream.Write(buffer, 0, (int)Math.Min(buffer.Length, fileLength - position));

				position += buffer.Length;
			}
		}
	}

	/// <inheritdoc />
	[return: NotNullIfNotNull(nameof(directoryPath))]
	public bool IsDirectoryExists([NotNullWhen(true)] string? directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
		{
			return false;
		}

		if (Path.GetDirectoryName(directoryPath) is not { } parent)
		{
			return true;
		}

		try
		{
			string name = Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

			if (string.IsNullOrEmpty(name))
			{
				return true;
			}

			return Directory
				.EnumerateDirectories(parent)
				.Any(x => string.Equals(Path.GetFileName(x), name, StringComparison.Ordinal));
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);

			return false;
		}
	}

	/// <inheritdoc />
	public bool IsFileExists([NotNullWhen(true)] string? filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return false;
		}

		if (Path.GetDirectoryName(filePath) is not { } parent)
		{
			return true;
		}

		try
		{
			string name = Path.GetFileName(filePath);

			if (string.IsNullOrEmpty(name))
			{
				return false;
			}

			return Directory
				.EnumerateFiles(parent)
				.Any(x => string.Equals(Path.GetFileName(x), name, StringComparison.Ordinal));
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);

			return false;
		}
	}

	/// <inheritdoc />
	public bool IsFileLocked(string filePath)
	{
		try
		{
			using FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
		}
		catch (IOException e) when ((e.HResult & 0x0000FFFF) == 32)
		{
			return true;
		}

		return false;
	}

	/// <inheritdoc />
	public Stream OpenRead(string filePath) => File.Open(
		filePath,
		FileMode.Open,
		FileAccess.Read,
		FileShare.ReadWrite);

	/// <inheritdoc />
	public string ReadAllText(string filePath) => File.ReadAllText(filePath);

	/// <inheritdoc />
	public void SerializeToJsonFile<T>(
		T value,
		string filePath,
		bool isHide)
	{
		if (IsFileExists(filePath))
		{
			SetFileHidden(filePath, false);
		}

		if (Path.GetDirectoryName(filePath) is { } parentDirectory)
		{
			Directory.CreateDirectory(parentDirectory);
		}

		File.WriteAllText(filePath, _jsonSerializer.Serialize(value, AppUtils.JsonOptions));

		if (!isHide)
		{
			return;
		}

		SetFileHidden(filePath, true);
	}

	/// <inheritdoc />
	public void SetFileHidden(string filePath, bool value)
	{
		const FileAttributes attribute = FileAttributes.Hidden;

		if (value)
		{
			AddFileAttributes(filePath, attribute);
		}
		else
		{
			RemoveFileAttributes(filePath, attribute);
		}
	}

	/// <inheritdoc />
	public void SetFileReadOnly(string filePath, bool value)
	{
		const FileAttributes attribute = FileAttributes.ReadOnly;

		if (value)
		{
			AddFileAttributes(filePath, attribute);
		}
		else
		{
			RemoveFileAttributes(filePath, attribute);
		}
	}

	/// <inheritdoc />
	public async Task WaitWhileFileIsLockedAsync(
		string filePath,
		ILogger? logger = null,
		CancellationToken token = default)
	{
		while (IsFileExists(filePath) && IsFileLocked(filePath))
		{
			logger?.LogWarning($@"File ""{filePath}"" is locked by another process, waiting it to be released.");

			await Task
				.Delay(500, token)
				.ConfigureAwait(false);
		}
	}

	/// <inheritdoc />
	public Task WriteAllBytesAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default)
	{
		return File.WriteAllBytesAsync(filePath, bytes, token);
	}

	/// <inheritdoc />
	public void WriteAllText(string filePath, string? contents) => File.WriteAllText(filePath, contents);
	#endregion

	#region Service
	/// <summary>
	/// Adds attributes <see cref="FileAttributes" /> to a file.
	/// </summary>
	private static void AddFileAttributes(string filePath, in FileAttributes value)
	{
		FileAttributes attributes = File.GetAttributes(filePath);

		attributes |= value;

		File.SetAttributes(filePath, attributes);
	}

	/// <summary>
	/// Removes the <see cref="FileAttributes" /> attribute from a file.
	/// </summary>
	private static void RemoveFileAttributes(string filePath, in FileAttributes value)
	{
		FileAttributes attributes = File.GetAttributes(filePath);

		attributes &= ~value;

		File.SetAttributes(filePath, attributes);
	}
	#endregion
}
