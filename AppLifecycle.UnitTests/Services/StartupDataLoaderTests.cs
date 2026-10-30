using AppLifecycle.Services;
using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using GlobalHotkeys.Interfaces;
using GlobalHotkeys.Services;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using HierarchyStorage.Interfaces;
using Notifications.Interfaces;
using NSubstitute;
using Repository.Enums;
using Repository.Interfaces.Database;
using SharpHook.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using WindowContracts.Interfaces;

namespace AppLifecycle.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(StartupDataLoader)}"" type")]
internal class StartupDataLoaderTests
{
	#region Methods
	/// <summary>
	/// <see cref="StartupDataLoader.LoadAsync" />: a schema that does not match reports itself and ends the launch,
	/// instead of showing intact data as an empty hierarchy.
	/// </summary>
	[Test]
	public async Task LoadAsync_Ends_The_Launch_On_A_Schema_That_Does_Not_Match(
		[Values(DbConnectionStatus.SchemaTooOld, DbConnectionStatus.SchemaTooNew)] DbConnectionStatus status)
	{
		// Arrange
		string databaseFilePath = TestPath.Combine("Database", "DataOrganizer.sqlite");

		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		INoticeLauncher noticeLauncher = Substitute.For<INoticeLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(status);

			dbConnector
				.GetDbFilePath()
				.Returns(databaseFilePath);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(noticeLauncher);
		});

		StartupDataLoader sut = mock.Create<StartupDataLoader>();

		// Act
		LoadedHierarchy? result = await sut.LoadAsync();

		// Assert
		result
			.Should()
			.BeNull();

		await noticeLauncher
			.Received(1)
			.ShowStartupErrorAsync(databaseFilePath);

		await entityLoader
			.DidNotReceive()
			.LoadHierarchyAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="StartupDataLoader.LoadAsync" />: a file whose hotkeys could not be read comes with them, unlike an
	/// imported one.
	/// </summary>
	[Test]
	public async Task LoadAsync_Keeps_The_Hotkeys_That_Could_Not_Be_Read()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		HotkeyDto hotkey = new()
		{
			Code = KeyCode.VcUndefined,
			Id = Guid.NewGuid(),
			Index = 0,
			Mask = EventMask.LeftCtrl,
			OwnerId = file.Id
		};

		file
			.Hotkeys
			.Add(hotkey);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns(ItemDtoFactory.CreateLoadedHierarchy([file]));

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(entityLoader);

			builder
				.RegisterType<UnreadableHotkeysFinder>()
				.As<IUnreadableHotkeysFinder>();
		});

		StartupDataLoader sut = mock.Create<StartupDataLoader>();

		// Act
		LoadedHierarchy? result = await sut.LoadAsync();

		// Assert
		result!.Items
			.Should()
			.Equal(file);

		file.Hotkeys
			.Should()
			.Equal(hotkey);
	}

	/// <summary>
	/// <see cref="StartupDataLoader.LoadAsync" />: an unavailable database leaves the data unread, and the launch goes on
	/// with an empty tree.
	/// </summary>
	[Test]
	public async Task LoadAsync_Reads_Nothing_When_The_Database_Is_Unavailable()
	{
		// Arrange
		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.FileUnreadable);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(entityLoader);
		});

		StartupDataLoader sut = mock.Create<StartupDataLoader>();

		// Act
		LoadedHierarchy? result = await sut.LoadAsync();

		// Assert
		result!.Items
			.Should()
			.BeEmpty();

		await entityLoader
			.DidNotReceive()
			.LoadHierarchyAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="StartupDataLoader.LoadAsync" />: the hotkeys that could not be read are reported.
	/// </summary>
	[Test]
	public async Task LoadAsync_Reports_The_Hotkeys_That_Could_Not_Be_Read()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		file
			.Hotkeys
			.Add(new()
			{
				Code = KeyCode.VcUndefined,
				Id = Guid.NewGuid(),
				Index = 0,
				Mask = EventMask.LeftCtrl,
				OwnerId = file.Id
			});

		INotificationService notification = Substitute.For<INotificationService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns(ItemDtoFactory.CreateLoadedHierarchy([file]));

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(notification);

			builder
				.RegisterType<UnreadableHotkeysFinder>()
				.As<IUnreadableHotkeysFinder>();
		});

		StartupDataLoader sut = mock.Create<StartupDataLoader>();

		// Act
		await sut.LoadAsync();

		// Assert
		notification
			.Received(1)
			.ShowToast(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="StartupDataLoader.LoadAsync" />: the launch goes on with an empty tree when the data cannot be read.
	/// </summary>
	[Test]
	public async Task LoadAsync_Returns_An_Empty_Tree_When_The_Data_Cannot_Be_Read()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns((LoadedHierarchy?)null);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(entityLoader);
		});

		StartupDataLoader sut = mock.Create<StartupDataLoader>();

		// Act
		LoadedHierarchy? result = await sut.LoadAsync();

		// Assert
		result!.Items
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="StartupDataLoader.LoadAsync" />: connects to the database and returns the tree read from it.
	/// </summary>
	[Test]
	public async Task LoadAsync_Returns_The_Tree_Read_From_The_Database()
	{
		// Arrange
		LoadedHierarchy loaded = ItemDtoFactory.CreateLoadedHierarchy([.. ItemDtoFactory.CreateFolderDtos(3)]);

		IDbConnector dbConnector = Substitute.For<IDbConnector>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns(loaded);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(entityLoader);
		});

		StartupDataLoader sut = mock.Create<StartupDataLoader>();

		// Act
		LoadedHierarchy? result = await sut.LoadAsync();

		// Assert
		result
			.Should()
			.BeSameAs(loaded);

		await dbConnector
			.Received(1)
			.ConnectAsync(Arg.Any<CancellationToken>());
	}
	#endregion
}
