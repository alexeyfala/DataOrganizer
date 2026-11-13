using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services.Dialogs;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(FileSystemPicker)}"" type")]
internal class FileSystemPickerTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileSystemPicker.SaveFileAsync" />: returns null when no main window is open.
	/// </summary>
	[Test]
	public async Task SaveFileAsync_Returns_Null_Without_A_Main_Window()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FileSystemPicker sut = mock.Create<FileSystemPicker>();

		// Act
		string? result = await sut.SaveFileAsync(new()
		{
			DefaultExtension = "json",
			FileTypes = [],
			ShowOverwritePrompt = true,
			SuggestedFileName = "name",
			Title = "title"
		});

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileSystemPicker.SelectFilesAsync" />: returns an empty array when no main window is open.
	/// </summary>
	[Test]
	public async Task SelectFilesAsync_Returns_Empty_Array_Without_A_Main_Window()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FileSystemPicker sut = mock.Create<FileSystemPicker>();

		// Act
		string[] result = await sut.SelectFilesAsync(new()
		{
			AllowMultiple = false,
			FileTypes = [],
			Title = "title"
		});

		// Assert
		result
			.Should()
			.BeEmpty();
	}
	#endregion
}
