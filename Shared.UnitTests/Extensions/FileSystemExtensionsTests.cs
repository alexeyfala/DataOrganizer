using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Extensions;
using System;
using System.IO;
using System.IO.Abstractions;
using Testably.Abstractions.Testing;
using TestSupport.Common;

namespace Shared.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(FileSystemExtensions)}"" type")]
internal class FileSystemExtensionsTests
{
	#region Data
	/// <summary>
	/// Name of the folder the checked file lives in.
	/// </summary>
	private const string FolderName = "dir";

	/// <summary>
	/// Path of the file the attributes are set on.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine("a.txt");

	/// <summary>
	/// Folder the checked file lives in.
	/// </summary>
	private static readonly string Folder = TestPath.Combine(FolderName);

	/// <summary>
	/// Path of the checked file.
	/// </summary>
	private static readonly string NestedFilePath = TestPath.Combine(FolderName, "a.txt");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="FileSystemExtensions.DirectoryExists" />: a folder named in another case is not found.
	/// </summary>
	[Test]
	public void DirectoryExists_Is_False_For_The_Name_In_Another_Case()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		// Act
		bool result = files.DirectoryExists(TestPath.Combine(FolderName.ToUpperInvariant()));

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.DirectoryExists" />: a folder whose parent cannot be listed is not found.
	/// </summary>
	[Test]
	public void DirectoryExists_Is_False_When_The_Parent_Cannot_Be_Listed()
	{
		// Arrange
		// The mock cannot fail a listing, so a substitute stands in for the file system.
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		fileSystem
			.Directory
			.Exists(Folder)
			.Returns(true);

		fileSystem
			.Directory
			.EnumerateDirectories(TestPath.Root)
			.Throws(new UnauthorizedAccessException());

		// Act
		bool result = fileSystem.DirectoryExists(Folder);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.DirectoryExists" />: no path means no folder.
	/// </summary>
	[Test]
	public void DirectoryExists_Is_False_Without_A_Path([Values(null, "")] string? path)
	{
		// Arrange
		MockFileSystem files = new();

		// Act
		bool result = files.DirectoryExists(path);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.DirectoryExists" />: a folder is found by its exact name, with or without trailing
	/// separators.
	/// </summary>
	[Test]
	public void DirectoryExists_Is_True_For_The_Exact_Name([Values(0, 1, 2)] int trailingSeparators)
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		string path = Folder + new string(Path.DirectorySeparatorChar, trailingSeparators);

		// Act
		bool result = files.DirectoryExists(path);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.DirectoryExists" />: the root has no name to compare and is found.
	/// </summary>
	[Test]
	public void DirectoryExists_Is_True_For_The_Root()
	{
		// Arrange
		MockFileSystem files = new();

		// Act
		bool result = files.DirectoryExists(TestPath.Root);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.FileExists" />: a file named in another case is not found.
	/// </summary>
	[Test]
	public void FileExists_Is_False_For_The_Name_In_Another_Case()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllBytes(NestedFilePath, [1]);

		// Act
		bool result = files.FileExists(TestPath.Combine(FolderName, "A.TXT"));

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.FileExists" />: a file whose folder cannot be listed is not found.
	/// </summary>
	[Test]
	public void FileExists_Is_False_When_The_Folder_Cannot_Be_Listed()
	{
		// Arrange
		// The mock cannot fail a listing, so a substitute stands in for the file system.
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		fileSystem
			.File
			.Exists(NestedFilePath)
			.Returns(true);

		fileSystem
			.Directory
			.EnumerateFiles(Folder)
			.Throws(new UnauthorizedAccessException());

		// Act
		bool result = fileSystem.FileExists(NestedFilePath);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.FileExists" />: no path means no file.
	/// </summary>
	[Test]
	public void FileExists_Is_False_Without_A_Path([Values(null, "")] string? path)
	{
		// Arrange
		MockFileSystem files = new();

		// Act
		bool result = files.FileExists(path);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.FileExists" />: a file is found by its exact name among the other files of its folder.
	/// </summary>
	[Test]
	public void FileExists_Is_True_For_The_Exact_Name()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllBytes(NestedFilePath, [1]);

		files
			.File
			.WriteAllBytes(TestPath.Combine(FolderName, "b.txt"), [2]);

		// Act
		bool result = files.FileExists(NestedFilePath);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.SetFileHidden" />: the sign is added and the other attributes stay.
	/// </summary>
	[Test]
	public void SetFileHidden_Adds_The_Sign_And_Keeps_The_Others()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(FilePath, [1]);

		files
			.File
			.SetAttributes(FilePath, FileAttributes.ReadOnly);

		// Act
		files.SetFileHidden(FilePath, true);

		// Assert
		FileAttributes attributes = files
			.File
			.GetAttributes(FilePath);

		attributes.HasFlag(FileAttributes.ReadOnly)
			.Should()
			.BeTrue();

		// Linux keeps no such sign: a file is hidden there by the dot in its name.
		attributes.HasFlag(FileAttributes.Hidden)
			.Should()
			.Be(!OperatingSystem.IsLinux());
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.SetFileHidden" />: the sign is removed and the other attributes stay.
	/// </summary>
	[Test]
	public void SetFileHidden_Removes_The_Sign_And_Keeps_The_Others()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(FilePath, [1]);

		files
			.File
			.SetAttributes(FilePath, FileAttributes.Hidden | FileAttributes.ReadOnly);

		// Act
		files.SetFileHidden(FilePath, false);

		// Assert
		FileAttributes attributes = files
			.File
			.GetAttributes(FilePath);

		attributes.HasFlag(FileAttributes.ReadOnly)
			.Should()
			.BeTrue();

		attributes.HasFlag(FileAttributes.Hidden)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.SetFileReadOnly" />: the sign is added and the other attributes stay.
	/// </summary>
	[Test]
	public void SetFileReadOnly_Adds_The_Sign_And_Keeps_The_Others()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(FilePath, [1]);

		files
			.File
			.SetAttributes(FilePath, FileAttributes.Hidden);

		// Act
		files.SetFileReadOnly(FilePath, true);

		// Assert
		FileAttributes attributes = files
			.File
			.GetAttributes(FilePath);

		attributes.HasFlag(FileAttributes.ReadOnly)
			.Should()
			.BeTrue();

		// Linux keeps no such sign: a file is hidden there by the dot in its name.
		attributes.HasFlag(FileAttributes.Hidden)
			.Should()
			.Be(!OperatingSystem.IsLinux());
	}

	/// <summary>
	/// <see cref="FileSystemExtensions.SetFileReadOnly" />: the sign is removed and the other attributes stay.
	/// </summary>
	[Test]
	public void SetFileReadOnly_Removes_The_Sign_And_Keeps_The_Others()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(FilePath, [1]);

		files
			.File
			.SetAttributes(FilePath, FileAttributes.Hidden | FileAttributes.ReadOnly);

		// Act
		files.SetFileReadOnly(FilePath, false);

		// Assert
		FileAttributes attributes = files
			.File
			.GetAttributes(FilePath);

		attributes.HasFlag(FileAttributes.ReadOnly)
			.Should()
			.BeFalse();

		// Linux keeps no such sign: a file is hidden there by the dot in its name.
		attributes.HasFlag(FileAttributes.Hidden)
			.Should()
			.Be(!OperatingSystem.IsLinux());
	}
	#endregion
}
