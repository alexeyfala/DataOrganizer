using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.ViewModels;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EditingFilesViewModel)}"" type")]
internal class EditingFilesViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EditingFilesViewModel.CloseTab" />: removes the tab from the control and clears the file's editing flag.
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
	/// Test of <see cref="EditingFilesViewModel.OpenInEditor" />: a file already being edited is not added again.
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
	/// Test of <see cref="EditingFilesViewModel.OpenInEditor" />: adds the file as a tab, sets its editing flag and selects it.
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
	#endregion
}
