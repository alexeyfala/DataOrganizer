using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using NSubstitute;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EditFilesViewModel)}"" type")]
internal class EditFilesViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EditFilesViewModel.CloseTab" />.
	/// </summary>
	[Test]
	public void CloseTab_Removes_Tab_From_TabControl()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		dto.IsEdited = true;

		using AutoMock mock = AutoMock.GetLoose();

		EditFilesViewModel sut = mock.Create<EditFilesViewModel>();

		// Act
		sut.CloseTab(dto);

		// Assert
		sut.EditFiles
			.Should()
			.NotContain(dto);

		dto.IsEdited
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="EditFilesViewModel.OpenInEditor" />.
	/// </summary>
	[Test]
	public void OpenInEditor_Cannot_Open_File_Twice()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		dto.IsEdited = true;

		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose();

		EditFilesViewModel sut = mock.Create<EditFilesViewModel>(TypedParameter.From(viewLauncher));

		// Act
		sut.OpenInEditor(dto);

		// Assert
		sut.EditFiles
			.Should()
			.NotContain(dto);
	}

	/// <summary>
	/// Test of <see cref="EditFilesViewModel.OpenInEditor" />.
	/// </summary>
	[Test]
	public void OpenInEditor_Opens_File_In_Built_In_Editor()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose();

		EditFilesViewModel sut = mock.Create<EditFilesViewModel>();

		// Act
		sut.OpenInEditor(dto);

		// Assert
		dto.IsEdited
			.Should()
			.BeTrue();

		sut.EditFiles
			.Should()
			.Contain(dto);

		sut.SelectedIndex
			.Should()
			.Be(sut.EditFiles.Count - 1);
	}
	#endregion
}
