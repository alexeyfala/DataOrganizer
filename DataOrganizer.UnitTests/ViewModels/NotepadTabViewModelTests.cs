using AwesomeAssertions;
using DataOrganizer.ViewModels;
using System.Collections.Generic;
using System.Text;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadTabViewModel)}"" type")]
internal class NotepadTabViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="NotepadTabViewModel.FindUnreadableEncodings" />: the encodings that cannot read the bytes of the text are
	/// found, here UTF-16 for an odd number of bytes.
	/// </summary>
	[Test]
	public void FindUnreadableEncodings_Finds_The_Encodings_That_Cannot_Read_The_Text()
	{
		// Arrange
		NotepadTabViewModel sut = new()
		{
			Number = 1
		};

		sut.Document.Text = "Hi!";

		// Act
		sut.FindUnreadableEncodings();

		// Assert
		sut.UnreadableEncodings
			.Should()
			.Contain(Encoding.Unicode.WebName);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.Header" />: a tab with a name is headed by the name.
	/// </summary>
	[Test]
	public void Header_Is_The_Name_Of_A_Named_Tab()
	{
		// Arrange
		NotepadTabViewModel sut = new()
		{
			Name = "Notes",
			Number = 1
		};

		// Act, Assert
		sut.Header
			.Should()
			.Be("Notes");
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.Name" />: a new name is reported as a change of the header too.
	/// </summary>
	[Test]
	public void Name_Reports_A_Change_Of_The_Header()
	{
		// Arrange
		NotepadTabViewModel sut = new()
		{
			Number = 1
		};

		List<string?> changed = [];

		sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		// Act
		sut.Name = "Notes";

		// Assert
		changed
			.Should()
			.Contain(nameof(NotepadTabViewModel.Header));
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.RefreshEncoding" />: the tab takes the encoding its text is read in, with its name,
	/// and the one found from its bytes.
	/// </summary>
	[Test]
	public void RefreshEncoding_Takes_The_Encodings_Of_The_Text()
	{
		// Arrange
		NotepadTabViewModel sut = new()
		{
			Number = 1
		};

		// Bytes of UTF-8, read in UTF-16 by choice.
		sut.Codec.Read(Encoding.UTF8.GetBytes("Text"), Encoding.Unicode.WebName);

		// Act
		sut.RefreshEncoding();

		// Assert
		sut
			.Should()
			.BeEquivalentTo(new
			{
				DefaultEncoding = Encoding.UTF8.WebName,
				Encoding = Encoding.Unicode.WebName,
				EncodingName = "UTF-16 LE"
			});
	}
	#endregion
}
