using AwesomeAssertions;
using Shared.Services;
using System;
using System.IO;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FileSystem)}"" type")]
internal class FileSystemTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="FileSystem.EraseAndDeleteFile" />.
	/// </summary>
	[Test]
	public void EraseAndDeleteFile_Removes_The_File_From_Disk()
	{
		// Arrange
		FileSystem sut = CreateSut();

		string filePath = Path.Combine(Path.GetTempPath(), $"EraseDelete_{Guid.NewGuid():N}.bin");

		File.WriteAllBytes(filePath, new byte[256]);

		// Act
		sut.EraseAndDeleteFile(filePath);

		// Assert
		File.Exists(filePath)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="FileSystem.EraseFile" />.
	/// </summary>
	[Test]
	public void EraseFile_Overwrites_File_Contents_So_They_Differ_From_Original()
	{
		// Arrange
		FileSystem sut = CreateSut();

		string filePath = Path.Combine(Path.GetTempPath(), $"Erase_{Guid.NewGuid():N}.bin");

		byte[] original = new byte[1024];

		for (int i = 0; i < original.Length; i++)
		{
			original[i] = 0xAB;
		}

		File.WriteAllBytes(filePath, original);

		try
		{
			// Act
			sut.EraseFile(filePath);

			// Assert
			byte[] erased = File.ReadAllBytes(filePath);

			erased.Length
				.Should()
				.Be(original.Length);

			erased
				.Should()
				.NotEqual(original);
		}
		finally
		{
			File.Delete(filePath);
		}
	}

	/// <summary>
	/// Test of <see cref="FileSystem.IsDirectoryExists" />.
	/// </summary>
	[Test]
	public void IsDirectoryExists_Returns_False_For_Null_Or_Empty_Path()
	{
		// Arrange
		FileSystem sut = CreateSut();

		// Act + Assert
		sut.IsDirectoryExists(null)
			.Should()
			.BeFalse();

		sut.IsDirectoryExists(string.Empty)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="FileSystem.IsDirectoryExists" />.
	/// </summary>
	[Test]
	public void IsDirectoryExists_Returns_False_When_Case_Does_Not_Match()
	{
		// Arrange
		FileSystem sut = CreateSut();

		string directoryName = $"Mixed_Case_Dir_{Guid.NewGuid():N}";

		string directoryPath = Path.Combine(Path.GetTempPath(), directoryName);

		Directory.CreateDirectory(directoryPath);

		string lowerCasePath = Path.Combine(Path.GetTempPath(), directoryName.ToLowerInvariant());

		try
		{
			// Act
			bool result = sut.IsDirectoryExists(lowerCasePath);

			// Assert
			result
				.Should()
				.BeFalse();
		}
		finally
		{
			Directory.Delete(directoryPath, recursive: true);
		}
	}

	/// <summary>
	/// Test of <see cref="FileSystem.IsDirectoryExists" />.
	/// </summary>
	[Test]
	public void IsDirectoryExists_Returns_True_For_Existing_Directory_With_Exact_Case()
	{
		// Arrange
		FileSystem sut = CreateSut();

		string directoryPath = Path.Combine(Path.GetTempPath(), $"DirSample_{Guid.NewGuid():N}");

		Directory.CreateDirectory(directoryPath);

		try
		{
			// Act
			bool result = sut.IsDirectoryExists(directoryPath);

			// Assert
			result
				.Should()
				.BeTrue();
		}
		finally
		{
			Directory.Delete(directoryPath, recursive: true);
		}
	}

	/// <summary>
	/// Test of <see cref="FileSystem.IsFileExists" />.
	/// </summary>
	[Test]
	public void IsFileExists_Returns_False_For_Null_Or_Empty_Path()
	{
		// Arrange
		FileSystem sut = CreateSut();

		// Act + Assert
		sut.IsFileExists(null)
			.Should()
			.BeFalse();

		sut.IsFileExists(string.Empty)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="FileSystem.IsFileExists" />.
	/// </summary>
	[Test]
	public void IsFileExists_Returns_False_When_Case_Does_Not_Match()
	{
		// Arrange
		FileSystem sut = CreateSut();

		string fileName = $"Mixed_Case_{Guid.NewGuid():N}.txt";

		string filePath = Path.Combine(Path.GetTempPath(), fileName);

		File.WriteAllText(filePath, "x");

		string lowerCaseFilePath = Path.Combine(Path.GetTempPath(), fileName.ToLowerInvariant());

		try
		{
			// Act
			bool result = sut.IsFileExists(lowerCaseFilePath);

			// Assert
			result
				.Should()
				.BeFalse();
		}
		finally
		{
			File.Delete(filePath);
		}
	}

	/// <summary>
	/// Test of <see cref="FileSystem.IsFileExists" />.
	/// </summary>
	[Test]
	public void IsFileExists_Returns_True_For_Existing_File_With_Exact_Case()
	{
		// Arrange
		FileSystem sut = CreateSut();

		string filePath = Path.Combine(Path.GetTempPath(), $"Sample_{Guid.NewGuid():N}.txt");

		File.WriteAllText(filePath, "x");

		try
		{
			// Act
			bool result = sut.IsFileExists(filePath);

			// Assert
			result
				.Should()
				.BeTrue();
		}
		finally
		{
			File.Delete(filePath);
		}
	}

	/// <summary>
	/// Test of <see cref="FileSystem.IsFileLocked" />.
	/// </summary>
	[Test]
	public void IsFileLocked_Returns_True_While_Stream_Is_Held_And_False_Once_Closed()
	{
		// Arrange
		FileSystem sut = CreateSut();

		string filePath = Path.Combine(Path.GetTempPath(), $"Locked_{Guid.NewGuid():N}.bin");

		File.WriteAllBytes(filePath, [0x01, 0x02, 0x03]);

		try
		{
			FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);

			// Act + Assert
			sut.IsFileLocked(filePath)
				.Should()
				.BeTrue();

			stream.Dispose();

			sut.IsFileLocked(filePath)
				.Should()
				.BeFalse();
		}
		finally
		{
			File.Delete(filePath);
		}
	}

	/// <summary>
	/// Test of <see cref="FileSystem.SerializeToJsonFile{T}" />.
	/// </summary>
	[Test]
	public void SerializeToJsonFile_Creates_Parent_Directory_And_Writes_Json_Content()
	{
		// Arrange
		FileSystem sut = CreateSut();

		string parentDirectory = Path.Combine(Path.GetTempPath(), $"SerializeJson_{Guid.NewGuid():N}");

		string filePath = Path.Combine(parentDirectory, "settings.json");

		Sample value = new() { Name = "alpha", Number = 42 };

		try
		{
			// Act
			sut.SerializeToJsonFile(value, filePath, isHide: false);

			// Assert
			Directory.Exists(parentDirectory)
				.Should()
				.BeTrue();

			File.Exists(filePath)
				.Should()
				.BeTrue();

			string contents = File.ReadAllText(filePath);

			contents
				.Should()
				.Contain("alpha");

			contents
				.Should()
				.Contain("42");
		}
		finally
		{
			Directory.Delete(parentDirectory, recursive: true);
		}
	}

	/// <summary>
	/// Test of <see cref="FileSystem.SerializeToJsonFile{T}" />.
	/// </summary>
	[Test]
	[Platform("Win")]
	public void SerializeToJsonFile_Sets_Hidden_Attribute_When_IsHide_Is_True()
	{
		// Arrange
		FileSystem sut = CreateSut();

		string parentDirectory = Path.Combine(Path.GetTempPath(), $"SerializeHidden_{Guid.NewGuid():N}");

		string filePath = Path.Combine(parentDirectory, "secret.json");

		Sample value = new() { Name = "hidden", Number = 1 };

		try
		{
			// Act
			sut.SerializeToJsonFile(value, filePath, isHide: true);

			// Assert
			File.GetAttributes(filePath)
				.HasFlag(FileAttributes.Hidden)
				.Should()
				.BeTrue();
		}
		finally
		{
			File.SetAttributes(filePath, FileAttributes.Normal);

			Directory.Delete(parentDirectory, recursive: true);
		}
	}
	#endregion

	#region Service
	/// <summary>
	/// Builds a fresh <see cref="FileSystem" /> with a real <see cref="JsonSerializerWrapper" /> as dependency.
	/// </summary>
	private static FileSystem CreateSut() => new(new JsonSerializerWrapper());

	/// <summary>
	/// Sample DTO for <see cref="FileSystem.SerializeToJsonFile{T}" /> tests.
	/// </summary>
	private sealed class Sample
	{
		public string Name { get; init; } = string.Empty;

		public int Number { get; init; }
	}
	#endregion
}
