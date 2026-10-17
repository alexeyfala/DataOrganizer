using AwesomeAssertions;
using DataOrganizer.ViewModels;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadTabViewModel)}"" type")]
internal class NotepadTabViewModelTests
{
	#region Methods
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
	#endregion
}
