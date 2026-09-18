using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Clipboard;
using DataOrganizer.Dto.Clipboard.Persistence;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using NSubstitute;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Clipboard;

[TestFixture(Description = "Guards that a clipboard history left by an earlier build still opens")]
internal class ClipboardLogFormatCompatibilityTests
{
	#region Data
	/// <summary>
	/// A journal file holding <see cref="Journal" /> under the key <see cref="KeyFile" /> wraps.
	/// </summary>
	private const string BinFile =
		"02E11DC0EBFC573223051C22ACFF3C0075F1A94C4E50E7C6B9703C699A616F0268F3F88386E2F8DDC5056D577EF36FB61F2ED172581E15ED5657C3149F4FE0B0163396BB6B30E55640863A7A17A9645BAB076234CD8BF52EAC32BB6C459589CC6C3279DD2D89B8F7AEDAE423D1CFB350417A16D2844B50ED82499A64548A13BDA6B2D3A385EC07F4014E0DA7E41E310B764D90046AF0F4FCB397FF2AF5CB48C86CB1FA2D630F2AF91DD4616109766D6FA228FBBCCF08AEBCB6372D8EC07B53239A5216DBB64B83EA81202B8A71C0C6306D6F1061BF8FF6B9CBD88974B762791D4DD76938BD597F05B83E63A3A2344F8D5ECCCA7995AA0899B1D4D04639F764366D290243B80A2723BE620902A8C54CE67E3BE150FF11C5606094C879B94FC026E0F9863D6CA1382A0D318E6F96FE10C8B18F323701E6A5FC417599660FAB2BDCA42A49C42CFACA4539AC19F281E1A12DFCA73ABE24E50C304CDE4E0CF47773CA3F9D4DEA4A7029958F3F86A497B476B5C1AE604BE7BC13F2D8911B8B7FA4CA81ED490521E713463FA3E8B921FA3956738E60D3D9C814FDE23AEF4D28B419BF8E198A5520DAB44BA74B6BE4E62B97D064AF26825E6195FD17116036704F81E87BD4F8996459901625097B6141AF9AE98E0158A486FCE0FAF7E6B180D0773F325AEE0E27937546D28660C31C10";

	/// <summary>
	/// Directory the clipboard history files live in.
	/// </summary>
	private const string HistoryFolder = "clip";

	/// <summary>
	/// The payload <see cref="BinFile" /> carries, as an earlier build wrote it.
	/// </summary>
	private const string Journal = """
		{
		  "Entries": [
		    {
		      "$type": "Text",
		      "Html": null,
		      "Rtf": null,
		      "Text": "Golden text",
		      "Hash": "AQID",
		      "IsPinned": true
		    },
		    {
		      "$type": "Text",
		      "Html": null,
		      "Rtf": null,
		      "Text": "https://example.com/golden",
		      "Hash": "BA==",
		      "IsPinned": false
		    },
		    {
		      "$type": "Files",
		      "Files": [
		        {
		          "Path": "C:\\golden\\file.txt",
		          "IsFolder": false
		        },
		        {
		          "Path": "C:\\golden\\folder",
		          "IsFolder": true
		        }
		      ],
		      "Hash": "BQ==",
		      "IsPinned": false
		    },
		    {
		      "$type": "Image",
		      "OriginalPng": "iVBORw==",
		      "Hash": "Bg==",
		      "IsPinned": false
		    }
		  ],
		  "Version": 1
		}
		""";

	/// <summary>
	/// A key file wrapping the data encryption key of the history with <see cref="Password" />,
	/// written at the lowest cost the derivation supports, so opening it stays cheap.
	/// </summary>
	private const string KeyFile =
		"0100200000010137142D66FCC43295649878F2D792207ABD626F0E6164385E56563652840D7238A47081542B01147318725C0B0B5FBC5189981451FB622D0EF3008B65F92CAEB39C6B757D398FB7FA76D2EC6E82C9536DC63E2A50C328253017971C1874BDB51AED397D0B38C0F626";

	/// <summary>
	/// Password the recorded history was written with.
	/// </summary>
	private const string Password = "GoldenPassword";

	/// <summary>
	/// Path of the clipboard history file.
	/// </summary>
	private static readonly string BinPath = Path.Combine(HistoryFolder, "History.bin");

	/// <summary>
	/// Path of the clipboard history key file.
	/// </summary>
	private static readonly string KeyPath = Path.Combine(HistoryFolder, "History.key");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ClipboardLogMapper.ToDomain" />: the persisted schema of the journal is what an
	/// earlier build wrote, down to the type discriminators and the names of the properties.
	/// </summary>
	[Test]
	public void ToDomain_Maps_A_Journal_Of_The_Recorded_Schema()
	{
		// Arrange
		PersistedClipboardLog? history = JsonSerializer.Deserialize<PersistedClipboardLog>(Journal);

		history
			.Should()
			.NotBeNull();

		history.Version
			.Should()
			.Be(PersistedClipboardLog.CurrentVersion);

		// Act
		List<ClipboardLogEntryBase> entries = ClipboardLogMapper.ToDomain(history);

		// Assert
		entries
			.Should()
			.HaveCount(4);

		ClipboardTextEntry text = entries[0]
			.Should()
			.BeOfType<ClipboardTextEntry>()
			.Subject;

		text.Text
			.Should()
			.Be("Golden text");

		text.IsPinned
			.Should()
			.BeTrue();

		Convert
			.ToHexString(text.Hash)
			.Should()
			.Be("010203");

		entries[1]
			.Should()
			.BeOfType<ClipboardUrlEntry>()
			.Which
			.Url
			.Should()
			.Be("https://example.com/golden");

		entries[2]
			.Should()
			.BeOfType<ClipboardFilesEntry>()
			.Which
			.FileSystemEntries
			.Should()
			.Equal(
				new ClipboardFileSystemEntry(@"C:\golden\file.txt", IsFolder: false),
				new ClipboardFileSystemEntry(@"C:\golden\folder", IsFolder: true));

		Convert
			.ToHexString(entries[3]
				.Should()
				.BeOfType<ClipboardImageEntry>()
				.Subject
				.OriginalPng)
			.Should()
			.Be("89504E47");
	}

	/// <summary>
	/// <see cref="ClipboardLogStore.TryUnlockAsync" />: a key file and a journal written before this
	/// build still open with their password, so both blob formats and both purposes are unchanged.
	/// </summary>
	[Test]
	public async Task TryUnlockAsync_Opens_A_History_Recorded_By_An_Earlier_Build()
	{
		// Arrange
		InMemoryFileSystem files = new();

		files.Files[KeyPath] = Convert.FromHexString(KeyFile);

		files.Files[BinPath] = Convert.FromHexString(BinFile);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.ClipboardHistoryDirectoryPath
				.Returns(HistoryFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder.Register(_ => Argon2SettingsFactory.CreateLowestCost());

			builder
				.RegisterType<EncryptionService>()
				.As<IEncryptionService>();

			builder
				.RegisterType<SessionKeyStore>()
				.As<ISessionKeyStore>();
		});

		ClipboardLogStore sut = mock.Create<ClipboardLogStore>();

		// Act
		ClipboardLogUnlockResult result = await sut.TryUnlockAsync(SecretFactory.CreatePassword(Password));

		// Assert
		result.Status
			.Should()
			.Be(ClipboardLogStatus.Unlocked);

		result.Entries
			.Should()
			.HaveCount(4);

		result.Entries[0]
			.Should()
			.BeOfType<ClipboardTextEntry>()
			.Which
			.Text
			.Should()
			.Be("Golden text");
	}
	#endregion
}
