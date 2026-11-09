using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Settings.Services;
using Shared.Interfaces;
using Shared.Services;
using System.IO;
using System.IO.Abstractions;
using Testably.Abstractions.Testing;
using TestSupport.Common;

namespace Settings.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(JsonFileStore)}"" type")]
internal class JsonFileStoreTests
{
	#region Data
	/// <summary>
	/// Name of the folder the settings live in.
	/// </summary>
	private const string FolderName = "settings";

	/// <summary>
	/// Path of the file the value is kept in.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine(FolderName, "sample.json");

	/// <summary>
	/// Folder the value is kept in.
	/// </summary>
	private static readonly string Folder = TestPath.Combine(FolderName);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="JsonFileStore.Read{T}" />: a file that cannot be parsed gives the default value.
	/// </summary>
	[Test]
	public void Read_Gives_The_Default_For_A_Malformed_File()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllText(FilePath, """{ "Name": """);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<IFileSystem>(files);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		JsonFileStore sut = mock.Create<JsonFileStore>();

		// Act
		JsonSample? result = sut.Read<JsonSample>(FilePath);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="JsonFileStore.Read{T}" />: a missing file gives the default value.
	/// </summary>
	[Test]
	public void Read_Gives_The_Default_For_A_Missing_File()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		JsonFileStore sut = mock.Create<JsonFileStore>();

		// Act
		JsonSample? result = sut.Read<JsonSample>(FilePath);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="JsonFileStore.Write{T}" />, <see cref="JsonFileStore.Read{T}" />: the value read back is the one written,
	/// and the folder of the file is created for it.
	/// </summary>
	[Test]
	public void Write_Keeps_The_Value_For_The_Read()
	{
		// Arrange
		JsonSample sample = new()
		{
			Name = "name",
			Number = 42
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		JsonFileStore sut = mock.Create<JsonFileStore>();

		// Act
		sut.Write(sample, FilePath);

		// Assert
		sut.Read<JsonSample>(FilePath)
			.Should()
			.BeEquivalentTo(sample);
	}

	/// <summary>
	/// <see cref="JsonFileStore.Write{T}" />: a hidden file is overwritten and stays visible.
	/// </summary>
	[Test]
	public void Write_Overwrites_A_Hidden_File()
	{
		// Arrange
		JsonSample sample = new()
		{
			Name = "name",
			Number = 42
		};

		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Folder);

		files
			.File
			.WriteAllText(FilePath, "{}");

		files
			.File
			.SetAttributes(FilePath, FileAttributes.Hidden);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance<IFileSystem>(files);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		JsonFileStore sut = mock.Create<JsonFileStore>();

		// Act
		sut.Write(sample, FilePath);

		// Assert
		sut.Read<JsonSample>(FilePath)
			.Should()
			.BeEquivalentTo(sample);

		files.File.GetAttributes(FilePath).HasFlag(FileAttributes.Hidden)
			.Should()
			.BeFalse();
	}
	#endregion
}

/// <summary>
/// Sample value kept in a Json file.
/// </summary>
public sealed class JsonSample
{
	public string Name { get; set; } = string.Empty;

	public int Number { get; set; }
}
