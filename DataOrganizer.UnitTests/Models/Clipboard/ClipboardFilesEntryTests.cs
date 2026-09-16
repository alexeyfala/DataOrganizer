using AwesomeAssertions;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;
using Shared.Properties;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Models.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardFilesEntry)}"" type")]
internal class ClipboardFilesEntryTests
{
	#region Data
	/// <summary>
	/// Path of the first listed file.
	/// </summary>
	private const string FirstFile = $@"{Folder}\a.txt";

	/// <summary>
	/// Directory holding the listed files.
	/// </summary>
	private const string Folder = @"C:\dir";

	/// <summary>
	/// Path of the second listed file.
	/// </summary>
	private const string SecondFile = $@"{Folder}\b.txt";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ClipboardFilesEntry.ContentToolTip" />: a short list shows no expanded tooltip.
	/// </summary>
	[Test]
	public void ContentToolTip_Is_Null_When_Not_Truncated()
	{
		// Arrange
		ClipboardFilesEntry sut = ClipboardEntryFactory.CreateFilesEntry(
		[
			new ClipboardFileSystemEntry(FirstFile, IsFolder: false)
		]);

		// Act, Assert
		sut.ContentToolTip
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardFilesEntry.Preview" />: a short list is shown in full with a header.
	/// </summary>
	[Test]
	public void Preview_Lists_Header_And_All_Items_When_Short()
	{
		// Arrange
		ClipboardFilesEntry sut = ClipboardEntryFactory.CreateFilesEntry(
		[
			new ClipboardFileSystemEntry(Folder, IsFolder: true),
			new ClipboardFileSystemEntry(FirstFile, IsFolder: false)
		]);

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
	/// <see cref="ClipboardFilesEntry.Preview" /> / <see cref="ClipboardFilesEntry.ContentToolTip" />:
	/// a long list is truncated in the summary but fully shown (capped) in the tooltip.
	/// </summary>
	[Test]
	public void Preview_Truncates_A_Long_List_And_Exposes_The_ToolTip()
	{
		// Arrange (10 files: more than the 6-item summary budget).
		ClipboardFilesEntry sut = ClipboardEntryFactory.CreateFilesEntry([.. Enumerable
			.Range(0, 10)
			.Select(i => new ClipboardFileSystemEntry($@"{Folder}\file{i}.txt", IsFolder: false))]);

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
	/// <see cref="ClipboardFilesEntry.TypeToolTip" />: folder / file / total counts are reported.
	/// </summary>
	[Test]
	public void TypeToolTip_Reports_Folder_And_File_Counts()
	{
		// Arrange
		ClipboardFilesEntry sut = ClipboardEntryFactory.CreateFilesEntry(
		[
			new ClipboardFileSystemEntry(Folder, IsFolder: true),
			new ClipboardFileSystemEntry(FirstFile, IsFolder: false),
			new ClipboardFileSystemEntry(SecondFile, IsFolder: false)
		]);

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
}
