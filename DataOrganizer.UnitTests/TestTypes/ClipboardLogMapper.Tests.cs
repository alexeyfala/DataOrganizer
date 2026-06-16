using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.DTO.Clipboard.Persistence;
using DataOrganizer.Helpers.Clipboard;
using System.Collections.Generic;
using System.Text.Json;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardLogMapper)}"" type")]
internal class ClipboardLogMapperTests
{
	#region Methods
	/// <summary>
	/// Test that the persisted history survives a JSON serialization round-trip with polymorphic types.
	/// </summary>
	[Test]
	public void Json_RoundTrip_Preserves_Polymorphic_Entries()
	{
		// Arrange
		List<ClipboardLogEntryBase> entries =
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
		string json = JsonSerializer.Serialize(ClipboardLogMapper.ToPersisted(entries));

		PersistedClipboardLog? deserialized = JsonSerializer.Deserialize<PersistedClipboardLog>(json);

		// Assert
		deserialized
			.Should()
			.NotBeNull();

		deserialized!.Version
			.Should()
			.Be(PersistedClipboardLog.CurrentVersion);

		List<ClipboardLogEntryBase> domain = ClipboardLogMapper.ToDomain(deserialized);

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
	/// <see cref="ClipboardLogMapper.ToDomain" />: URL detection trims surrounding whitespace.
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
		ClipboardLogEntryBase result = RoundTrip(entry);

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
	/// <see cref="ClipboardLogMapper.ToPersisted" /> / <see cref="ClipboardLogMapper.ToDomain" />:
	/// an empty history maps to a versioned, empty container and back to no entries.
	/// </summary>
	[Test]
	public void RoundTrip_Empty_History_Yields_No_Entries()
	{
		// Act
		PersistedClipboardLog persisted = ClipboardLogMapper.ToPersisted([]);

		// Assert
		persisted.Version
			.Should()
			.Be(PersistedClipboardLog.CurrentVersion);

		ClipboardLogMapper
			.ToDomain(persisted)
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogMapper.ToPersisted" /> / <see cref="ClipboardLogMapper.ToDomain" />.
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
		ClipboardLogEntryBase result = RoundTrip(entry);

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
	/// <see cref="ClipboardLogMapper.ToPersisted" /> / <see cref="ClipboardLogMapper.ToDomain" />.
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
		ClipboardLogEntryBase result = RoundTrip(entry);

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
	/// <see cref="ClipboardLogMapper.ToPersisted" /> / <see cref="ClipboardLogMapper.ToDomain" />:
	/// the pinned flag survives the round-trip.
	/// </summary>
	[Test]
	public void RoundTrip_Preserves_Pinned_State()
	{
		// Arrange
		ClipboardTextEntry entry = new()
		{
			Text = "pinned",
			Html = null,
			Rtf = null,
			Hash = [1],
			IsPinned = true
		};

		// Act
		ClipboardLogEntryBase result = RoundTrip(entry);

		// Assert
		result.IsPinned
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardLogMapper.ToPersisted" /> / <see cref="ClipboardLogMapper.ToDomain" />.
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
		ClipboardLogEntryBase result = RoundTrip(entry);

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
	/// <see cref="ClipboardLogMapper.ToDomain" />: a whole-string URL becomes a URL entry.
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
		ClipboardLogEntryBase result = RoundTrip(entry);

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
	private static ClipboardLogEntryBase RoundTrip(ClipboardLogEntryBase entry)
	{
		PersistedClipboardLog persisted = ClipboardLogMapper.ToPersisted([entry]);

		return ClipboardLogMapper
			.ToDomain(persisted)
			.Should()
			.ContainSingle()
			.Subject;
	}
	#endregion
}
