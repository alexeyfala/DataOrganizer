using AwesomeAssertions;
using DataOrganizer.Models.Notepad;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Models.Notepad;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadTab)}"" type")]
internal class NotepadTabTests
{
	#region Methods
	/// <summary>
	/// <see cref="NotepadTab.Header" />: a tab with a name is headed by the name.
	/// </summary>
	[Test]
	public void Header_Is_The_Name_Of_A_Named_Tab()
	{
		// Arrange
		NotepadTab sut = new()
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
	/// <see cref="NotepadTab.Name" />: a new name is reported as a change of the header too.
	/// </summary>
	[Test]
	public void Name_Reports_A_Change_Of_The_Header()
	{
		// Arrange
		NotepadTab sut = new()
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
			.Contain(nameof(NotepadTab.Header));
	}
	#endregion
}
