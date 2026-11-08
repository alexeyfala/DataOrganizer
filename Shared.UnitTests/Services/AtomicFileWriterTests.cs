using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Shared.Services;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Testably.Abstractions.Testing;
using TestSupport.Common;

namespace Shared.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(AtomicFileWriter)}"" type")]
internal class AtomicFileWriterTests
{
	#region Data
	/// <summary>
	/// Name of the folder the written file lives in.
	/// </summary>
	private const string FolderName = "store";

	/// <summary>
	/// Path of the written file.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine(FolderName, "a.bin");

	/// <summary>
	/// Folder the written file lives in.
	/// </summary>
	private static readonly string Folder = TestPath.Combine(FolderName);

	/// <summary>
	/// Path of the file the write is prepared in.
	/// </summary>
	private static readonly string TemporaryFilePath = TestPath.Combine(FolderName, "a.bin.tmp");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="AtomicFileWriter.WriteAllBytes" />: a buffer the caller wipes after the write leaves the file as written.
	/// </summary>
	[Test]
	public void WriteAllBytes_Keeps_The_Bytes_The_Caller_Wipes_Afterwards()
	{
		// Arrange
		byte[] bytes = [4, 5, 6];

		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IFileSystem>(files));

		AtomicFileWriter sut = mock.Create<AtomicFileWriter>();

		// Act
		sut.WriteAllBytes(FilePath, bytes);

		Array.Clear(bytes);

		// Assert
		files.File.ReadAllBytes(FilePath)
			.Should()
			.Equal(4, 5, 6);
	}

	/// <summary>
	/// <see cref="AtomicFileWriter.WriteAllBytes" />: a write cut short leaves the old contents and no temporary file.
	/// </summary>
	[Test]
	public void WriteAllBytes_Leaves_The_Old_Contents_When_The_Write_Fails()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllBytes(FilePath, [1, 2, 3]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			_ = files
				.Intercept
				.Changing(
					FileSystemTypes.File,
					_ => throw new IOException(),
					predicate: x => x.Path == TemporaryFilePath);

			builder.RegisterInstance<IFileSystem>(files);
		});

		AtomicFileWriter sut = mock.Create<AtomicFileWriter>();

		// Act
		Action act = () => sut.WriteAllBytes(FilePath, [4, 5, 6]);

		// Assert
		act
			.Should()
			.Throw<IOException>();

		files.File.ReadAllBytes(FilePath)
			.Should()
			.Equal(1, 2, 3);

		files.File.Exists(TemporaryFilePath)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="AtomicFileWriter.WriteAllBytes" />: the new bytes take the place of the old contents, and the temporary
	/// file is gone.
	/// </summary>
	[Test]
	public void WriteAllBytes_Replaces_The_Contents()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllBytes(FilePath, [1, 2, 3]);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IFileSystem>(files));

		AtomicFileWriter sut = mock.Create<AtomicFileWriter>();

		// Act
		sut.WriteAllBytes(FilePath, [4, 5, 6]);

		// Assert
		files.File.ReadAllBytes(FilePath)
			.Should()
			.Equal(4, 5, 6);

		files.File.Exists(TemporaryFilePath)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="AtomicFileWriter.WriteAllBytesAsync" />: a buffer the caller wipes after the write leaves the file as
	/// written.
	/// </summary>
	[Test]
	public async Task WriteAllBytesAsync_Keeps_The_Bytes_The_Caller_Wipes_Afterwards()
	{
		// Arrange
		byte[] bytes = [4, 5, 6];

		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IFileSystem>(files));

		AtomicFileWriter sut = mock.Create<AtomicFileWriter>();

		// Act
		await sut.WriteAllBytesAsync(FilePath, bytes);

		Array.Clear(bytes);

		// Assert
		files.File.ReadAllBytes(FilePath)
			.Should()
			.Equal(4, 5, 6);
	}

	/// <summary>
	/// <see cref="AtomicFileWriter.WriteAllBytesAsync" />: a write cut short leaves the old contents and no temporary file.
	/// </summary>
	[Test]
	public async Task WriteAllBytesAsync_Leaves_The_Old_Contents_When_The_Write_Fails()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllBytes(FilePath, [1, 2, 3]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			_ = files
				.Intercept
				.Changing(
					FileSystemTypes.File,
					_ => throw new IOException(),
					predicate: x => x.Path == TemporaryFilePath);

			builder.RegisterInstance<IFileSystem>(files);
		});

		AtomicFileWriter sut = mock.Create<AtomicFileWriter>();

		// Act
		Func<Task> act = () => sut.WriteAllBytesAsync(FilePath, [4, 5, 6]);

		// Assert
		await act
			.Should()
			.ThrowAsync<IOException>();

		files.File.ReadAllBytes(FilePath)
			.Should()
			.Equal(1, 2, 3);

		files.File.Exists(TemporaryFilePath)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="AtomicFileWriter.WriteAllBytesAsync" />: the new bytes take the place of the old contents, and the
	/// temporary file is gone.
	/// </summary>
	[Test]
	public async Task WriteAllBytesAsync_Replaces_The_Contents()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllBytes(FilePath, [1, 2, 3]);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IFileSystem>(files));

		AtomicFileWriter sut = mock.Create<AtomicFileWriter>();

		// Act
		await sut.WriteAllBytesAsync(FilePath, [4, 5, 6]);

		// Assert
		files.File.ReadAllBytes(FilePath)
			.Should()
			.Equal(4, 5, 6);

		files.File.Exists(TemporaryFilePath)
			.Should()
			.BeFalse();
	}
	#endregion
}
