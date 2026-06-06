using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.DTO.Clipboard.Persistence;
using DataOrganizer.Helpers;
using System.Collections.Generic;
using System.Text.Json;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardHistoryMapper)}"" type")]
internal class ClipboardHistoryMapperTests
{
	#region Methods
	/// <summary>
	/// Test that the persisted history survives a JSON serialization round-trip with polymorphic types.
	/// </summary>
	[Test]
	public void Json_RoundTrip_Preserves_Polymorphic_Entries()
	{
		// Arrange
		List<ClipboardHistoryEntryBase> entries =
		[
			new ClipboardTextEntry { Text = "plain", Html = null, Rtf = null, Hash = [1] },
			new ClipboardImageEntry { OriginalPng = [2, 2], Hash = [2] },
			new ClipboardFilesEntry
			{
				FileSystemEntries = [new("C:\\a", IsFolder: false)],
				Hash = [3]
			}
		];

		// Act
		string json = JsonSerializer.Serialize(ClipboardHistoryMapper.ToPersisted(entries));

		PersistedClipboardHistory? deserialized = JsonSerializer.Deserialize<PersistedClipboardHistory>(json);

		// Assert
		deserialized
			.Should()
			.NotBeNull();

		deserialized!.Version
			.Should()
			.Be(PersistedClipboardHistory.CurrentVersion);

		List<ClipboardHistoryEntryBase> domain = ClipboardHistoryMapper.ToDomain(deserialized);

		domain
			.Should()
			.HaveCount(3);

		domain[0]
			.Should()
			.BeOfType<ClipboardTextEntry>();

		domain[1]
			.Should()
			.BeOfType<ClipboardImageEntry>();

		domain[2]
			.Should()
			.BeOfType<ClipboardFilesEntry>();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryMapper.ToDomain" />: URL detection trims surrounding whitespace.
	/// </summary>
	[Test]
	public void RoundTrip_Detects_Url_With_Surrounding_Whitespace()
	{
		// Arrange
		ClipboardTextEntry entry = new()
		{
			Text = "  https://example.com/x  ",
			Html = null,
			Rtf = null,
			Hash = [5]
		};

		// Act
		ClipboardHistoryEntryBase result = RoundTrip(entry);

		// Assert
		result
			.Should()
			.BeOfType<ClipboardUrlEntry>()
			.Subject
			.Url
			.Should()
			.Be("https://example.com/x");
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryMapper.ToPersisted" /> / <see cref="ClipboardHistoryMapper.ToDomain" />:
	/// an empty history maps to a versioned, empty container and back to no entries.
	/// </summary>
	[Test]
	public void RoundTrip_Empty_History_Yields_No_Entries()
	{
		// Act
		PersistedClipboardHistory persisted = ClipboardHistoryMapper.ToPersisted([]);

		// Assert
		persisted.Version
			.Should()
			.Be(PersistedClipboardHistory.CurrentVersion);

		ClipboardHistoryMapper
			.ToDomain(persisted)
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryMapper.ToPersisted" /> / <see cref="ClipboardHistoryMapper.ToDomain" />.
	/// </summary>
	[Test]
	public void RoundTrip_Preserves_Files_Entry()
	{
		// Arrange
		ClipboardFilesEntry entry = new()
		{
			FileSystemEntries =
			[
				new("C:\\folder", IsFolder: true),
				new("C:\\folder\\file.txt", IsFolder: false)
			],
			Hash = [9, 9, 9]
		};

		// Act
		ClipboardHistoryEntryBase result = RoundTrip(entry);

		// Assert
		ClipboardFilesEntry files = result
			.Should()
			.BeOfType<ClipboardFilesEntry>()
			.Subject;

		files.Hash
			.Should()
			.Equal("\t\t\t"u8.ToArray());

		files.FileSystemEntries
			.Should()
			.HaveCount(2);

		files.FileSystemEntries[0].Path
			.Should()
			.Be("C:\\folder");

		files.FileSystemEntries[0].IsFolder
			.Should()
			.BeTrue();

		files.FileSystemEntries[1].IsFolder
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryMapper.ToPersisted" /> / <see cref="ClipboardHistoryMapper.ToDomain" />.
	/// </summary>
	[Test]
	public void RoundTrip_Preserves_Image_Entry()
	{
		// Arrange
		ClipboardImageEntry entry = new()
		{
			OriginalPng = [1, 2, 3, 4, 5],
			Hash = [10, 20, 30]
		};

		// Act
		ClipboardHistoryEntryBase result = RoundTrip(entry);

		// Assert
		ClipboardImageEntry image = result
			.Should()
			.BeOfType<ClipboardImageEntry>()
			.Subject;

		image.OriginalPng
			.Should()
			.Equal(1, 2, 3, 4, 5);

		image.Hash
			.Should()
			.Equal(10, 20, 30);
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryMapper.ToPersisted" /> / <see cref="ClipboardHistoryMapper.ToDomain" />.
	/// </summary>
	[Test]
	public void RoundTrip_Preserves_Text_Entry()
	{
		// Arrange
		ClipboardTextEntry entry = new()
		{
			Text = "hello world",
			Html = "<b>hello</b>",
			Rtf = null,
			Hash = [1, 2, 3]
		};

		// Act
		ClipboardHistoryEntryBase result = RoundTrip(entry);

		// Assert
		ClipboardTextEntry text = result
			.Should()
			.BeOfType<ClipboardTextEntry>()
			.Subject;

		text.Text
			.Should()
			.Be("hello world");

		text.Html
			.Should()
			.Be("<b>hello</b>");

		text.Rtf
			.Should()
			.BeNull();

		text.Hash
			.Should()
			.Equal(1, 2, 3);
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryMapper.ToDomain" />: a whole-string URL becomes a URL entry.
	/// </summary>
	[Test]
	public void RoundTrip_Rebuilds_Url_Entry_From_Url_Text()
	{
		// Arrange
		ClipboardUrlEntry entry = new()
		{
			Text = "https://example.com/page",
			Html = null,
			Rtf = null,
			Url = "https://example.com/page",
			Hash = [7]
		};

		// Act
		ClipboardHistoryEntryBase result = RoundTrip(entry);

		// Assert
		ClipboardUrlEntry url = result
			.Should()
			.BeOfType<ClipboardUrlEntry>()
			.Subject;

		url.Url
			.Should()
			.Be("https://example.com/page");

		url.IsUrl
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Maps an entry to its persisted form and back through the in-memory model (no JSON).
	/// </summary>
	private static ClipboardHistoryEntryBase RoundTrip(ClipboardHistoryEntryBase entry)
	{
		PersistedClipboardHistory persisted = ClipboardHistoryMapper.ToPersisted([entry]);

		return ClipboardHistoryMapper
			.ToDomain(persisted)
			.Should()
			.ContainSingle()
			.Subject;
	}
	#endregion
}
