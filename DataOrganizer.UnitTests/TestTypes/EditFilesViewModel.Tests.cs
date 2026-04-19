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

		dto.IsEditing = true;

		using AutoMock mock = AutoMock.GetLoose();

		EditFilesViewModel sut = mock.Create<EditFilesViewModel>();

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
	/// Test of <see cref="EditFilesViewModel.OpenInEditor" />.
	/// </summary>
	[Test]
	public void OpenInEditor_Cannot_Open_File_Twice()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		dto.IsEditing = true;

		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose();

		EditFilesViewModel sut = mock.Create<EditFilesViewModel>(TypedParameter.From(viewLauncher));

		// Act
		sut.OpenInEditor(dto);

		// Assert
		sut.Items
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
