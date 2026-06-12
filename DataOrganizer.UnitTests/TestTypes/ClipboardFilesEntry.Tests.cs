using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using Shared.Properties;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardFilesEntry)}"" type")]
internal class ClipboardFilesEntryTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardFilesEntry.ContentToolTip" />: a short list shows no expanded tooltip.
	/// </summary>
	[Test]
	public void ContentToolTip_Is_Null_When_Not_Truncated()
	{
		// Arrange
		ClipboardFilesEntry sut = FilesEntry(
			new ClipboardFileSystemEntry("C:\\dir\\a.txt", IsFolder: false));

		// Act, Assert
		sut.ContentToolTip
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardFilesEntry.Preview" /> / <see cref="ClipboardFilesEntry.ContentToolTip" />:
	/// a long list is truncated in the summary but fully shown (capped) in the tooltip.
	/// </summary>
	[Test]
	public void Long_List_Truncates_Summary_And_Exposes_ToolTip()
	{
		// Arrange (10 files: more than the 6-item summary budget).
		ClipboardFilesEntry sut = FilesEntry([.. Enumerable
			.Range(0, 10)
			.Select(i => new ClipboardFileSystemEntry($"C:\\dir\\file{i}.txt", IsFolder: false))]);

		// Act, Assert
		sut.Preview!
			.Split(Environment.NewLine)
			.Last()
			.Should()
			.Be("...");

		sut.ContentToolTip
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="ClipboardFilesEntry.Preview" />: a short list is shown in full with a header.
	/// </summary>
	[Test]
	public void Preview_Lists_Header_And_All_Items_When_Short()
	{
		// Arrange
		ClipboardFilesEntry sut = FilesEntry(
			new ClipboardFileSystemEntry("C:\\dir", IsFolder: true),
			new ClipboardFileSystemEntry("C:\\dir\\a.txt", IsFolder: false));

		// Act
		string[] lines = sut
			.Preview!
			.Split(Environment.NewLine);

		// Assert (header + 2 items, no ellipsis).
		lines
			.Should()
			.HaveCount(3);

		lines[0]
			.Should()
			.Contain("Σ: 2");

		lines
			.Should()
			.NotContain("...");
	}

	/// <summary>
	/// <see cref="ClipboardFilesEntry.TypeToolTip" />: folder / file / total counts are reported.
	/// </summary>
	[Test]
	public void TypeToolTip_Reports_Folder_And_File_Counts()
	{
		// Arrange
		ClipboardFilesEntry sut = FilesEntry(
			new ClipboardFileSystemEntry("C:\\dir", IsFolder: true),
			new ClipboardFileSystemEntry("C:\\dir\\a.txt", IsFolder: false),
			new ClipboardFileSystemEntry("C:\\dir\\b.txt", IsFolder: false));

		// Act
		string tooltip = sut.TypeToolTip;

		// Assert
		tooltip
			.Should()
			.Contain($"{Strings.Folders}: 1");

		tooltip
			.Should()
			.Contain($"{Strings.Files}: 2");

		tooltip
			.Should()
			.Contain("Σ: 3");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// A files entry holding <paramref name="entries" />.
	/// </summary>
	private static ClipboardFilesEntry FilesEntry(params ClipboardFileSystemEntry[] entries) => new()
	{
		FileSystemEntries = entries,
		Hash = [1]
	};
	#endregion
}
