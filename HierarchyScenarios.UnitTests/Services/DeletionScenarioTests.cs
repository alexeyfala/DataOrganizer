using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Clipboard.Interfaces;
using DataProtection.Interfaces;
using FileExecution.Interfaces;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using HierarchyScenarios.Services;
using HierarchyStorage.Interfaces;
using NSubstitute;
using Shared.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HierarchyScenarios.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(DeletionScenario)}"" type")]
internal class DeletionScenarioTests
{
	#region Methods
	/// <summary>
	/// <see cref="DeletionScenario.DeleteAsync" />: a deleted file is closed.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Closes_The_Deleted_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		Collection<ExplorerItemDtoBase> hierarchy = [file];

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(file, hierarchy, Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(hierarchyEditor);

			builder.RegisterInstance(openFilesCloser);
		});

		DeletionScenario sut = mock.Create<DeletionScenario>();

		// Act
		await sut.DeleteAsync(file, hierarchy);

		// Assert
		openFilesCloser
			.Received(1)
			.Close(Arg.Is<IReadOnlyCollection<FileDto>>(x => x.Count == 1 && x.Contains(file)));
	}

	/// <summary>
	/// <see cref="DeletionScenario.DeleteAsync" />: every file of the subtree of a deleted folder is closed.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Closes_The_Files_Of_A_Deleted_Folder()
	{
		// Arrange
		FileDto first = ItemDtoFactory.CreateFileDto();

		FileDto second = ItemDtoFactory.CreateFileDto();

		FolderDto subfolder = ItemDtoFactory.CreateFolderDto();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		subfolder
			.Children
			.Add(second);

		folder
			.Children
			.AddRange([first, subfolder]);

		Collection<ExplorerItemDtoBase> hierarchy = [folder];

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(folder, hierarchy, Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(hierarchyEditor);

			builder.RegisterInstance(openFilesCloser);
		});

		DeletionScenario sut = mock.Create<DeletionScenario>();

		// Act
		await sut.DeleteAsync(folder, hierarchy);

		// Assert
		openFilesCloser
			.Received(1)
			.Close(Arg.Is<IReadOnlyCollection<FileDto>>(x => x.Count == 1 && x.Contains(first)));

		openFilesCloser
			.Received(1)
			.Close(Arg.Is<IReadOnlyCollection<FileDto>>(x => x.Count == 1 && x.Contains(second)));
	}

	/// <summary>
	/// <see cref="DeletionScenario.DeleteAsync" />: the keys of a deleted folder are forgotten.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Discards_The_Keys_Of_A_Deleted_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		Collection<ExplorerItemDtoBase> hierarchy = [folder];

		IContentKeys contentKeys = Substitute.For<IContentKeys>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(folder, hierarchy, Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(contentKeys);

			builder.RegisterInstance(hierarchyEditor);
		});

		DeletionScenario sut = mock.Create<DeletionScenario>();

		// Act
		await sut.DeleteAsync(folder, hierarchy);

		// Assert
		contentKeys
			.Received(1)
			.DiscardKeys(folder);
	}

	/// <summary>
	/// <see cref="DeletionScenario.DeleteAsync" />: a deleted file leaves the keys alone.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Keeps_The_Keys_For_A_Deleted_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		Collection<ExplorerItemDtoBase> hierarchy = [file];

		IContentKeys contentKeys = Substitute.For<IContentKeys>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(file, hierarchy, Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(contentKeys);

			builder.RegisterInstance(hierarchyEditor);
		});

		DeletionScenario sut = mock.Create<DeletionScenario>();

		// Act
		await sut.DeleteAsync(file, hierarchy);

		// Assert
		contentKeys
			.DidNotReceive()
			.DiscardKeys(Arg.Any<FolderDto>());
	}

	/// <summary>
	/// <see cref="DeletionScenario.DeleteAsync" />: an object the database keeps leaves its files and keys alone.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Leaves_The_Files_And_Keys_When_The_Object_Is_Not_Deleted()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.Add(file);

		Collection<ExplorerItemDtoBase> hierarchy = [folder];

		IContentKeys contentKeys = Substitute.For<IContentKeys>();

		ICopyHistoryRecorder copyHistoryRecorder = Substitute.For<ICopyHistoryRecorder>();

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			// The database keeps the object
			hierarchyEditor
				.DeleteAsync(folder, hierarchy, Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(contentKeys);

			builder.RegisterInstance(copyHistoryRecorder);

			builder.RegisterInstance(hierarchyEditor);

			builder.RegisterInstance(openFilesCloser);
		});

		DeletionScenario sut = mock.Create<DeletionScenario>();

		// Act
		bool result = await sut.DeleteAsync(folder, hierarchy);

		// Assert
		result
			.Should()
			.BeFalse();

		openFilesCloser
			.DidNotReceive()
			.Close(Arg.Any<IReadOnlyCollection<FileDto>>());

		copyHistoryRecorder
			.DidNotReceive()
			.Remove(Arg.Any<FileDto>());

		contentKeys
			.DidNotReceive()
			.DiscardKeys(Arg.Any<FolderDto>());
	}

	/// <summary>
	/// <see cref="DeletionScenario.DeleteAsync" />: a deleted file leaves the copy history.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Removes_The_Deleted_File_From_The_Copy_History()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		Collection<ExplorerItemDtoBase> hierarchy = [file];

		ICopyHistoryRecorder copyHistoryRecorder = Substitute.For<ICopyHistoryRecorder>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(file, hierarchy, Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(copyHistoryRecorder);

			builder.RegisterInstance(hierarchyEditor);
		});

		DeletionScenario sut = mock.Create<DeletionScenario>();

		// Act
		await sut.DeleteAsync(file, hierarchy);

		// Assert
		copyHistoryRecorder
			.Received(1)
			.Remove(file);
	}

	/// <summary>
	/// <see cref="DeletionScenario.DeleteAsync" />: every file of the subtree of a deleted folder leaves the copy
	/// history.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Removes_The_Files_Of_A_Deleted_Folder_From_The_Copy_History()
	{
		// Arrange
		FileDto first = ItemDtoFactory.CreateFileDto();

		FileDto second = ItemDtoFactory.CreateFileDto();

		FolderDto subfolder = ItemDtoFactory.CreateFolderDto();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		subfolder
			.Children
			.Add(second);

		folder
			.Children
			.AddRange([first, subfolder]);

		Collection<ExplorerItemDtoBase> hierarchy = [folder];

		ICopyHistoryRecorder copyHistoryRecorder = Substitute.For<ICopyHistoryRecorder>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(folder, hierarchy, Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(copyHistoryRecorder);

			builder.RegisterInstance(hierarchyEditor);
		});

		DeletionScenario sut = mock.Create<DeletionScenario>();

		// Act
		await sut.DeleteAsync(folder, hierarchy);

		// Assert
		copyHistoryRecorder
			.Received(1)
			.Remove(first);

		copyHistoryRecorder
			.Received(1)
			.Remove(second);
	}

	/// <summary>
	/// <see cref="DeletionScenario.DeleteAsync" />: an object the database deletes is reported deleted.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Returns_True_When_The_Object_Is_Deleted()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		Collection<ExplorerItemDtoBase> hierarchy = [file];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(file, hierarchy, Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(hierarchyEditor);
		});

		DeletionScenario sut = mock.Create<DeletionScenario>();

		// Act
		bool result = await sut.DeleteAsync(file, hierarchy);

		// Assert
		result
			.Should()
			.BeTrue();
	}
	#endregion
}
