using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels.Hierarchy;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(ExplorerNodeExtensions)}"" type")]
internal class ExplorerNodeExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExplorerNodeExtensions.FindNode" />: an object the rows do not show has no row.
	/// </summary>
	[Test]
	public void FindNode_Finds_No_Row_For_An_Object_The_Rows_Do_Not_Show()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		file.Parent = folder;

		using FolderNodeViewModel row = new(folder, new WorkSession());

		// Act
		ExplorerNodeViewModelBase? found = ((ExplorerNodeViewModelBase[])[row]).FindNode(file);

		// Assert
		found
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ExplorerNodeExtensions.FindNode" />: the row of an object inside nested folders is found.
	/// </summary>
	[Test]
	public void FindNode_Finds_The_Row_Of_An_Object_Inside_Nested_Folders()
	{
		// Arrange
		FolderDto outer = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		inner.Parent = outer;

		outer.Children.Add(inner);

		file.Parent = inner;

		inner.Children.Add(file);

		using FolderNodeViewModel row = new(outer, new WorkSession());

		// Act
		ExplorerNodeViewModelBase? found = ((ExplorerNodeViewModelBase[])[row]).FindNode(file);

		// Assert
		found
			.Should()
			.NotBeNull()
			.And
			.Match<ExplorerNodeViewModelBase>(x => x.Model == file);
	}

	/// <summary>
	/// <see cref="ExplorerNodeExtensions.FindPath" />: the rows of the folders of an object come first, from the root down,
	/// and the row of the object last.
	/// </summary>
	[Test]
	public void FindPath_Returns_The_Rows_From_The_Root_Down_To_The_Object()
	{
		// Arrange
		FolderDto outer = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		inner.Parent = outer;

		outer.Children.Add(inner);

		file.Parent = inner;

		inner.Children.Add(file);

		using FolderNodeViewModel row = new(outer, new WorkSession());

		// Act
		ExplorerNodeViewModelBase[] path = ((ExplorerNodeViewModelBase[])[row]).FindPath(file);

		// Assert
		path.Select(static x => x.Model)
			.Should()
			.Equal(outer, inner, file);
	}

	/// <summary>
	/// <see cref="ExplorerNodeExtensions.MergeManyInTree" />: a row added to a folder of the tree is followed from then on.
	/// </summary>
	[Test]
	public void MergeManyInTree_Follows_A_Row_Added_Later()
	{
		// Arrange
		List<string> renamed = [];

		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		root.Children.Add(folder);

		using FolderNodeViewModel row = new(root, new WorkSession());

		using IDisposable subscription = row
			.Children
			.MergeManyInTree(x => x.Model.WhenPropertyChanged(y => y.Name, notifyOnInitialValue: false))
			.Subscribe(x => renamed.Add(x.Sender.Name));

		folder.Children.Add(file);

		// Act
		file.Name = "added";

		// Assert
		renamed
			.Should()
			.Equal("added");
	}

	/// <summary>
	/// <see cref="ExplorerNodeExtensions.MergeManyInTree" />: a row removed from the tree is no longer followed.
	/// </summary>
	[Test]
	public void MergeManyInTree_Lets_Go_Of_A_Row_Removed()
	{
		// Arrange
		List<string> renamed = [];

		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(file);

		root.Children.Add(folder);

		using FolderNodeViewModel row = new(root, new WorkSession());

		using IDisposable subscription = row
			.Children
			.MergeManyInTree(x => x.Model.WhenPropertyChanged(y => y.Name, notifyOnInitialValue: false))
			.Subscribe(x => renamed.Add(x.Sender.Name));

		folder.Children.Remove(file);

		// Act
		file.Name = "removed";

		// Assert
		renamed
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ExplorerNodeExtensions.MergeManyInTree" />: the rows of the tree are followed at every depth, the rows of
	/// folders among them.
	/// </summary>
	[Test]
	public void MergeManyInTree_Merges_The_Rows_At_Any_Depth()
	{
		// Arrange
		List<string> renamed = [];

		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FolderDto innerFolder = ItemDtoFactory.CreateFolderDto();

		FileDto innerFile = ItemDtoFactory.CreateFileDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		innerFolder.Children.Add(innerFile);

		folder.Children.Add(innerFolder);

		root.Children.Add(folder);

		root.Children.Add(file);

		using FolderNodeViewModel row = new(root, new WorkSession());

		using IDisposable subscription = row
			.Children
			.MergeManyInTree(x => x.Model.WhenPropertyChanged(y => y.Name, notifyOnInitialValue: false))
			.Subscribe(x => renamed.Add(x.Sender.Name));

		// Act
		folder.Name = "folder";

		innerFolder.Name = "inner folder";

		innerFile.Name = "inner file";

		file.Name = "file";

		// Assert
		renamed
			.Should()
			.Equal("folder", "inner folder", "inner file", "file");
	}
	#endregion
}
