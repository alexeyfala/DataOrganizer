using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AwesomeAssertions;
using DataOrganizer.Services;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FileSystemPicker)}"" type")]
internal class FileSystemPickerTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileSystemPicker.SaveFileAsync{T}" />: returns null when no window of the required type is open.
	/// </summary>
	[Test]
	public async Task SaveFileAsync_Returns_Null_When_Application_Has_No_Window_Of_Required_Type()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FileSystemPicker sut = mock.Create<FileSystemPicker>();

		// Act
		string? result = await sut.SaveFileAsync<Window>(new FilePickerSaveOptions());

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileSystemPicker.SelectFilesAsync{T}" />: returns an empty array when no window of the required type is open.
	/// </summary>
	[Test]
	public async Task SelectFilesAsync_Returns_Empty_Array_When_Application_Has_No_Window_Of_Required_Type()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FileSystemPicker sut = mock.Create<FileSystemPicker>();

		// Act
		string[] result = await sut.SelectFilesAsync<Window>(new FilePickerOpenOptions());

		// Assert
		result
			.Should()
			.BeEmpty();
	}
	#endregion
}
