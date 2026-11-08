using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Testably.Abstractions.Testing;
using TestSupport.Common;

namespace Shared.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(FileEraser)}"" type")]
internal class FileEraserTests
{
	#region Data
	/// <summary>
	/// Length of the contents, larger than the buffer of the erase, so it takes more than one round.
	/// </summary>
	private const int ContentsLength = 5000;

	/// <summary>
	/// Name of the folder the erased files live in.
	/// </summary>
	private const string FolderName = "sandbox";

	/// <summary>
	/// Name of the subfolder of <see cref="FolderName" />.
	/// </summary>
	private const string SubfolderName = "nested";

	/// <summary>
	/// Path of a file right in the folder.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine(FolderName, "a.bin");

	/// <summary>
	/// Folder the erased files live in.
	/// </summary>
	private static readonly string Folder = TestPath.Combine(FolderName);

	/// <summary>
	/// Path of a file in the subfolder.
	/// </summary>
	private static readonly string NestedFilePath = TestPath.Combine(FolderName, SubfolderName, "b.bin");

	/// <summary>
	/// Subfolder of the folder.
	/// </summary>
	private static readonly string Subfolder = TestPath.Combine(FolderName, SubfolderName);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="FileEraser.EraseAndDeleteDirectory" />: a read-only file is erased and goes with the folder.
	/// </summary>
	[Test]
	public void EraseAndDeleteDirectory_Clears_The_Read_Only_Sign()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllBytes(FilePath, RandomValues.CreateBytes(ContentsLength));

		files
			.File
			.SetAttributes(FilePath, FileAttributes.ReadOnly);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IFileSystem>(files));

		FileEraser sut = mock.Create<FileEraser>();

		// Act
		Action act = () => sut.EraseAndDeleteDirectory(Folder);

		// Assert
		act
			.Should()
			.NotThrow();

		files.Directory.Exists(Folder)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileEraser.EraseAndDeleteDirectory" />: a file that cannot be overwritten does not stop the others, and
	/// its failure is reported once the folder is gone.
	/// </summary>
	[Test]
	public void EraseAndDeleteDirectory_Erases_The_Other_Files_When_One_Fails()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(ContentsLength);

		byte[]? erased = null;

		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Subfolder);

		files
			.File
			.WriteAllBytes(FilePath, RandomValues.CreateBytes(ContentsLength));

		files
			.File
			.WriteAllBytes(NestedFilePath, contents);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			_ = files
				.Intercept
				.Changing(
					FileSystemTypes.File,
					_ => throw new IOException(),
					predicate: x => x.Path == FilePath);

			// The first deletion comes before anything is gone, so the file can still be read there.
			_ = files
				.Intercept
				.Deleting(FileSystemTypes.DirectoryOrFile, _ => erased ??= files.File.ReadAllBytes(NestedFilePath));

			builder.RegisterInstance<IFileSystem>(files);
		});

		FileEraser sut = mock.Create<FileEraser>();

		// Act
		Action act = () => sut.EraseAndDeleteDirectory(Folder);

		// Assert
		act
			.Should()
			.Throw<AggregateException>()
			.Which
			.InnerExceptions
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<IOException>();

		erased
			.Should()
			.HaveCount(ContentsLength)
			.And
			.NotEqual(contents);

		files.Directory.Exists(Folder)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileEraser.EraseAndDeleteDirectory" />: the files of the folder and of its subfolders are overwritten
	/// before the folder is deleted.
	/// </summary>
	[Test]
	public void EraseAndDeleteDirectory_Overwrites_Every_File_Of_The_Subfolders()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(ContentsLength);

		byte[] nestedContents = RandomValues.CreateBytes(ContentsLength);

		byte[][]? erased = null;

		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Subfolder);

		files
			.File
			.WriteAllBytes(FilePath, contents);

		files
			.File
			.WriteAllBytes(NestedFilePath, nestedContents);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// The first deletion comes before anything is gone, so the files can still be read there.
			_ = files
				.Intercept
				.Deleting(FileSystemTypes.DirectoryOrFile, _ => erased ??= [files.File.ReadAllBytes(FilePath), files.File.ReadAllBytes(NestedFilePath)]);

			builder.RegisterInstance<IFileSystem>(files);
		});

		FileEraser sut = mock.Create<FileEraser>();

		// Act
		sut.EraseAndDeleteDirectory(Folder);

		// Assert
		erased
			.Should()
			.HaveCount(2);

		erased![0]
			.Should()
			.HaveCount(ContentsLength)
			.And
			.NotEqual(contents);

		erased[1]
			.Should()
			.HaveCount(ContentsLength)
			.And
			.NotEqual(nestedContents);

		files.Directory.Exists(Folder)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileEraser.EraseAndDeleteDirectory" />: a folder that cannot be deleted is reported after its files are
	/// erased.
	/// </summary>
	[Test]
	public void EraseAndDeleteDirectory_Reports_A_Folder_That_Cannot_Be_Deleted()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllBytes(FilePath, RandomValues.CreateBytes(ContentsLength));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			_ = files
				.Intercept
				.Deleting(
					FileSystemTypes.Directory,
					_ => throw new IOException(),
					predicate: x => x.Path == Folder);

			builder.RegisterInstance<IFileSystem>(files);
		});

		FileEraser sut = mock.Create<FileEraser>();

		// Act
		Action act = () => sut.EraseAndDeleteDirectory(Folder);

		// Assert
		act
			.Should()
			.Throw<AggregateException>()
			.Which
			.InnerExceptions
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<IOException>();
	}

	/// <summary>
	/// <see cref="FileEraser.EraseAndDeleteFile" />: the contents are overwritten with random bytes, so two equal files
	/// get different ones.
	/// </summary>
	[Test]
	public void EraseAndDeleteFile_Overwrites_With_Random_Bytes()
	{
		// Arrange
		string secondFilePath = TestPath.Combine(FolderName, "c.bin");

		byte[] contents = RandomValues.CreateBytes(ContentsLength);

		Dictionary<string, byte[]> erased = [];

		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllBytes(FilePath, contents);

		files
			.File
			.WriteAllBytes(secondFilePath, contents);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			_ = files
				.Intercept
				.Deleting(FileSystemTypes.File, x => erased[x.Path] = files.File.ReadAllBytes(x.Path));

			builder.RegisterInstance<IFileSystem>(files);
		});

		FileEraser sut = mock.Create<FileEraser>();

		// Act
		sut.EraseAndDeleteFile(FilePath);

		sut.EraseAndDeleteFile(secondFilePath);

		// Assert
		erased[FilePath]
			.Should()
			.NotEqual(erased[secondFilePath]);
	}

	/// <summary>
	/// <see cref="FileEraser.EraseAndDeleteFile" />: the contents are overwritten before the file is deleted.
	/// </summary>
	[Test]
	public void EraseAndDeleteFile_Overwrites_The_Contents_Before_The_Delete()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(ContentsLength);

		byte[]? erased = null;

		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllBytes(FilePath, contents);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			_ = files
				.Intercept
				.Deleting(FileSystemTypes.File, x => erased = files.File.ReadAllBytes(x.Path));

			builder.RegisterInstance<IFileSystem>(files);
		});

		FileEraser sut = mock.Create<FileEraser>();

		// Act
		sut.EraseAndDeleteFile(FilePath);

		// Assert
		erased
			.Should()
			.HaveCount(ContentsLength)
			.And
			.NotEqual(contents);

		files.File.Exists(FilePath)
			.Should()
			.BeFalse();
	}
	#endregion
}
