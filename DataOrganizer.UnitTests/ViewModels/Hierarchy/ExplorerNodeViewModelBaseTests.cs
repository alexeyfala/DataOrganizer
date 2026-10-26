using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services.Runtime;
using DataOrganizer.ViewModels.Hierarchy;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.ViewModels.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(ExplorerNodeViewModelBase)}"" type")]
internal class ExplorerNodeViewModelBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExplorerNodeViewModelBase.Create" />: a folder gets the row of a folder and a file the row of a file,
	/// both showing the object.
	/// </summary>
	[Test]
	public void Create_Makes_The_Row_Of_The_Kind_Of_The_Object([Values] bool isFolder)
	{
		// Arrange
		ExplorerItemDtoBase model = isFolder ? ItemDtoFactory.CreateFolderDto() : ItemDtoFactory.CreateFileDto();

		// Act
		using ExplorerNodeViewModelBase row = ExplorerNodeViewModelBase.Create(model, new WorkSession());

		// Assert
		row
			.Should()
			.BeOfType(isFolder ? typeof(FolderNodeViewModel) : typeof(FileNodeViewModel));

		row.Model
			.Should()
			.BeSameAs(model);
	}

	/// <summary>
	/// <see cref="ExplorerNodeViewModelBase.IsExpanded" />: the work session keeps the state, so a row made again shows the
	/// object as the row before left it.
	/// </summary>
	[Test]
	public void IsExpanded_Is_Kept_By_The_Work_Session([Values] bool isExpanded)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		WorkSession session = new();

		session.SetExpanded(folder.Id, !isExpanded);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(folder);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		FolderNodeViewModel sut = mock.Create<FolderNodeViewModel>();

		// Act
		sut.IsExpanded = isExpanded;

		// Assert
		session.IsExpanded(folder.Id)
			.Should()
			.Be(isExpanded);

		using FolderNodeViewModel next = new(folder, session);

		next.IsExpanded
			.Should()
			.Be(isExpanded);
	}

	/// <summary>
	/// <see cref="ExplorerNodeViewModelBase.IsExpanded" />: only a change is announced.
	/// </summary>
	[Test]
	public void IsExpanded_Notifies_Only_A_Change()
	{
		// Arrange
		List<string?> changed = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(ItemDtoFactory.CreateFolderDto());

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FolderNodeViewModel sut = mock.Create<FolderNodeViewModel>();

		sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		// Act
		sut.IsExpanded = true;

		sut.IsExpanded = true;

		sut.IsExpanded = false;

		// Assert
		changed
			.Should()
			.Equal(nameof(ExplorerNodeViewModelBase.IsExpanded), nameof(ExplorerNodeViewModelBase.IsExpanded));
	}

	/// <summary>
	/// <see cref="ExplorerNodeViewModelBase.IsSelected" />: the work session keeps the state, so a row made again shows the
	/// object as the row before left it.
	/// </summary>
	[Test]
	public void IsSelected_Is_Kept_By_The_Work_Session([Values] bool isSelected)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		WorkSession session = new();

		session.SetSelected(file.Id, !isSelected);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(file);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		FileNodeViewModel sut = mock.Create<FileNodeViewModel>();

		// Act
		sut.IsSelected = isSelected;

		// Assert
		session.IsSelected(file.Id)
			.Should()
			.Be(isSelected);

		using FileNodeViewModel next = new(file, session);

		next.IsSelected
			.Should()
			.Be(isSelected);
	}

	/// <summary>
	/// <see cref="ExplorerNodeViewModelBase.IsSelected" />: only a change is announced.
	/// </summary>
	[Test]
	public void IsSelected_Notifies_Only_A_Change()
	{
		// Arrange
		List<string?> changed = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(ItemDtoFactory.CreateFileDto());

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FileNodeViewModel sut = mock.Create<FileNodeViewModel>();

		sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		// Act
		sut.IsSelected = true;

		sut.IsSelected = true;

		sut.IsSelected = false;

		// Assert
		changed
			.Should()
			.Equal(nameof(ExplorerNodeViewModelBase.IsSelected), nameof(ExplorerNodeViewModelBase.IsSelected));
	}
	#endregion
}
