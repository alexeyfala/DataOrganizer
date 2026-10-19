using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Hierarchy;
using System.Linq;

namespace DataOrganizer.UnitTests.ViewModels.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(FolderNodeViewModel)}"" type")]
internal class FolderNodeViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="FolderNodeViewModel.Children" />: the row of an object that leaves the folder is disposed.
	/// </summary>
	[Test]
	public void Children_Dispose_The_Row_Of_An_Object_That_Leaves_The_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		folder.Children.Add(inner);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(folder);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FolderNodeViewModel sut = mock.Create<FolderNodeViewModel>();

		ExplorerNodeViewModelBase row = sut.Children.Single();

		// Act
		folder.Children.Remove(inner);

		// Assert
		row.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FolderNodeViewModel.Children" />: the rows follow the objects of the folder as they come and go, in their
	/// order.
	/// </summary>
	[Test]
	public void Children_Follow_The_Objects_Of_The_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto left = ItemDtoFactory.CreateFileDto();

		FolderDto added = ItemDtoFactory.CreateFolderDto();

		FileDto kept = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(left);

		folder.Children.Add(kept);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(folder);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FolderNodeViewModel sut = mock.Create<FolderNodeViewModel>();

		// Act
		folder.Children.Remove(left);

		folder.Children.Insert(0, added);

		// Assert
		sut.Children.Select(static x => x.Model)
			.Should()
			.Equal(added, kept);
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: the rows inside are disposed with the row of the folder.
	/// </summary>
	[Test]
	public void Dispose_Disposes_The_Rows_Inside()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.Children.Add(ItemDtoFactory.CreateFolderDto());

		folder.Children.Add(ItemDtoFactory.CreateFileDto());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(folder);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FolderNodeViewModel sut = mock.Create<FolderNodeViewModel>();

		ExplorerNodeViewModelBase[] rows = [.. sut.Children];

		// Act
		sut.Dispose();

		// Assert
		rows
			.Should()
			.HaveCount(2)
			.And
			.OnlyContain(static x => x.IsDisposed);
	}
	#endregion
}
