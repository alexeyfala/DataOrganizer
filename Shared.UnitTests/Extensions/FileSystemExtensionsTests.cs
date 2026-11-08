using AwesomeAssertions;
using Shared.Extensions;
using System;
using System.IO;
using Testably.Abstractions.Testing;
using TestSupport.Common;

namespace Shared.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(FileSystemExtensions)}"" type")]
internal class FileSystemExtensionsTests
{
	#region Data
	/// <summary>
	/// Path of the file the attributes are set on.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine("a.txt");
	#endregion

	#region Methods
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
