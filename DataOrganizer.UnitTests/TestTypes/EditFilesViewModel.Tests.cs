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
	/// Test of <see cref="EditFilesViewModel.AddTab(FileModelDto)" />.
	/// </summary>
	[Test]
	public void AddTab_Adds_Tab()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose();

		EditFilesViewModel sut = mock.Create<EditFilesViewModel>();

		// Act
		sut.AddTab(dto);

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

	/// <summary>
	/// Test of <see cref="EditFilesViewModel.AddTab(FileModelDto)" />.
	/// </summary>
	[Test]
	public void AddTab_Tab_Should_Not_Be_Added_Twice()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		dto.IsEdited = true;

		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose();

		EditFilesViewModel sut = mock.Create<EditFilesViewModel>(TypedParameter.From(viewLauncher));

		// Act
		sut.AddTab(dto);

		// Assert
		sut.EditFiles
			.Should()
			.NotContain(dto);
	}

	/// <summary>
	/// Test of <see cref="EditFilesViewModel.CloseTab(FileModelDto)" />.
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
	#endregion
}
