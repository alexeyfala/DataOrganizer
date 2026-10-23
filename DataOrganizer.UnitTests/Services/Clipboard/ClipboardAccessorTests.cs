using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AwesomeAssertions;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Storage;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardAccessor)}"" type")]
internal class ClipboardAccessorTests
{
	#region Data
	/// <summary>
	/// Name of the file on the clipboard.
	/// </summary>
	private const string FileName = "a.txt";

	/// <summary>
	/// Name of the folder that holds the items of the clipboard.
	/// </summary>
	private const string FolderName = "dir";

	/// <summary>
	/// Path of the file on the clipboard.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine(FolderName, FileName);

	/// <summary>
	/// Path of the folder on the clipboard.
	/// </summary>
	private static readonly string SubfolderPath = TestPath.Combine(FolderName, "sub");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ClipboardAccessor.ClearAsync" />: the clipboard is emptied.
	/// </summary>
	[Test]
	public async Task ClearAsync_Empties_The_Clipboard()
	{
		// Arrange
		IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(clipboard));

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		await sut.ClearAsync();

		// Assert
		await clipboard
			.Received(1)
			.ClearAsync();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.ContainsOwnershipMarkerAsync" />: the marker the application puts on its secrets is
	/// found.
	/// </summary>
	[Test]
	public async Task ContainsOwnershipMarkerAsync_Finds_The_Marker_Of_The_Application()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.GetDataFormatsAsync()
				.Returns([DataFormat.CreateBytesApplicationFormat(ClipboardSensitivityMarkers.AutoClearOwnership)]);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.ContainsOwnershipMarkerAsync();

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.ContainsOwnershipMarkerAsync" />: content copied by something else carries no marker.
	/// </summary>
	[Test]
	public async Task ContainsOwnershipMarkerAsync_Without_The_Marker_Is_False()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.GetDataFormatsAsync()
				.Returns([DataFormat.Text]);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.ContainsOwnershipMarkerAsync();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.ContainsRestoreMarkerAsync" />: the marker of a payload put back from the clipboard log is
	/// found.
	/// </summary>
	[Test]
	public async Task ContainsRestoreMarkerAsync_Finds_The_Marker_Of_A_Restored_Payload()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.GetDataFormatsAsync()
				.Returns([DataFormat.CreateBytesApplicationFormat(ClipboardRestoreMarker.Identifier)]);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.ContainsRestoreMarkerAsync();

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.ContainsRestoreMarkerAsync" />: content copied by something else carries no marker.
	/// </summary>
	[Test]
	public async Task ContainsRestoreMarkerAsync_Without_The_Marker_Is_False()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.GetDataFormatsAsync()
				.Returns([DataFormat.Text]);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.ContainsRestoreMarkerAsync();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.ContainsSensitivityMarkerAsync" />: the marker of any password manager marks the content
	/// as a secret.
	/// </summary>
	[Test]
	public async Task ContainsSensitivityMarkerAsync_Finds_The_Marker_Of_Every_Password_Manager(
		[Values(
			ClipboardSensitivityMarkers.ClipboardViewerIgnore,
			ClipboardSensitivityMarkers.ExcludeFromMonitorProcessing,
			ClipboardSensitivityMarkers.KdePasswordManagerHint,
			ClipboardSensitivityMarkers.NsPasteboardConcealedType)] string marker)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.GetDataFormatsAsync()
				.Returns([DataFormat.CreateBytesPlatformFormat(marker)]);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.ContainsSensitivityMarkerAsync();

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.ContainsSensitivityMarkerAsync" />: content re-published from Windows clipboard history
	/// (ClipboardHistoryItemId present) is not a secret, even though it carries the exclude marker (anti-loop, not secrecy).
	/// </summary>
	[Test]
	public async Task ContainsSensitivityMarkerAsync_Ignores_The_Exclude_Marker_Of_Content_Restored_By_Win_V()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			// Order mirrors a real Win+V restore: the exclude marker precedes the history id.
			clipboard
				.GetDataFormatsAsync()
				.Returns(
				[
					DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.ExcludeFromMonitorProcessing),
					DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.ClipboardHistoryItemId)
				]);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.ContainsSensitivityMarkerAsync();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.ContainsSensitivityMarkerAsync" />: a clipboard that fails to list its formats marks
	/// nothing as a secret.
	/// </summary>
	[Test]
	public async Task ContainsSensitivityMarkerAsync_Is_False_When_The_Clipboard_Fails()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.GetDataFormatsAsync()
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.ContainsSensitivityMarkerAsync();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.ContainsSensitivityMarkerAsync" />: CanIncludeInClipboardHistory = 1 (allowed) does not
	/// mark the content as a secret (e.g. content restored via Win+V).
	/// </summary>
	[Test]
	public async Task ContainsSensitivityMarkerAsync_Is_False_When_The_History_Flag_Allows()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.GetDataFormatsAsync()
				.Returns([DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanIncludeInClipboardHistory)]);

			clipboard
				.TryGetValueAsync(Arg.Is<DataFormat<byte[]>>(format => format!.Identifier == ClipboardSensitivityMarkers.CanIncludeInClipboardHistory))
				.Returns([1, 0, 0, 0]);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.ContainsSensitivityMarkerAsync();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.ContainsSensitivityMarkerAsync" />: a CanIncludeInClipboardHistory flag without a value
	/// does not mark the content as a secret.
	/// </summary>
	[Test]
	public async Task ContainsSensitivityMarkerAsync_Is_False_When_The_History_Flag_Has_No_Value()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.GetDataFormatsAsync()
				.Returns([DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanIncludeInClipboardHistory)]);

			// The flag is there, its value is not.
			clipboard
				.TryGetValueAsync(Arg.Is<DataFormat<byte[]>>(format => format!.Identifier == ClipboardSensitivityMarkers.CanIncludeInClipboardHistory))
				.Returns((byte[]?)null);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.ContainsSensitivityMarkerAsync();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.ContainsSensitivityMarkerAsync" />: CanIncludeInClipboardHistory = 0 (exclude) marks the
	/// content as a secret.
	/// </summary>
	[Test]
	public async Task ContainsSensitivityMarkerAsync_Is_True_When_The_History_Flag_Excludes()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.GetDataFormatsAsync()
				.Returns([DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanIncludeInClipboardHistory)]);

			clipboard
				.TryGetValueAsync(Arg.Is<DataFormat<byte[]>>(format => format!.Identifier == ClipboardSensitivityMarkers.CanIncludeInClipboardHistory))
				.Returns([0, 0, 0, 0]);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.ContainsSensitivityMarkerAsync();

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetFilesAsync" />: a clipboard that refuses the files raises no error and answers
	/// <c>false</c>.
	/// </summary>
	[Test]
	public async Task SetFilesAsync_Answers_False_When_The_Clipboard_Refuses()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			IStorageAccessor storage = Substitute.For<IStorageAccessor>();

			IStorageFile file = Substitute.For<IStorageFile>();

			// On Linux the path also goes into the format of the file managers.
			file.Path.Returns(new Uri(FilePath));

			storage
				.TryGetFileFromPathAsync(FilePath)
				.Returns(file);

			clipboard
				.SetDataAsync(Arg.Any<DataTransfer>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(storage);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool isWritten = await sut.SetFilesAsync([new ClipboardFileSystemEntry(FilePath, IsFolder: false)], isRestored: false);

		// Assert
		isWritten
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetFilesAsync" />: a file whose lookup fails is left out, and the others still go to the
	/// clipboard.
	/// </summary>
	[Test]
	public async Task SetFilesAsync_Leaves_Out_A_File_Whose_Lookup_Fails()
	{
		// Arrange
		string failingPath = TestPath.Combine(FolderName, "locked.txt");

		IStorageFile file = Substitute.For<IStorageFile>();

		DataTransfer? placed = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			IStorageAccessor storage = Substitute.For<IStorageAccessor>();

			// On Linux the path also goes into the format of the file managers.
			file.Path.Returns(new Uri(FilePath));

			storage
				.TryGetFileFromPathAsync(failingPath)
				.ThrowsAsync(new UnauthorizedAccessException());

			storage
				.TryGetFileFromPathAsync(FilePath)
				.Returns(file);

			clipboard.SetDataAsync(Arg.Do<DataTransfer>(x => placed = x));

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(storage);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		await sut.SetFilesAsync(
		[
			new ClipboardFileSystemEntry(failingPath, IsFolder: false),
			new ClipboardFileSystemEntry(FilePath, IsFolder: false)
		], isRestored: false);

		// Assert
		placed!.TryGetFiles()
			.Should()
			.Equal(file);
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetFilesAsync" />: a list whose files are all gone leaves the clipboard alone and answers
	/// <c>false</c>.
	/// </summary>
	[Test]
	public async Task SetFilesAsync_Leaves_The_Clipboard_When_No_File_Is_Found()
	{
		// Arrange
		IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IStorageAccessor storage = Substitute.For<IStorageAccessor>();

			// The file is gone.
			storage
				.TryGetFileFromPathAsync(Arg.Any<string>())
				.Returns((IStorageFile?)null);

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(storage);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool isWritten = await sut.SetFilesAsync([new ClipboardFileSystemEntry(TestPath.Combine("file.txt"), IsFolder: false)], isRestored: false);

		// Assert
		isWritten
			.Should()
			.BeFalse();

		await clipboard
			.DidNotReceive()
			.SetDataAsync(Arg.Any<DataTransfer>());
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetFilesAsync" />: only files put back from the clipboard log carry the restore marker.
	/// </summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task SetFilesAsync_Marks_Only_Restored_Files(bool isRestored)
	{
		// Arrange
		DataTransfer? placed = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			IStorageAccessor storage = Substitute.For<IStorageAccessor>();

			IStorageFile file = Substitute.For<IStorageFile>();

			// On Linux the path also goes into the format of the file managers.
			file.Path.Returns(new Uri(FilePath));

			storage
				.TryGetFileFromPathAsync(FilePath)
				.Returns(file);

			clipboard.SetDataAsync(Arg.Do<DataTransfer>(x => placed = x));

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(storage);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		await sut.SetFilesAsync([new ClipboardFileSystemEntry(FilePath, IsFolder: false)], isRestored);

		// Assert
		ClipboardRestoreMarker.IsAmong(placed!.Formats)
			.Should()
			.Be(isRestored);
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetFilesAsync" />: the files and folders that are still there go to the clipboard, and
	/// the missing ones are left out; the write answers <c>true</c>.
	/// </summary>
	[Test]
	public async Task SetFilesAsync_Puts_The_Found_Files_On_The_Clipboard()
	{
		// Arrange
		string missingPath = TestPath.Combine(FolderName, "gone.txt");

		IStorageFile file = Substitute.For<IStorageFile>();

		IStorageFolder folder = Substitute.For<IStorageFolder>();

		DataTransfer? placed = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			IStorageAccessor storage = Substitute.For<IStorageAccessor>();

			// On Linux the paths also go into the format of the file managers.
			file.Path.Returns(new Uri(FilePath));

			folder.Path.Returns(new Uri(SubfolderPath));

			storage
				.TryGetFileFromPathAsync(FilePath)
				.Returns(file);

			storage
				.TryGetFolderFromPathAsync(SubfolderPath)
				.Returns(folder);

			// The third one is gone.
			storage
				.TryGetFileFromPathAsync(missingPath)
				.Returns((IStorageFile?)null);

			clipboard.SetDataAsync(Arg.Do<DataTransfer>(x => placed = x));

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(storage);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool isWritten = await sut.SetFilesAsync(
		[
			new ClipboardFileSystemEntry(FilePath, IsFolder: false),
			new ClipboardFileSystemEntry(SubfolderPath, IsFolder: true),
			new ClipboardFileSystemEntry(missingPath, IsFolder: false)
		], isRestored: false);

		// Assert
		isWritten
			.Should()
			.BeTrue();

		placed!.TryGetFiles()
			.Should()
			.Equal(file, folder);
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetImageAsync" />: a clipboard that refuses the picture raises no error and answers
	/// <c>false</c>.
	/// </summary>
	[AvaloniaTest]
	public async Task SetImageAsync_Answers_False_When_The_Clipboard_Refuses()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.SetDataAsync(Arg.Any<DataTransfer>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool isWritten = await sut.SetImageAsync(ImageFactory.CreatePng(), isRestored: false);

		// Assert
		isWritten
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetImageAsync" />: a picture put back from the clipboard log carries the restore marker.
	/// </summary>
	[AvaloniaTest]
	public async Task SetImageAsync_Marks_A_Restored_Picture()
	{
		// Arrange
		IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(clipboard));

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		await sut.SetImageAsync(ImageFactory.CreatePng(), isRestored: true);

		// Assert
		await clipboard
			.Received(1)
			.SetDataAsync(Arg.Is<DataTransfer>(x => x.TryGetBitmap() != null && ClipboardRestoreMarker.IsAmong(x.Formats)));
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetImageAsync" />: the picture of the PNG goes to the clipboard, without markers, and
	/// the write answers <c>true</c>.
	/// </summary>
	[AvaloniaTest]
	public async Task SetImageAsync_Puts_The_Picture_Of_The_Png_On_The_Clipboard()
	{
		// Arrange
		IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(clipboard));

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool isWritten = await sut.SetImageAsync(ImageFactory.CreatePng(), isRestored: false);

		// Assert
		isWritten
			.Should()
			.BeTrue();

		await clipboard
			.Received(1)
			.SetDataAsync(Arg.Is<DataTransfer>(x => x.TryGetBitmap() != null && x.Formats.Count == 1));
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetTextAsync" />: a text put back from the clipboard log carries the restore marker.
	/// </summary>
	[Test]
	public async Task SetTextAsync_Marks_A_Restored_Text()
	{
		// Arrange
		string text = RandomString.Create(16);

		IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(clipboard));

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		await sut.SetTextAsync(text, html: null, rtf: null, isSensitive: false, isRestored: true);

		// Assert
		await clipboard
			.Received(1)
			.SetDataAsync(Arg.Is<DataTransfer>(x => x.TryGetText() == text && ClipboardRestoreMarker.IsAmong(x.Formats)));
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetTextAsync" />: a sensitive text goes to the clipboard marked as a secret.
	/// </summary>
	[Test]
	public async Task SetTextAsync_Marks_A_Sensitive_Text_As_Secret()
	{
		// Arrange
		string text = RandomString.Create(16);

		IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(clipboard));

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.SetTextAsync(text, html: null, rtf: null, isSensitive: true, isRestored: false);

		// Assert
		result
			.Should()
			.BeTrue();

		await clipboard
			.Received(1)
			.SetDataAsync(Arg.Is<DataTransfer>(x => x.TryGetText() == text
				&& ClipboardSensitivityMarkerWriter.ContainsOwnershipMarker(x.Formats)));
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetTextAsync" />: an ordinary text reaches the clipboard as plain text, without markers.
	/// </summary>
	[Test]
	public async Task SetTextAsync_Places_Plain_Text_For_An_Ordinary_Text()
	{
		// Arrange
		string text = RandomString.Create(16);

		IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(clipboard));

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		await sut.SetTextAsync(text, html: null, rtf: null, isSensitive: false, isRestored: false);

		// Assert
		await clipboard
			.Received(1)
			.SetDataAsync(Arg.Is<DataTransfer>(x => x.TryGetText() == text && x.Formats.Count == 1));
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetTextAsync" />: a text goes to the clipboard with its HTML and RTF.
	/// </summary>
	[Test]
	public async Task SetTextAsync_Puts_The_Html_And_Rtf_On_The_Clipboard()
	{
		// Arrange
		const string html = "<b>hello</b>";

		const string rtf = @"{\rtf1 hello}";

		DataTransfer? placed = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard.SetDataAsync(Arg.Do<DataTransfer>(x => placed = x));

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		await sut.SetTextAsync("hello", html, rtf, isSensitive: false, isRestored: false);

		// Assert
		DataTransfer transfer = placed!;

		transfer.TryGetText()
			.Should()
			.Be("hello");

		IEnumerable<string> payloads = transfer
			.Formats
			.OfType<DataFormat<byte[]>>()
			.Select(x => TextDefaults.Encoding.GetString(transfer.TryGetValue(x)!));

		payloads
			.Should()
			.BeEquivalentTo(html, rtf);
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.SetTextAsync" />: a clipboard that refuses the text is reported.
	/// </summary>
	[Test]
	public async Task SetTextAsync_Reports_A_Refused_Write()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.SetDataAsync(Arg.Any<DataTransfer>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		bool result = await sut.SetTextAsync(RandomString.Create(16), html: null, rtf: null, isSensitive: false, isRestored: false);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.TryReadFilesAsync" />: a clipboard that fails to give its files gives none.
	/// </summary>
	[Test]
	public async Task TryReadFilesAsync_Is_Null_When_The_Clipboard_Fails()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.TryGetFilesAsync()
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		IReadOnlyList<ClipboardFileSystemEntry>? result = await sut.TryReadFilesAsync();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.TryReadFilesAsync" />: folders come before files.
	/// </summary>
	[Test]
	public async Task TryReadFilesAsync_Puts_Folders_First()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			IStorageFile file = Substitute.For<IStorageFile>();

			IStorageFolder folder = Substitute.For<IStorageFolder>();

			file.Path.Returns(new Uri(FilePath));

			folder.Path.Returns(new Uri(SubfolderPath));

			clipboard
				.TryGetFilesAsync()
				.Returns([file, folder]);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		IReadOnlyList<ClipboardFileSystemEntry>? result = await sut.TryReadFilesAsync();

		// Assert
		result!.Select(static x => x.IsFolder)
			.Should()
			.Equal(true, false);
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.TryReadFilesAsync" />: items without an absolute path are skipped.
	/// </summary>
	[Test]
	public async Task TryReadFilesAsync_Skips_Items_Without_An_Absolute_Path()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			IStorageFile valid = Substitute.For<IStorageFile>();

			IStorageFile relative = Substitute.For<IStorageFile>();

			valid.Path.Returns(new Uri(FilePath));

			relative.Path.Returns(new Uri("b.txt", UriKind.Relative));

			clipboard
				.TryGetFilesAsync()
				.Returns([valid, relative]);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		IReadOnlyList<ClipboardFileSystemEntry>? result = await sut.TryReadFilesAsync();

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Name
			.Should()
			.Be(FileName);
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.TryReadFilesAsync" />: files of one kind come in the order of their names, whatever the
	/// case of the letters.
	/// </summary>
	[Test]
	public async Task TryReadFilesAsync_Sorts_By_Name_Within_A_Kind()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			IStorageFile upper = Substitute.For<IStorageFile>();

			IStorageFile lower = Substitute.For<IStorageFile>();

			upper.Path.Returns(new Uri(TestPath.Combine(FolderName, "B.txt")));

			lower.Path.Returns(new Uri(TestPath.Combine(FolderName, "a.txt")));

			clipboard
				.TryGetFilesAsync()
				.Returns([upper, lower]);

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		IReadOnlyList<ClipboardFileSystemEntry>? result = await sut.TryReadFilesAsync();

		// Assert
		result!.Select(static x => x.Name)
			.Should()
			.Equal("a.txt", "B.txt");
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.TryReadHtmlAsync" />: a clipboard that fails to give its HTML gives none.
	/// </summary>
	[Test]
	public async Task TryReadHtmlAsync_Is_Null_When_The_Clipboard_Fails()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.TryGetValueAsync(Arg.Any<DataFormat<byte[]>>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		string? result = await sut.TryReadHtmlAsync();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.TryReadHtmlAsync" />: the HTML is read from the HTML format of the platform.
	/// </summary>
	[Test]
	public async Task TryReadHtmlAsync_Reads_The_Html_Format()
	{
		// Arrange
		const string html = "<b>hello</b>";

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			// The names of the format on Windows, macOS and Linux.
			clipboard
				.TryGetValueAsync(Arg.Is<DataFormat<byte[]>>(format => format!.Identifier == "HTML Format"
					|| format.Identifier == "public.html"
					|| format.Identifier == "text/html"))
				.Returns(TextDefaults.Encoding.GetBytes(html));

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		string? result = await sut.TryReadHtmlAsync();

		// Assert
		result
			.Should()
			.Be(html);
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.TryReadImagePngAsync" />: a clipboard that fails to give its image gives none.
	/// </summary>
	[Test]
	public async Task TryReadImagePngAsync_Is_Null_When_The_Clipboard_Fails()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.TryGetBitmapAsync()
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		byte[]? result = await sut.TryReadImagePngAsync();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.TryReadRtfAsync" />: the RTF is read from the RTF format of the platform.
	/// </summary>
	[Test]
	public async Task TryReadRtfAsync_Reads_The_Rtf_Format()
	{
		// Arrange
		const string rtf = @"{\rtf1 hello}";

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			// The names of the format on Windows, macOS and Linux.
			clipboard
				.TryGetValueAsync(Arg.Is<DataFormat<byte[]>>(format => format!.Identifier == "Rich Text Format"
					|| format.Identifier == "public.rtf"
					|| format.Identifier == "text/rtf"))
				.Returns(TextDefaults.Encoding.GetBytes(rtf));

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		string? result = await sut.TryReadRtfAsync();

		// Assert
		result
			.Should()
			.Be(rtf);
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.TryReadTextAsync" />: a clipboard that fails to give its text gives none.
	/// </summary>
	[Test]
	public async Task TryReadTextAsync_Is_Null_When_The_Clipboard_Fails()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.TryGetTextAsync()
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		string? result = await sut.TryReadTextAsync();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardAccessor.TryReadTextAsync" />: the text on the clipboard is read.
	/// </summary>
	[Test]
	public async Task TryReadTextAsync_Reads_The_Text()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAvaloniaClipboard clipboard = Substitute.For<IAvaloniaClipboard>();

			clipboard
				.TryGetTextAsync()
				.Returns("hello");

			builder.RegisterInstance(clipboard);
		});

		ClipboardAccessor sut = mock.Create<ClipboardAccessor>();

		// Act
		string? result = await sut.TryReadTextAsync();

		// Assert
		result
			.Should()
			.Be("hello");
	}
	#endregion
}
