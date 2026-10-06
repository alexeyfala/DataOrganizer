using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.UnitTests.Factories;
using Shared.Services;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(FileDtoExtensions)}"" type")]
internal class FileDtoExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileDtoExtensions.FindChosenEncoding" />: gives the encoding that the editor state of the file holds.
	/// </summary>
	[Test]
	public void FindChosenEncoding_Reads_The_Encoding_Of_The_Editor_State()
	{
		// Arrange
		SystemTextJsonSerializer serializer = new();

		FileDto file = ItemDtoFactory.CreateFileDto(editorState: serializer.Serialize(new FileEditorState
		{
			Encoding = "cp866"
		}));

		// Act
		string? encoding = file.FindChosenEncoding(serializer);

		// Assert
		encoding
			.Should()
			.Be("cp866");
	}

	/// <summary>
	/// <see cref="FileDtoExtensions.FindChosenEncoding" />: an editor state that cannot be read holds no choice.
	/// </summary>
	[Test]
	public void FindChosenEncoding_Returns_Null_For_A_State_It_Cannot_Read()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(editorState: @"{ ""Encoding"": ");

		// Act
		string? encoding = file.FindChosenEncoding(new SystemTextJsonSerializer());

		// Assert
		encoding
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileDtoExtensions.FindChosenEncoding" />: a file without an editor state, or with one stored before the
	/// encoding could be chosen, holds no choice.
	/// </summary>
	[TestCase(null)]
	[TestCase(@"{ ""FontSize"": 14 }")]
	public void FindChosenEncoding_Returns_Null_Without_A_Choice(string? editorState)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(editorState: editorState);

		// Act
		string? encoding = file.FindChosenEncoding(new SystemTextJsonSerializer());

		// Assert
		encoding
			.Should()
			.BeNull();
	}
	#endregion
}
