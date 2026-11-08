using AppInstance.Interfaces;
using AppInstance.Services;
using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using System;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using Testably.Abstractions.Testing;

namespace AppInstance.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(InstanceRegistry)}"" type")]
internal class InstanceRegistryTests
{
	#region Data
	/// <summary>
	/// Name of the folder that holds the lock files of the slots.
	/// </summary>
	private const string LocksDirectoryName = ".instancelocks";

	/// <summary>
	/// Number of slots tried before the registry gives up.
	/// </summary>
	private const int MaxInstances = 1024;

	/// <summary>
	/// Lock file of the first slot.
	/// </summary>
	private static readonly string FirstSlotPath = Path.Combine(
		IAppEnvironment.GetAppDataDirectoryPath(),
		LocksDirectoryName,
		"1.lock");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="InstanceRegistry(IFileSystem)" />: when every slot is held, the instance takes the first number
	/// without a lock, after trying each slot once.
	/// </summary>
	[Test]
	public void Constructor_Falls_Back_To_The_First_Slot_When_Every_Slot_Is_Held()
	{
		// Arrange
		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// No slot file opens, as when other instances hold them all.
			_ = files
				.Intercept
				.Creating(FileSystemTypes.File, _ => throw new IOException());

			builder.RegisterInstance<IFileSystem>(files);
		});

		// Act
		InstanceRegistry sut = mock.Create<InstanceRegistry>();

		// Assert
		sut.InstanceNumber
			.Should()
			.Be(1);

		files.Statistics.FileStream.Methods
			.Should()
			.HaveCount(MaxInstances)
			.And
			.OnlyContain(x => x.Name == nameof(IFileStreamFactory.New));
	}

	/// <summary>
	/// <see cref="InstanceRegistry(IFileSystem)" />: when the folder of the locks cannot be made, the instance takes the
	/// first number without trying a slot.
	/// </summary>
	[Test]
	public void Constructor_Falls_Back_To_The_First_Slot_When_The_Locks_Folder_Cannot_Be_Made()
	{
		// Arrange
		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			_ = files
				.Intercept
				.Creating(FileSystemTypes.Directory, _ => throw new UnauthorizedAccessException());

			builder.RegisterInstance<IFileSystem>(files);
		});

		// Act
		InstanceRegistry sut = mock.Create<InstanceRegistry>();

		// Assert
		sut.InstanceNumber
			.Should()
			.Be(1);

		files.Statistics.FileStream.Methods
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="InstanceRegistry(IFileSystem)" />: on an empty disk the instance takes the first slot and holds it shut,
	/// even to a reader that shares it.
	/// </summary>
	[Test]
	public void Constructor_Holds_The_First_Slot_On_An_Empty_Disk()
	{
		// Arrange
		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IFileSystem>(files));

		// Act
		InstanceRegistry sut = mock.Create<InstanceRegistry>();

		// Assert
		sut.InstanceNumber
			.Should()
			.Be(1);

		// A shared read gets through any hold but an exclusive one.
		Action read = () => files.FileStream.New(
			FirstSlotPath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.ReadWrite);

		read
			.Should()
			.Throw<IOException>();
	}

	/// <summary>
	/// <see cref="InstanceRegistry(IFileSystem)" />: a slot another instance holds is skipped for the next one.
	/// </summary>
	[Test]
	public void Constructor_Skips_A_Slot_Another_Instance_Holds()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Path.GetDirectoryName(FirstSlotPath)!);

		using FileSystemStream held = files.FileStream.New(
			FirstSlotPath,
			FileMode.CreateNew,
			FileAccess.ReadWrite,
			FileShare.None);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IFileSystem>(files));

		// Act
		InstanceRegistry sut = mock.Create<InstanceRegistry>();

		// Assert
		sut.InstanceNumber
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="InstanceRegistry(IFileSystem)" />: the slot file an exited instance left behind is taken again, and its
	/// old contents give way to the id of this process.
	/// </summary>
	[Test]
	public void Constructor_Takes_A_Slot_File_Left_Behind_And_Writes_The_Process_Id()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(Path.GetDirectoryName(FirstSlotPath)!);

		files
			.File
			.WriteAllText(FirstSlotPath, "4294967295 of an instance long gone");

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IFileSystem>(files));

		// Act
		InstanceRegistry sut = mock.Create<InstanceRegistry>();

		// Assert
		sut.InstanceNumber
			.Should()
			.Be(1);

		// The slot is held while the instance runs, so the file is read after it ends.
		sut.Dispose();

		files.File.ReadAllText(FirstSlotPath)
			.Should()
			.Be(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
	}

	/// <summary>
	/// <see cref="InstanceRegistry.Dispose" />: the slot is freed for the next instance.
	/// </summary>
	[Test]
	public void Dispose_Frees_The_Slot()
	{
		// Arrange
		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IFileSystem>(files));

		InstanceRegistry sut = mock.Create<InstanceRegistry>();

		// Act
		sut.Dispose();

		// Assert
		Func<FileSystemStream> takeAgain = () => files.FileStream.New(
			FirstSlotPath,
			FileMode.Open,
			FileAccess.ReadWrite,
			FileShare.None);

		takeAgain
			.Should()
			.NotThrow();
	}
	#endregion
}
