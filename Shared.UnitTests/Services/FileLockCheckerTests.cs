using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Testably.Abstractions.Testing;
using TestSupport.Common;
using TimeProvider = System.TimeProvider;

namespace Shared.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(FileLockChecker)}"" type")]
internal class FileLockCheckerTests
{
	#region Data
	/// <summary>
	/// Path of the checked file.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine("a.txt");

	/// <summary>
	/// Mirrors the pause between two checks of a locked file.
	/// </summary>
	private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="FileLockChecker.IsLocked" />: a file nobody holds is free.
	/// </summary>
	[Test]
	public void IsLocked_Is_False_For_A_Free_File()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(FilePath, [1]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPlatform platform = Substitute.For<IPlatform>();

			platform
				.IsWindows
				.Returns(OperatingSystem.IsWindows());

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(platform);
		});

		FileLockChecker sut = mock.Create<FileLockChecker>();

		// Act
		bool result = sut.IsLocked(FilePath);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileLockChecker.IsLocked" />: a holder that shares the file locks it only on Windows, where no exclusive
	/// open passes another handle.
	/// </summary>
	[Test]
	public void IsLocked_Follows_A_Sharing_Holder_On_Windows_Only()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(FilePath, [1]);

		using FileSystemStream holder = files
			.File
			.Open(
				FilePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPlatform platform = Substitute.For<IPlatform>();

			platform
				.IsWindows
				.Returns(OperatingSystem.IsWindows());

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(platform);
		});

		FileLockChecker sut = mock.Create<FileLockChecker>();

		// Act
		bool result = sut.IsLocked(FilePath);

		// Assert
		// Unix sees only an exclusive lock of the holder.
		result
			.Should()
			.Be(OperatingSystem.IsWindows());
	}

	/// <summary>
	/// <see cref="FileLockChecker.IsLocked" />: a holder that keeps the file to itself locks it.
	/// </summary>
	[Test]
	public void IsLocked_Is_True_While_A_Holder_Keeps_The_File_To_Itself()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(FilePath, [1]);

		using FileSystemStream holder = files
			.File
			.Open(
				FilePath,
				FileMode.Open,
				FileAccess.ReadWrite,
				FileShare.None);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPlatform platform = Substitute.For<IPlatform>();

			platform
				.IsWindows
				.Returns(OperatingSystem.IsWindows());

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(platform);
		});

		FileLockChecker sut = mock.Create<FileLockChecker>();

		// Act
		bool result = sut.IsLocked(FilePath);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FileLockChecker.WaitUntilUnlockedAsync" />: a cancel while the file is still held ends the wait with
	/// <c>False</c>.
	/// </summary>
	[Test]
	public async Task WaitUntilUnlockedAsync_Returns_False_When_Cancelled_While_Locked()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(FilePath, [1]);

		using FileSystemStream holder = files
			.File
			.Open(
				FilePath,
				FileMode.Open,
				FileAccess.ReadWrite,
				FileShare.None);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPlatform platform = Substitute.For<IPlatform>();

			platform
				.IsWindows
				.Returns(OperatingSystem.IsWindows());

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(platform);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileLockChecker sut = mock.Create<FileLockChecker>();

		Task<bool> waiting = sut
			.WaitUntilUnlockedAsync(FilePath, cancellation.Token)
			.AsTask();

		// Act
		await cancellation.CancelAsync();

		// Assert
		(await waiting.WaitAsync(TimeSpan.FromSeconds(10)))
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileLockChecker.WaitUntilUnlockedAsync" />: a file that is gone is not waited for.
	/// </summary>
	[Test]
	public async Task WaitUntilUnlockedAsync_Returns_True_For_A_Missing_File()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		FileLockChecker sut = mock.Create<FileLockChecker>();

		// Act
		bool result = await sut.WaitUntilUnlockedAsync(FilePath);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FileLockChecker.WaitUntilUnlockedAsync" />: the file is checked again after a pause and the wait ends
	/// once the holder has let it go.
	/// </summary>
	[Test]
	public async Task WaitUntilUnlockedAsync_Waits_For_The_Holder_To_Let_Go()
	{
		// Arrange
		FakeTimeProvider time = new();

		MockFileSystem files = new();

		files
			.File
			.WriteAllBytes(FilePath, [1]);

		FileSystemStream holder = files
			.File
			.Open(
				FilePath,
				FileMode.Open,
				FileAccess.ReadWrite,
				FileShare.None);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPlatform platform = Substitute.For<IPlatform>();

			platform
				.IsWindows
				.Returns(OperatingSystem.IsWindows());

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(platform);

			builder.RegisterInstance<TimeProvider>(time);
		});

		FileLockChecker sut = mock.Create<FileLockChecker>();

		Task<bool> waiting = sut
			.WaitUntilUnlockedAsync(FilePath)
			.AsTask();

		await holder.DisposeAsync();

		await Task.Run(() => time.Advance(PollInterval - TimeSpan.FromTicks(1)));

		bool isCompletedBeforeThePause = waiting.IsCompleted;

		// Act
		await Task.Run(() => time.Advance(TimeSpan.FromTicks(1)));

		// Assert
		isCompletedBeforeThePause
			.Should()
			.BeFalse();

		waiting.IsCompleted
			.Should()
			.BeTrue();

		(await waiting)
			.Should()
			.BeTrue();
	}
	#endregion
}
