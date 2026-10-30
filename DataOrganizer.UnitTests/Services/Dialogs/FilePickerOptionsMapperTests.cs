using Avalonia.Platform.Storage;
using AwesomeAssertions;
using DataOrganizer.Services.Dialogs;
using DialogContracts.Dto;
using System.Linq;

namespace DataOrganizer.UnitTests.Services.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(FilePickerOptionsMapper)}"" type")]
internal class FilePickerOptionsMapperTests
{
	#region Methods
	/// <summary>
	/// <see cref="FilePickerOptionsMapper.ToOpenOptions" />: the options keep the choice of many files, the title and the
	/// file types in their order.
	/// </summary>
	[Test]
	public void ToOpenOptions_Keeps_The_Parameters([Values] bool allowMultiple)
	{
		// Arrange
		FileOpenParameters parameters = new()
		{
			AllowMultiple = allowMultiple,
			FileTypes =
			[
				new()
				{
					MimeTypes = ["application/json"],
					Name = "JSON File",
					Patterns = ["*.json"]
				},
				new()
				{
					MimeTypes = null,
					Name = "Executable",
					Patterns = ["*.exe"]
				}
			],
			Title = "Select"
		};

		// Act
		FilePickerOpenOptions result = FilePickerOptionsMapper.ToOpenOptions(parameters);

		// Assert
		result.AllowMultiple
			.Should()
			.Be(allowMultiple);

		result.Title
			.Should()
			.Be("Select");

		result.FileTypeFilter!.Select(static x => (x.Name, x.MimeTypes, x.Patterns))
			.Should()
			.BeEquivalentTo(
				[
					("JSON File", (string[]?)["application/json"], (string[]?)["*.json"]),
					("Executable", null, ["*.exe"])
				],
				static x => x.WithStrictOrdering());
	}

	/// <summary>
	/// <see cref="FilePickerOptionsMapper.ToSaveOptions" />: the options keep the extension, the overwrite question, the
	/// suggested name, the title and the file types.
	/// </summary>
	[Test]
	public void ToSaveOptions_Keeps_The_Parameters([Values] bool showOverwritePrompt)
	{
		// Arrange
		FileSaveParameters parameters = new()
		{
			DefaultExtension = "json",
			FileTypes =
			[
				new()
				{
					MimeTypes = ["application/xml"],
					Name = "XML File",
					Patterns = ["*.xml"]
				}
			],
			ShowOverwritePrompt = showOverwritePrompt,
			SuggestedFileName = "DataOrganizer",
			Title = "Save as"
		};

		// Act
		FilePickerSaveOptions result = FilePickerOptionsMapper.ToSaveOptions(parameters);

		// Assert
		result.DefaultExtension
			.Should()
			.Be("json");

		result.ShowOverwritePrompt
			.Should()
			.Be(showOverwritePrompt);

		result.SuggestedFileName
			.Should()
			.Be("DataOrganizer");

		result.Title
			.Should()
			.Be("Save as");

		result.FileTypeChoices!.Select(static x => (x.Name, string.Join(", ", x.Patterns!), string.Join(", ", x.MimeTypes!)))
			.Should()
			.Equal(("XML File", "*.xml", "application/xml"));
	}
	#endregion
}
