using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Models.Notepad;
using DataOrganizer.ViewModels.Windows;
using System.Linq;

namespace DataOrganizer.UnitTests.ViewModels.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadViewModel)}"" type")]
internal class NotepadViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="NotepadViewModel.AddTabCommand" />: the new tab comes after the open ones and gets selected.
	/// </summary>
	[Test]
	public void AddTabCommand_Adds_A_Selected_Tab_At_The_End()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut
			.AddTabCommand
			.Execute(null);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1, 2);

		sut.SelectedTab
			.Should()
			.BeSameAs(sut.Tabs[1]);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.AddTabCommand" />: the new tab takes the smallest number no open tab has.
	/// </summary>
	[Test]
	public void AddTabCommand_Takes_The_Smallest_Free_Number()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut
			.AddTabCommand
			.Execute(null);

		sut
			.CloseTabCommand
			.Execute(sut.Tabs[1]);

		// Act
		sut
			.AddTabCommand
			.Execute(null);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1, 3, 2);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: the tab goes away and the others stay.
	/// </summary>
	[Test]
	public void CloseTabCommand_Closes_The_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		NotepadTab tab = sut.Tabs[0];

		// Act
		sut
			.CloseTabCommand
			.Execute(tab);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(2);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: the last tab gives way to a new selected tab with the first number.
	/// </summary>
	[Test]
	public void CloseTabCommand_Replaces_The_Last_Tab_With_The_First_One()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut
			.CloseTabCommand
			.Execute(sut.Tabs[0]);

		NotepadTab tab = sut.Tabs[0];

		// Act
		sut
			.CloseTabCommand
			.Execute(tab);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1);

		sut.SelectedTab
			.Should()
			.BeSameAs(sut.Tabs[0]);
	}

	/// <summary>
	/// <see cref="NotepadViewModel(IViewLauncher)" />: the notepad opens with one selected tab with the first number.
	/// </summary>
	[Test]
	public void Constructor_Opens_The_First_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		// Act
		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1);

		sut.SelectedTab
			.Should()
			.BeSameAs(sut.Tabs[0]);
	}
	#endregion
}
