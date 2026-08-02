using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities;
using DataOrganizer.ViewModels;
using Shared.Extensions;

namespace DataOrganizer.UnitTests.TestTypes.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(EditingFilesViewModel)}"" type")]
internal class EditingFilesViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="EditingFilesViewModel.CloseAllTabs" />: removes every tab and clears the editing flag of each file.
	/// </summary>
	[Test]
	public void CloseAllTabs_Removes_Every_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		FileModelDto[] dtos =
		[
			TestUtils.CreateFileDto(),
			TestUtils.CreateFileDto(),
			TestUtils.CreateFileDto()
		];

		dtos.ForEach(sut.OpenInEditor);

		// Act
		sut.CloseAllTabs();

		// Assert
		sut.Items
			.Should()
			.BeEmpty();

		dtos
			.Should()
			.OnlyContain(x => !x.IsEditing);
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.CloseOtherTabs" />: keeps the specified tab and removes the rest.
	/// </summary>
	[Test]
	public void CloseOtherTabs_Keeps_Only_The_Specified_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		FileModelDto kept = TestUtils.CreateFileDto();

		FileModelDto closed = TestUtils.CreateFileDto();

		sut.OpenInEditor(kept);

		sut.OpenInEditor(closed);

		// Act
		sut.CloseOtherTabs(kept);

		// Assert
		sut.Items
			.Should()
			.Equal(kept);

		kept.IsEditing
			.Should()
			.BeTrue();

		closed.IsEditing
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.CloseOtherTabsCommand" />: cannot be executed while a single tab is opened.
	/// </summary>
	[Test]
	public void CloseOtherTabsCommand_Is_Disabled_For_A_Single_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		FileModelDto dto = TestUtils.CreateFileDto();

		sut.OpenInEditor(dto);

		// Act
		bool canExecuteWithSingleTab = sut.CloseOtherTabsCommand.CanExecute(dto);

		sut.OpenInEditor(TestUtils.CreateFileDto());

		bool canExecuteWithSecondTab = sut.CloseOtherTabsCommand.CanExecute(dto);

		// Assert
		canExecuteWithSingleTab
			.Should()
			.BeFalse();

		canExecuteWithSecondTab
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.CloseTab" />: removes the tab from the control and clears the file's editing flag.
	/// </summary>
	[Test]
	public void CloseTab_Removes_Tab_From_TabControl()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		dto.IsEditing = true;

		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		// Act
		sut.CloseTab(dto);

		// Assert
		sut.Items
			.Should()
			.NotContain(dto);

		dto.IsEditing
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.OpenInEditor" />: a file already being edited is not added again.
	/// </summary>
	[Test]
	public void OpenInEditor_Cannot_Open_File_Twice()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		dto.IsEditing = true;

		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		// Act
		sut.OpenInEditor(dto);

		// Assert
		sut.Items
			.Should()
			.NotContain(dto);
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.OpenInEditor" />: adds the file as a tab, sets its editing flag and selects it.
	/// </summary>
	[Test]
	public void OpenInEditor_Opens_File_In_Built_In_Editor()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		// Act
		sut.OpenInEditor(dto);

		// Assert
		dto.IsEditing
			.Should()
			.BeTrue();

		sut.Items
			.Should()
			.Contain(dto);

		sut.SelectedIndex
			.Should()
			.Be(sut.Items.Count - 1);
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.SwitchToPreviousTabCommand" />: follows the previously selected file
	/// after other tabs shifted its index.
	/// </summary>
	[Test]
	public void SwitchToPreviousTab_Follows_The_File_Not_The_Index()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		FileModelDto first = TestUtils.CreateFileDto();

		// Becomes the previously selected tab once the third file is opened.
		FileModelDto second = TestUtils.CreateFileDto();

		new[] { first, second, TestUtils.CreateFileDto() }.ForEach(sut.OpenInEditor);

		sut.CloseTab(first);

		// Act
		sut.SwitchToPreviousTabCommand.Execute(null);

		// Assert
		sut.Items[sut.SelectedIndex]
			.Should()
			.BeSameAs(second);
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.SwitchToPreviousTabCommand" />: does nothing once the previously
	/// selected tab is closed.
	/// </summary>
	[Test]
	public void SwitchToPreviousTab_Ignores_A_Closed_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		FileModelDto first = TestUtils.CreateFileDto();

		sut.OpenInEditor(first);

		sut.OpenInEditor(TestUtils.CreateFileDto());

		sut.CloseTab(first);

		int selectedIndexBefore = sut.SelectedIndex;

		// Act
		sut.SwitchToPreviousTabCommand.Execute(null);

		// Assert
		sut.SelectedIndex
			.Should()
			.Be(selectedIndexBefore);
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.SwitchToPreviousTabCommand" />: selects the tab that was active
	/// before the current one.
	/// </summary>
	[Test]
	public void SwitchToPreviousTab_Selects_The_Previously_Selected_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		FileModelDto second = TestUtils.CreateFileDto();

		new[] { TestUtils.CreateFileDto(), second, TestUtils.CreateFileDto() }.ForEach(sut.OpenInEditor);

		// Act
		sut.SwitchToPreviousTabCommand.Execute(null);

		// Assert
		sut.Items[sut.SelectedIndex]
			.Should()
			.BeSameAs(second);
	}
	#endregion
}
