using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.FilteredLists.Dto;
using DataOrganizer.Hierarchy.Interfaces;
using DataOrganizer.Hierarchy.MultipleSelection.Messages;
using DataOrganizer.Hierarchy.MultipleSelection.Services;
using DataOrganizer.Hierarchy.UnitTests.Factories;
using DataOrganizer.Hierarchy.UnitTests.MultipleSelection.Fakes;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace DataOrganizer.Hierarchy.UnitTests.MultipleSelection.Services;

[TestFixture(Description = $@"Tests of ""{nameof(CheckMarks)}"" type")]
internal class CheckMarksTests
{
	#region Methods
	/// <summary>
	/// <see cref="CheckMarks.CheckShown" />: a shown folder with nothing shown inside is checked whole, the hidden
	/// contents included, and its parent with hidden objects is checked in part.
	/// </summary>
	[Test]
	public void CheckShown_Checks_A_Folder_Whole_When_The_Search_Shows_Nothing_Inside_It()
	{
		// Arrange
		FileDto a1 = ItemDtoFactory.CreateFileDto();

		FolderDto a = Nest(ItemDtoFactory.CreateFolderDto(), a1);

		FileDto b = ItemDtoFactory.CreateFileDto();

		FolderDto root = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a,
			b);

		HierarchyFilter filter = CreateSearch(root.Id, a.Id);

