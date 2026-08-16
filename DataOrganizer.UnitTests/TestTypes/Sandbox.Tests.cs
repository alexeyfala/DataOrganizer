using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Interfaces;
using DataOrganizer.Services.Execution;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shared.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(Sandbox)}"" type")]
internal class SandboxTests
{
	#region Methods
	/// <summary>
	/// <see cref="Sandbox.EraseAsync" />: overwrites and deletes the folder left by the previous session.
	/// </summary>
	[Test]
	public async Task EraseAsync_Erases_The_Folder()
	{
		// Arrange
		IFileSystem fileSystem = CreateFileSystem();

		using AutoMock mock = AutoMock.GetLoose();

		Sandbox sut = CreateSut(mock, fileSystem, new FakeTimeProvider());

		// Act
		await sut.EraseAsync();

		// Assert
		fileSystem
			.Received(1)
			.EraseAndDeleteDirectory(DirectoryPath);
	}

	/// <summary>
	/// <see cref="Sandbox.EraseAsync" />: a locked folder is taken again after a pause.
	/// </summary>
	[Test]
	public async Task EraseAsync_Repeats_The_Attempt_For_A_Locked_Folder()
	{
		// Arrange
		IFileSystem fileSystem = CreateFileSystem();

		int attempts = 0;

		fileSystem
			.When(x => x.EraseAndDeleteDirectory(DirectoryPath))
			.Do(_ =>
			{
				attempts++;

				if (attempts == 1)
				{
					throw new IOException();
				}
			});

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose();

		Sandbox sut = CreateSut(mock, fileSystem, time);

		// Act
		Task task = sut.EraseAsync();

		time.Advance(TimeSpan.FromSeconds(1));

		await task;

		// Assert
		attempts
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="Sandbox.EraseAsync" />: a folder that does not exist is left alone.
	/// </summary>
	[Test]
	public async Task EraseAsync_Skips_A_Missing_Folder()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose();

		Sandbox sut = CreateSut(mock, fileSystem, new FakeTimeProvider());

		// Act
		await sut.EraseAsync();

		// Assert
		fileSystem
			.DidNotReceive()
			.EraseAndDeleteDirectory(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="Sandbox.GetFileDirectoryPath" />: every file gets a folder of its own inside the sandbox.
	/// </summary>
	[Test]
	public void GetFileDirectoryPath_Points_Inside_The_Sandbox()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		using AutoMock mock = AutoMock.GetLoose();

		Sandbox sut = CreateSut(mock, Substitute.For<IFileSystem>(), new FakeTimeProvider());

		// Act
		string directoryPath = sut.GetFileDirectoryPath(fileId);

		// Assert
		directoryPath
			.Should()
			.Be(Path.Combine(DirectoryPath, fileId.ToString()));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Path of the sandbox used in the tests.
	/// </summary>
	private static readonly string DirectoryPath = Path.Combine(Path.GetTempPath(), "Sandbox");

	/// <summary>
	/// Creates a file system in which the sandbox exists.
	/// </summary>
	private static IFileSystem CreateFileSystem()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		fileSystem
			.IsDirectoryExists(DirectoryPath)
			.Returns(true);

		return fileSystem;
	}

	/// <summary>
	/// Builds the service over the given file system and clock.
	/// </summary>
	private static Sandbox CreateSut(
		AutoMock mock,
		IFileSystem fileSystem,
		TimeProvider timeProvider)
	{
		IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

		appEnvironment
			.SandboxDirectoryPath
			.Returns(DirectoryPath);

		return mock.Create<Sandbox>(
			TypedParameter.From(appEnvironment),
			TypedParameter.From(fileSystem),
			TypedParameter.From(timeProvider));
	}
	#endregion
}
