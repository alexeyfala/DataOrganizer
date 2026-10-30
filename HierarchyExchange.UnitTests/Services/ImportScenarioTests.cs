using AppInstance.Interfaces;
using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Clipboard.Interfaces;
using DataProtection.Interfaces;
using FileExecution.Interfaces;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using HierarchyExchange.Dto;
using HierarchyExchange.Enums;
using HierarchyExchange.Interfaces;
using HierarchyExchange.Services;
using NSubstitute;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HierarchyExchange.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(ImportScenario)}"" type")]
internal class ImportScenarioTests
{
	#region Methods
	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: an import that replaces the tree empties the copy history.
	/// </summary>
	[Test]
	public async Task ImportAsync_Clears_The_Copy_History_On_Replacement()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFolderDto()];

		ICopyHistoryRecorder copyHistoryRecorder = Substitute.For<ICopyHistoryRecorder>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ImportDataResult imported = new(ItemDtoFactory.CreateLoadedHierarchy([]), ImportMode.Replace);

			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			dataExchange
				.ImportDataAsync(hierarchy)
				.Returns(imported);

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(copyHistoryRecorder);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		await sut.ImportAsync(hierarchy);

		// Assert
		copyHistoryRecorder
			.Received(1)
			.Clear();
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: an import that replaces the tree empties it.
	/// </summary>
	[Test]
	public async Task ImportAsync_Clears_The_Hierarchy_On_Replacement()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFolderDto()];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ImportDataResult imported = new(ItemDtoFactory.CreateLoadedHierarchy([]), ImportMode.Replace);

			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			dataExchange
				.ImportDataAsync(hierarchy)
				.Returns(imported);

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		await sut.ImportAsync(hierarchy);

		// Assert
		hierarchy
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: the open files are closed before anything is imported.
	/// </summary>
	[Test]
	public async Task ImportAsync_Closes_The_Open_Files_Before_The_Import()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFileDto(isEditing: true)];

		IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		await sut.ImportAsync(hierarchy);

		// Assert
		Received.InOrder(() =>
		{
			openFilesCloser.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>());

			dataExchange.ImportDataAsync(hierarchy);
		});
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: only the files that are open in an editor or a program are closed.
	/// </summary>
	[Test]
	public async Task ImportAsync_Closes_The_Open_Files_Of_The_Tree()
	{
		// Arrange
		FileDto edited = ItemDtoFactory.CreateFileDto(isEditing: true);

		FileDto executed = ItemDtoFactory.CreateFileDto(isExecuting: true);

		Collection<ExplorerItemDtoBase> hierarchy = [edited, ItemDtoFactory.CreateFileDto(), executed];

		FileDto[] expected = [edited, executed];

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(openFilesCloser));

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		await sut.ImportAsync(hierarchy);

		// Assert
		await openFilesCloser.Received(1).TryCloseAsync(
			Arg.Is<IReadOnlyCollection<FileDto>>(x => x.ToHashSet().SetEquals(expected)),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: an import that replaces the tree forgets every key.
	/// </summary>
	[Test]
	public async Task ImportAsync_Drops_The_Keys_On_Replacement()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFolderDto()];

		IContentKeys contentKeys = Substitute.For<IContentKeys>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ImportDataResult imported = new(ItemDtoFactory.CreateLoadedHierarchy([]), ImportMode.Replace);

			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			dataExchange
				.ImportDataAsync(hierarchy)
				.Returns(imported);

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(contentKeys);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		await sut.ImportAsync(hierarchy);

		// Assert
		contentKeys
			.Received(1)
			.DiscardAllKeys();
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: an import that replaces the tree forgets its state first, so the state
	/// of the imported objects stays.
	/// </summary>
	[Test]
	public async Task ImportAsync_Forgets_The_Tree_State_Before_Remembering_The_Imported_One()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFolderDto()];

		LoadedHierarchy loaded = ItemDtoFactory.CreateLoadedHierarchy([ItemDtoFactory.CreateFolderDto()]);

		ISessionTreeState sessionTreeState = Substitute.For<ISessionTreeState>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			dataExchange
				.ImportDataAsync(hierarchy)
				.Returns(new ImportDataResult(loaded, ImportMode.Replace));

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);

			builder.RegisterInstance(sessionTreeState);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		await sut.ImportAsync(hierarchy);

		// Assert
		Received.InOrder(() =>
		{
			sessionTreeState.Clear();

			sessionTreeState.Remember(loaded);
		});
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: an import that replaces the tree forgets its state.
	/// </summary>
	[Test]
	public async Task ImportAsync_Forgets_The_Tree_State_On_Replacement()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFolderDto()];

		ISessionTreeState sessionTreeState = Substitute.For<ISessionTreeState>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ImportDataResult imported = new(ItemDtoFactory.CreateLoadedHierarchy([]), ImportMode.Replace);

			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			dataExchange
				.ImportDataAsync(hierarchy)
				.Returns(imported);

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);

			builder.RegisterInstance(sessionTreeState);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		await sut.ImportAsync(hierarchy);

		// Assert
		sessionTreeState
			.Received(1)
			.Clear();
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: nothing is imported while open files stay open.
	/// </summary>
	[Test]
	public async Task ImportAsync_Imports_Nothing_When_The_Open_Files_Stay_Open()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFileDto(isEditing: true)];

		IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			// The user keeps the files open.
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		ImportDataResult? result = await sut.ImportAsync(hierarchy);

		// Assert
		result
			.Should()
			.BeNull();

		await dataExchange
			.DidNotReceive()
			.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: imported objects added to the tree leave its objects in place.
	/// </summary>
	[Test]
	public async Task ImportAsync_Keeps_The_Hierarchy_On_Appending()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		Collection<ExplorerItemDtoBase> hierarchy = [folder];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ImportDataResult imported = new(ItemDtoFactory.CreateLoadedHierarchy([]), ImportMode.Append);

			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			dataExchange
				.ImportDataAsync(hierarchy)
				.Returns(imported);

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		await sut.ImportAsync(hierarchy);

		// Assert
		hierarchy
			.Should()
			.Equal(folder);
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: imported objects added to the tree keep the keys of its folders.
	/// </summary>
	[Test]
	public async Task ImportAsync_Keeps_The_Keys_On_Appending()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFolderDto()];

		IContentKeys contentKeys = Substitute.For<IContentKeys>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ImportDataResult imported = new(ItemDtoFactory.CreateLoadedHierarchy([]), ImportMode.Append);

			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			dataExchange
				.ImportDataAsync(hierarchy)
				.Returns(imported);

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(contentKeys);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		await sut.ImportAsync(hierarchy);

		// Assert
		contentKeys
			.DidNotReceive()
			.DiscardAllKeys();
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: the stored tree state of the imported objects is kept for their rows.
	/// </summary>
	[Test]
	public async Task ImportAsync_Remembers_The_Tree_State_Of_The_Imported_Objects()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFolderDto()];

		LoadedHierarchy loaded = ItemDtoFactory.CreateLoadedHierarchy([ItemDtoFactory.CreateFolderDto()]);

		ISessionTreeState sessionTreeState = Substitute.For<ISessionTreeState>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			dataExchange
				.ImportDataAsync(hierarchy)
				.Returns(new ImportDataResult(loaded, ImportMode.Append));

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);

			builder.RegisterInstance(sessionTreeState);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		await sut.ImportAsync(hierarchy);

		// Assert
		sessionTreeState
			.Received(1)
			.Remember(loaded);
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: an import without a chosen way to put the objects into the tree imports
	/// nothing.
	/// </summary>
	[Test]
	public async Task ImportAsync_Returns_Nothing_When_No_Mode_Is_Chosen()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFolderDto()];

		ISessionTreeState sessionTreeState = Substitute.For<ISessionTreeState>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ImportDataResult imported = new(ItemDtoFactory.CreateLoadedHierarchy([]), ImportMode.None);

			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			dataExchange
				.ImportDataAsync(hierarchy)
				.Returns(imported);

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);

			builder.RegisterInstance(sessionTreeState);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		ImportDataResult? result = await sut.ImportAsync(hierarchy);

		// Assert
		result
			.Should()
			.BeNull();

		sessionTreeState
			.DidNotReceive()
			.Remember(Arg.Any<LoadedHierarchy>());
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: an import that fails imports nothing and leaves the tree as it is.
	/// </summary>
	[Test]
	public async Task ImportAsync_Returns_Nothing_When_The_Import_Fails()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		Collection<ExplorerItemDtoBase> hierarchy = [folder];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			// The import fails.
			dataExchange
				.ImportDataAsync(hierarchy)
				.Returns((ImportDataResult?)null);

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		ImportDataResult? result = await sut.ImportAsync(hierarchy);

		// Assert
		result
			.Should()
			.BeNull();

		hierarchy
			.Should()
			.Equal(folder);
	}

	/// <summary>
	/// <see cref="ImportScenario.ImportAsync" />: the result of the import is handed back.
	/// </summary>
	[Test]
	public async Task ImportAsync_Returns_The_Result_Of_The_Import()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFolderDto()];

		ImportDataResult imported = new(ItemDtoFactory.CreateLoadedHierarchy([]), ImportMode.Append);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			dataExchange
				.ImportDataAsync(hierarchy)
				.Returns(imported);

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(openFilesCloser);
		});

		ImportScenario sut = mock.Create<ImportScenario>();

		// Act
		ImportDataResult? result = await sut.ImportAsync(hierarchy);

		// Assert
		result
			.Should()
			.BeSameAs(imported);
	}
	#endregion
}