		CheckMarksTreeState treeState = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(treeState)
			.As<ISessionTreeState>());

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		sut.CheckShown([root], filter);

		// Assert
		bool?[] states =
		[
			treeState.IsChecked(a.Id),
			treeState.IsChecked(a1.Id),
			treeState.IsChecked(b.Id),
			treeState.IsChecked(root.Id)
		];

		states
			.Should()
			.Equal(
				true,
				true,
				false,
				null);
	}

	/// <summary>
	/// <see cref="CheckMarks.CheckShown" />: without a search every object ends up checked.
	/// </summary>
	[Test]
	public void CheckShown_Checks_Everything_Without_A_Search()
	{
		// Arrange
		FileDto a1 = ItemDtoFactory.CreateFileDto();

		FileDto a2 = ItemDtoFactory.CreateFileDto();

		FolderDto a = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a1,
			a2);

		FolderDto empty = ItemDtoFactory.CreateFolderDto();

		FolderDto root = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a,
			empty);

		CheckMarksTreeState treeState = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(treeState)
			.As<ISessionTreeState>());

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		sut.CheckShown([root], HierarchyFilter.None);

		// Assert
		bool?[] states =
		[
			treeState.IsChecked(root.Id),
			treeState.IsChecked(a.Id),
			treeState.IsChecked(a1.Id),
			treeState.IsChecked(a2.Id),
			treeState.IsChecked(empty.Id)
		];

		states
			.Should()
			.OnlyContain(static x => x == true);
	}

	/// <summary>
	/// <see cref="CheckMarks.CheckShown" />: under a search only the shown objects are checked, and the folders on the
	/// way to them are checked in part.
	/// </summary>
	[Test]
	public void CheckShown_Checks_Only_What_The_Search_Shows()
	{
		// Arrange
		FileDto a1 = ItemDtoFactory.CreateFileDto();

		FileDto a2 = ItemDtoFactory.CreateFileDto();

		FolderDto a = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a1,
			a2);

		FolderDto root = Nest(ItemDtoFactory.CreateFolderDto(), a);

		HierarchyFilter filter = CreateSearch(
			root.Id,
			a.Id,
			a1.Id);

		CheckMarksTreeState treeState = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(treeState)
			.As<ISessionTreeState>());

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		sut.CheckShown([root], filter);

		// Assert
		bool?[] states =
		[
			treeState.IsChecked(a1.Id),
			treeState.IsChecked(a2.Id),
			treeState.IsChecked(a.Id),
			treeState.IsChecked(root.Id)
		];

		states
			.Should()
			.Equal(
				true,
				false,
				null,
				null);
	}

	/// <summary>
	/// <see cref="CheckMarks.GetCheckedWithParents" />: the checked objects come with the folders on the way to them,
	/// the unchecked ones stay out.
	/// </summary>
	[Test]
	public void GetCheckedWithParents_Takes_The_Folders_On_The_Way()
	{
		// Arrange
		FileDto a1 = ItemDtoFactory.CreateFileDto();

		FileDto a2 = ItemDtoFactory.CreateFileDto();

		FolderDto a = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a1,
			a2);

		FolderDto root = Nest(ItemDtoFactory.CreateFolderDto(), a);

		CheckMarksTreeState treeState = new();

		treeState.SetChecked(a1.Id, true);

		treeState.SetChecked(a.Id, null);

		treeState.SetChecked(root.Id, null);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(treeState)
			.As<ISessionTreeState>());

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		IReadOnlySet<Guid> result = sut.GetCheckedWithParents([root]);

		// Assert
		result
			.Should()
			.BeEquivalentTo([root.Id, a.Id, a1.Id]);
	}

	/// <summary>
	/// <see cref="CheckMarks.GetTopmostChecked" />: a checked folder stands for what it holds, and a folder checked in
	/// part gives its checked objects.
	/// </summary>
	[Test]
	public void GetTopmostChecked_Leaves_Out_The_Objects_Inside_A_Checked_Folder()
	{
		// Arrange
		FileDto a1 = ItemDtoFactory.CreateFileDto();

		FolderDto a = Nest(ItemDtoFactory.CreateFolderDto(), a1);

		FileDto b = ItemDtoFactory.CreateFileDto();

		FileDto c = ItemDtoFactory.CreateFileDto();

		FolderDto root = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a,
			b,
			c);

		CheckMarksTreeState treeState = new();

		treeState.SetChecked(a.Id, true);

		treeState.SetChecked(a1.Id, true);

		treeState.SetChecked(c.Id, true);

		treeState.SetChecked(root.Id, null);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(treeState)
			.As<ISessionTreeState>());

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		IReadOnlyList<ExplorerItemDtoBase> result = sut.GetTopmostChecked([root]);

		// Assert
		result
			.Should()
			.Equal(a, c);
	}

	/// <summary>
	/// <see cref="CheckMarks.Toggle" />: a folder is checked with everything it holds, and its parent with an unchecked
	/// object is checked in part.
	/// </summary>
	[Test]
	public void Toggle_Checks_A_Folder_With_Everything_It_Holds()
	{
		// Arrange
		FileDto a1 = ItemDtoFactory.CreateFileDto();

		FolderDto a = Nest(ItemDtoFactory.CreateFolderDto(), a1);

		FileDto b = ItemDtoFactory.CreateFileDto();

		FolderDto root = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a,
			b);

		CheckMarksTreeState treeState = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(treeState)
			.As<ISessionTreeState>());

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		sut.Toggle(a);

		// Assert
		bool?[] states =
		[
			treeState.IsChecked(a.Id),
			treeState.IsChecked(a1.Id),
			treeState.IsChecked(b.Id),
			treeState.IsChecked(root.Id)
		];

		states
			.Should()
			.Equal(
				true,
				true,
				false,
				null);
	}

	/// <summary>
	/// <see cref="CheckMarks.Toggle" />: a folder checked in part becomes checked whole.
	/// </summary>
	[Test]
	public void Toggle_Checks_A_Partly_Checked_Folder_Whole()
	{
		// Arrange
		FileDto a1 = ItemDtoFactory.CreateFileDto();

		FileDto a2 = ItemDtoFactory.CreateFileDto();

		FolderDto a = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a1,
			a2);

		CheckMarksTreeState treeState = new();

		treeState.SetChecked(a1.Id, true);

		treeState.SetChecked(a.Id, null);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(treeState)
			.As<ISessionTreeState>());

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		sut.Toggle(a);

		// Assert
		bool?[] states =
		[
			treeState.IsChecked(a.Id),
			treeState.IsChecked(a1.Id),
			treeState.IsChecked(a2.Id)
		];

		states
			.Should()
			.OnlyContain(static x => x == true);
	}

	/// <summary>
	/// <see cref="CheckMarks.Toggle" />: checking the last unchecked object of a folder checks the folder whole, and so
	/// on up the tree.
	/// </summary>
	[Test]
	public void Toggle_Checks_The_Folders_Whose_Last_Unchecked_Object_It_Checks()
	{
		// Arrange
		FileDto a1 = ItemDtoFactory.CreateFileDto();

		FileDto a2 = ItemDtoFactory.CreateFileDto();

		FolderDto a = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a1,
			a2);

		FileDto b = ItemDtoFactory.CreateFileDto();

		FolderDto root = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a,
			b);

		CheckMarksTreeState treeState = new();

		treeState.SetChecked(a1.Id, true);

		treeState.SetChecked(a.Id, null);

		treeState.SetChecked(b.Id, true);

		treeState.SetChecked(root.Id, null);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(treeState)
			.As<ISessionTreeState>());

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		sut.Toggle(a2);

		// Assert
		bool?[] states =
		[
			treeState.IsChecked(a2.Id),
			treeState.IsChecked(a.Id),
			treeState.IsChecked(root.Id)
		];

		states
			.Should()
			.OnlyContain(static x => x == true);
	}

	/// <summary>
	/// <see cref="CheckMarks.Toggle" />: one message names the objects whose state changed and leaves out a folder whose
	/// state stays.
	/// </summary>
	[Test]
	public void Toggle_Reports_Only_The_Objects_Whose_State_Changed()
	{
		// Arrange
		FileDto a1 = ItemDtoFactory.CreateFileDto();

		FileDto a2 = ItemDtoFactory.CreateFileDto();

		FolderDto a = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a1,
			a2);

		FileDto b = ItemDtoFactory.CreateFileDto();

		FolderDto root = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a,
			b);

		CheckMarksTreeState treeState = new();

		treeState.SetChecked(a1.Id, true);

		treeState.SetChecked(a.Id, null);

		treeState.SetChecked(root.Id, null);

		IMessenger messenger = new StrongReferenceMessenger();

		List<CheckMarksChangedMessage> received = [];

		messenger.Register<List<CheckMarksChangedMessage>, CheckMarksChangedMessage>(
			received,
			static (list, message) => list.Add(message));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(messenger);

			builder
				.RegisterInstance(treeState)
				.As<ISessionTreeState>();
		});

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		sut.Toggle(a2);

		// Assert
		received
			.Should()
			.ContainSingle()
			.Which
			.Ids
			.Should()
			.BeEquivalentTo([a2.Id, a.Id]);
	}

	/// <summary>
	/// <see cref="CheckMarks.Toggle" />: a checked folder is unchecked with everything it holds, and its checked parent
	/// becomes checked in part.
	/// </summary>
	[Test]
	public void Toggle_Unchecks_A_Checked_Folder_With_Everything_It_Holds()
	{
		// Arrange
		FileDto a1 = ItemDtoFactory.CreateFileDto();

		FolderDto a = Nest(ItemDtoFactory.CreateFolderDto(), a1);

		FileDto b = ItemDtoFactory.CreateFileDto();

		FolderDto root = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a,
			b);

		CheckMarksTreeState treeState = new();

		treeState.SetChecked(root.Id, true);

		treeState.SetChecked(a.Id, true);

		treeState.SetChecked(a1.Id, true);

		treeState.SetChecked(b.Id, true);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(treeState)
			.As<ISessionTreeState>());

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		sut.Toggle(a);

		// Assert
		bool?[] states =
		[
			treeState.IsChecked(a.Id),
			treeState.IsChecked(a1.Id),
			treeState.IsChecked(b.Id),
			treeState.IsChecked(root.Id)
		];

		states
			.Should()
			.Equal(
				false,
				false,
				true,
				null);
	}

	/// <summary>
	/// <see cref="CheckMarks.UncheckAll" />: nothing is reported when no object is checked.
	/// </summary>
	[Test]
	public void UncheckAll_Reports_Nothing_When_Nothing_Is_Checked()
	{
		// Arrange
		IMessenger messenger = new StrongReferenceMessenger();

		List<CheckMarksChangedMessage> received = [];

		messenger.Register<List<CheckMarksChangedMessage>, CheckMarksChangedMessage>(
			received,
			static (list, message) => list.Add(message));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(messenger);

			builder
				.RegisterType<CheckMarksTreeState>()
				.As<ISessionTreeState>();
		});

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		sut.UncheckAll([Nest(ItemDtoFactory.CreateFolderDto(), ItemDtoFactory.CreateFileDto())]);

		// Assert
		received
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="CheckMarks.UncheckAll" />: every object checked whole or in part ends up unchecked.
	/// </summary>
	[Test]
	public void UncheckAll_Unchecks_Every_Object()
	{
		// Arrange
		FileDto a1 = ItemDtoFactory.CreateFileDto();

		FileDto a2 = ItemDtoFactory.CreateFileDto();

		FolderDto a = Nest(
			ItemDtoFactory.CreateFolderDto(),
			a1,
			a2);

		CheckMarksTreeState treeState = new();

		treeState.SetChecked(a1.Id, true);

		treeState.SetChecked(a.Id, null);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(treeState)
			.As<ISessionTreeState>());

		CheckMarks sut = mock.Create<CheckMarks>();

		// Act
		sut.UncheckAll([a]);

		// Assert
		bool?[] states =
		[
			treeState.IsChecked(a.Id),
			treeState.IsChecked(a1.Id),
			treeState.IsChecked(a2.Id)
		];

		states
			.Should()
			.OnlyContain(static x => x == false);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a search that shows the given objects.
	/// </summary>
	private static HierarchyFilter CreateSearch(params IEnumerable<Guid> shownIds)
	{
		return new()
		{
			ExpandedIds = FrozenSet<Guid>.Empty,
			Query = "query",
			ShownIds = shownIds.ToFrozenSet()
		};
	}

	/// <summary>
	/// Puts objects into a folder.
	/// </summary>
	private static FolderDto Nest(FolderDto folder, params ReadOnlySpan<ExplorerItemDtoBase> children)
	{
		foreach (ExplorerItemDtoBase child in children)
		{
			child.Parent = folder;

			folder.Children.Add(child);
		}

		return folder;
	}
	#endregion
}
