using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using Shared.Extensions;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(EditingFilesViewModel)}"" type")]
internal class EditingFilesViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="EditingFilesViewModel.CloseTab" />: removes the tab from the control and clears the file's editing flag.
	/// </summary>
	[Test]
	public void CloseTab_Removes_Tab_From_TabControl()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

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
		FileDto dto = ItemDtoFactory.CreateFileDto();

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
		FileDto dto = ItemDtoFactory.CreateFileDto();

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
	/// <see cref="EditingFilesViewModel.Restore" />: the files open in their order with the tab of the selected file
	/// selected and the previous file kept for Ctrl+Tab.
	/// </summary>
	[Test]
	public void Restore_Opens_The_Tabs()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(count: 3)];

		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		// Act
		sut.Restore(new()
		{
			Files = files,
			PreviousFile = files[2],
			SelectedFile = files[1]
		});

		// Assert
		sut.Items
			.Should()
			.Equal(files);

		sut.SelectedIndex
			.Should()
			.Be(1);

		sut.PreviousFile
			.Should()
			.BeSameAs(files[2]);
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.State" />: the file of a closed tab is no way back for Ctrl+Tab.
	/// </summary>
	[Test]
	public void State_Drops_A_Closed_Previous_File()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(count: 2)];

		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		sut
			.Items
			.AddRange(files);

		sut.PreviousFile = files[1];

		sut.CloseTab(files[1]);

		// Act
		EditorTabsState state = sut.State;

		// Assert
		state.PreviousFile
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.State" />: the files of the tabs in their order, with the selected one and the
	/// previous one.
	/// </summary>
	[Test]
	public void State_Holds_The_Tabs()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(count: 3)];

		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel sut = mock.Create<EditingFilesViewModel>();

		sut
			.Items
			.AddRange(files);

		sut.SelectedIndex = 1;

		sut.PreviousFile = files[0];

		// Act
		EditorTabsState state = sut.State;

		// Assert
		state.Files
			.Should()
			.Equal(files);

		state.SelectedFile
			.Should()
			.BeSameAs(files[1]);

		state.PreviousFile
			.Should()
			.BeSameAs(files[0]);
	}
	#endregion
}
