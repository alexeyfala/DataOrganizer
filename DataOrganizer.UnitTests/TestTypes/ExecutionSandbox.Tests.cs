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

[TestFixture(Description = $@"Tests of ""{nameof(ExecutionSandbox)}"" type")]
internal class ExecutionSandboxTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExecutionSandbox.EraseAsync" />: overwrites and deletes the folder left by the previous session.
	/// </summary>
	[Test]
	public async Task EraseAsync_Erases_The_Folder()
	{
		// Arrange
		IFileSystem fileSystem = CreateFileSystem();

		using AutoMock mock = AutoMock.GetLoose();

		ExecutionSandbox sut = CreateSut(mock, fileSystem, new FakeTimeProvider());

		// Act
		await sut.EraseAsync();

		// Assert
		fileSystem
			.Received(1)
			.EraseAndDeleteDirectory(DirectoryPath);
	}

	/// <summary>
	/// <see cref="ExecutionSandbox.EraseAsync" />: a locked folder is taken again after a pause.
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

		ExecutionSandbox sut = CreateSut(mock, fileSystem, time);

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
	/// <see cref="ExecutionSandbox.EraseAsync" />: a folder that does not exist is left alone.
	/// </summary>
	[Test]
	public async Task EraseAsync_Skips_A_Missing_Folder()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose();

		ExecutionSandbox sut = CreateSut(mock, fileSystem, new FakeTimeProvider());

		// Act
		await sut.EraseAsync();

		// Assert
		fileSystem
			.DidNotReceive()
			.EraseAndDeleteDirectory(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionSandbox.GetFileDirectoryPath" />: every file gets a folder of its own inside the sandbox.
	/// </summary>
	[Test]
	public void GetFileDirectoryPath_Points_Inside_The_Sandbox()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		using AutoMock mock = AutoMock.GetLoose();

		ExecutionSandbox sut = CreateSut(mock, Substitute.For<IFileSystem>(), new FakeTimeProvider());

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
	private static ExecutionSandbox CreateSut(
		AutoMock mock,
		IFileSystem fileSystem,
		TimeProvider timeProvider)
	{
		IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

		appEnvironment
			.SandboxDirectoryPath
			.Returns(DirectoryPath);

		return mock.Create<ExecutionSandbox>(
			TypedParameter.From(appEnvironment),
			TypedParameter.From(fileSystem),
			TypedParameter.From(timeProvider));
	}
	#endregion
}
