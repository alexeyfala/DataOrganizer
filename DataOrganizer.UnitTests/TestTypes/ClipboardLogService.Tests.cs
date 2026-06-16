using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Messages;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.UnitTests.Helpers;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardLogService)}"" type")]
internal class ClipboardLogServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardLogService.BuildTextEntry" />: plain text becomes a text entry.
	/// </summary>
	[Test]
	public void BuildTextEntry_Builds_Text_Entry_For_Plain_Text()
	{
		// Act
		ClipboardHistoryEntryBase entry = ClipboardLogService.BuildTextEntry("just text", "<b>x</b>", null, [1]);

		// Assert
		ClipboardTextEntry text = entry
			.Should()
			.BeOfType<ClipboardTextEntry>()
			.Subject;

		text.Text
			.Should()
			.Be("just text");

		text.Html
			.Should()
			.Be("<b>x</b>");
	}

	/// <summary>
	/// <see cref="ClipboardLogService.BuildTextEntry" />: whole-string URL text becomes a URL entry (trimmed).
	/// </summary>
	[Test]
	public void BuildTextEntry_Builds_Url_Entry_For_Url_Text()
	{
		// Act
		ClipboardHistoryEntryBase entry = ClipboardLogService.BuildTextEntry(
			text: "  https://example.com/x  ",
			html: null,
			rtf: null,
			hash: [1]);

		// Assert
		ClipboardUrlEntry url = entry
			.Should()
			.BeOfType<ClipboardUrlEntry>()
			.Subject;

		url.Url
			.Should()
			.Be("https://example.com/x");

		url.Text
			.Should()
			.Be("  https://example.com/x  ");
	}

	/// <summary>
	/// <see cref="ClipboardLogService.HandleNewPayload" />: the captured entry becomes the active one.
	/// </summary>
	[Test]
	public void Capture_Marks_Entry_Active_Clearing_Previous()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry first = TextEntry("a", [1]);

		ClipboardTextEntry second = TextEntry("b", [2]);

		sut.HandleNewPayload([1], () => first, isSensitive: false);

		// Act
		sut.HandleNewPayload([2], () => second, isSensitive: false);

		// Assert
		second.IsActive
			.Should()
			.BeTrue();

		first.IsActive
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.HandleNewPayload" />: pinned entries are exempt from the cap.
	/// </summary>
	[Test]
	public void Capture_Trims_Only_Unpinned_Keeping_Pinned()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry pinned = PinnedTextEntry("pinned", [200]);

		sut.Entries.Add(pinned);

		for (int i = 0; i < 25; i++)
		{
			sut.Entries.Add(TextEntry($"e{i}", [(byte)i]));
		}

		// Act (cap applies to the 25 unpinned only, so the pinned entry survives).
		sut.HandleNewPayload([99], () => TextEntry("new", [99]), isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.HaveCount(26);

		sut.Entries
			.Should()
			.Contain(pinned);

		sut.Entries[0]
			.Should()
			.Be(pinned);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.ClearAsync" />: the active highlight is cleared from a surviving pin.
	/// </summary>
	[Test]
	public async Task ClearAsync_Clears_Active_On_Surviving_Pinned()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry pinned = PinnedTextEntry("p", [1]);

		sut.Entries.Add(pinned);

		// Restoring marks it active.
		await sut.RestoreAsync(pinned);

		// Act
		await sut.ClearAsync();

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(pinned);

		pinned.IsActive
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.ClearAsync" />: pinned entries survive and Updated is raised.
	/// </summary>
	[Test]
	public async Task ClearAsync_Preserves_Pinned_And_Raises_Updated()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry pinned = PinnedTextEntry("p", [1]);

		sut.Entries.Add(pinned);

		sut.Entries.Add(TextEntry("u", [2]));

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act
		await sut.ClearAsync();

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(pinned);

		received
			.Should()
			.Equal(ClipboardHistoryChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.ClearAsync" />: clears entries and raises ClearedByUser.
	/// </summary>
	[Test]
	public async Task ClearAsync_Raises_ClearedByUser()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		sut.Entries.Add(TextEntry("a", [1]));

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act
		await sut.ClearAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();

		received
			.Should()
			.Equal(ClipboardHistoryChangeKind.ClearedByUser);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.ClearEntriesAsync" />: clears entries and raises ClearedForStop.
	/// </summary>
	[Test]
	public async Task ClearEntriesAsync_Raises_ClearedForStop()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		sut.Entries.Add(TextEntry("a", [1]));

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act
		await sut.ClearEntriesAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();

		received
			.Should()
			.Equal(ClipboardHistoryChangeKind.ClearedForStop);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.ComputeTextEntryHash" />: plain and formatted text hash differently.
	/// </summary>
	[Test]
	public void ComputeTextEntryHash_Differs_Between_Plain_And_Formatted()
	{
		// Arrange
		byte[] plain = ClipboardLogService.ComputeTextEntryHash("t", null, null);

		byte[] withHtml = ClipboardLogService.ComputeTextEntryHash("t", "<b>x</b>", null);

		byte[] withRtf = ClipboardLogService.ComputeTextEntryHash("t", null, @"{\rtf1 x}");

		// Act, Assert
		plain
			.Should()
			.NotEqual(withHtml);

		plain
			.Should()
			.NotEqual(withRtf);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.ComputeTextEntryHash" />: only the presence of companion
	/// formats matters, not their (delayed-rendered) payloads.
	/// </summary>
	[Test]
	public void ComputeTextEntryHash_Ignores_Companion_Payload_Differences()
	{
		// Act, Assert
		ClipboardLogService.ComputeTextEntryHash("t", "<a>one</a>", null)
			.Should()
			.Equal(ClipboardLogService.ComputeTextEntryHash("t", "<b>two</b>", null));
	}

	/// <summary>
	/// <see cref="ClipboardLogService.ComputeTextEntryHash" />: the same inputs hash identically.
	/// </summary>
	[Test]
	public void ComputeTextEntryHash_Is_Deterministic()
	{
		// Act, Assert
		ClipboardLogService.ComputeTextEntryHash("t", "<b>x</b>", null)
			.Should()
			.Equal(ClipboardLogService.ComputeTextEntryHash("t", "<b>x</b>", null));
	}

	/// <summary>
	/// <see cref="ClipboardLogService.DisposeAsync" />: disposing twice is safe.
	/// </summary>
	[Test]
	public async Task DisposeAsync_Is_Idempotent()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		await sut.DisposeAsync();

		// Act
		Func<Task> act = async () => await sut.DisposeAsync();

		// Assert
		await act
			.Should()
			.NotThrowAsync();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.HandleNewPayload" />: capture enforces the history cap.
	/// </summary>
	[Test]
	public void HandleNewPayload_Enforces_History_Cap()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMessenger messenger = new WeakReferenceMessenger();

			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		for (int i = 0; i < 25; i++)
		{
			sut.Entries.Add(TextEntry($"e{i}", [(byte)i]));
		}

		// Act
		sut.HandleNewPayload([99], () => TextEntry("new", [99]), isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.HaveCount(25);

		sut.Entries[0]
			.Hash
			.Should()
			.Equal("c"u8.ToArray());
	}

	/// <summary>
	/// <see cref="ClipboardLogService.HandleNewPayload" />: an unchanged payload is ignored.
	/// </summary>
	[Test]
	public void HandleNewPayload_Ignores_Unchanged_Payload()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		sut.HandleNewPayload([1], () => TextEntry("a", [1]), isSensitive: false);

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act (same hash as the last observed payload).
		sut.HandleNewPayload([1], static () => throw new InvalidOperationException("Factory should not be invoked."), isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.ContainSingle();

		received
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.HandleNewPayload" />: a new payload is inserted at the top.
	/// </summary>
	[Test]
	public void HandleNewPayload_Inserts_New_Entry_At_Top()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		ClipboardTextEntry entry = TextEntry("a", [1]);

		// Act
		sut.HandleNewPayload([1], () => entry, isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(entry);

		received
			.Should()
			.Equal(ClipboardHistoryChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.HandleNewPayload" />: a matching hash moves the existing entry up.
	/// </summary>
	[Test]
	public void HandleNewPayload_Moves_Existing_Entry_To_Top()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry target = TextEntry("target", [1]);

		sut.Entries.Add(TextEntry("top", [9]));

		sut.Entries.Add(target);

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act (the factory must not run — the entry already exists).
		sut.HandleNewPayload([1], static () => throw new InvalidOperationException("Factory should not be invoked."), isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.HaveCount(2);

		sut.Entries[0]
			.Should()
			.Be(target);

		received
			.Should()
			.Contain(ClipboardHistoryChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.HashFiles" />: same path as a folder vs a file hashes differently.
	/// </summary>
	[Test]
	public void HashFiles_Distinguishes_Folder_From_File()
	{
		// Arrange
		byte[] asFolder = ClipboardLogService.HashFiles([new ClipboardFileSystemEntry("C:\\x", IsFolder: true)]);

		byte[] asFile = ClipboardLogService.HashFiles([new ClipboardFileSystemEntry("C:\\x", IsFolder: false)]);

		// Act, Assert
		asFolder
			.Should()
			.NotEqual(asFile);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.HashFiles" />: the same list hashes identically.
	/// </summary>
	[Test]
	public void HashFiles_Is_Deterministic()
	{
		// Arrange
		ClipboardFileSystemEntry[] list = [new("C:\\a", IsFolder: false), new("C:\\b", IsFolder: true)];

		// Act, Assert
		ClipboardLogService.HashFiles(list)
			.Should()
			.Equal(ClipboardLogService.HashFiles(list));
	}

	/// <summary>
	/// <see cref="ClipboardLogService.HashFiles" />: ordering of the items affects the hash.
	/// </summary>
	[Test]
	public void HashFiles_Is_Order_Sensitive()
	{
		// Arrange
		ClipboardFileSystemEntry a = new("C:\\a", IsFolder: false);

		ClipboardFileSystemEntry b = new("C:\\b", IsFolder: false);

		// Act, Assert
		ClipboardLogService.HashFiles([a, b])
			.Should()
			.NotEqual(ClipboardLogService.HashFiles([b, a]));
	}

	/// <summary>
	/// <see cref="ClipboardLogService.StartAsync" /> / <see cref="ClipboardLogService.DisposeAsync" />:
	/// the running flag toggles around the loop's lifetime.
	/// </summary>
	[Test]
	public async Task IsRunning_Toggles_With_Start_And_Dispose()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act, Assert
		await sut.StartAsync();

		sut.IsRunning
			.Should()
			.BeTrue();

		await sut.DisposeAsync();

		sut.IsRunning
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.Merge" />: appends below current, dedupes by hash, no message.
	/// </summary>
	[Test]
	public void Merge_Appends_Below_Current_Skipping_Duplicates()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		sut.Entries.Add(TextEntry("C", [2]));

		sut.Entries.Add(TextEntry("D", [9]));

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act ("B" duplicates current "C" by hash [2] and is skipped).
		sut.Merge([TextEntry("A", [1]), TextEntry("B", [2])]);

		// Assert
		sut.Entries
			.Cast<ClipboardTextEntry>()
			.Select(x => x.Text)
			.Should()
			.Equal("C", "D", "A");

		received
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.Merge" />: the history cap is enforced.
	/// </summary>
	[Test]
	public void Merge_Enforces_History_Cap()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMessenger messenger = new WeakReferenceMessenger();

			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		for (int i = 0; i < 25; i++)
		{
			sut.Entries.Add(TextEntry($"cur{i}", [(byte)i]));
		}

		// Act
		sut.Merge([TextEntry("overflow", [200])]);

		// Assert
		sut.Entries
			.Should()
			.HaveCount(25);

		sut.Entries
			.Cast<ClipboardTextEntry>()
			.Should()
			.NotContain(x => x.Text == "overflow");
	}

	/// <summary>
	/// <see cref="ClipboardLogService.Merge" />: pinned entries are placed atop, keeping the invariant.
	/// </summary>
	[Test]
	public void Merge_Places_Pinned_Atop_Preserving_Invariant()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		sut.Entries.Add(TextEntry("newText", [9]));

		// Act
		sut.Merge(
		[
			PinnedTextEntry("p0", [1]),
			PinnedTextEntry("p1", [2]),
			TextEntry("u", [3])
		]);

		// Assert
		sut.Entries
			.Select(static entry => entry.Hash[0])
			.Should()
			.Equal(1, 2, 9, 3);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: files are captured with folders sorted first.
	/// </summary>
	[Test]
	public async Task PollOnce_Captures_Files_Entry_Folders_First()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		clipboard
			.GetDataFormatsAsync()
			.Returns([]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		IStorageFile file = Substitute.For<IStorageFile>();

		file.Path.Returns(new Uri("file:///C:/dir/a.txt"));

		IStorageFolder folder = Substitute.For<IStorageFolder>();

		folder.Path.Returns(new Uri("file:///C:/dir/sub"));

		clipboard
			.TryGetFilesAsync()
			.Returns([file, folder]);

		// Act
		await sut.PollOnceAsync();

		// Assert
		ClipboardFilesEntry entry = sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<ClipboardFilesEntry>()
			.Subject;

		entry.FileSystemEntries
			.Should()
			.HaveCount(2);

		entry.FileSystemEntries[0]
			.IsFolder
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: plain text is captured as a text entry.
	/// </summary>
	[Test]
	public async Task PollOnce_Captures_Text_Entry()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		clipboard
			.GetDataFormatsAsync()
			.Returns([]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		clipboard
			.TryGetTextAsync()
			.Returns("hello");

		// Act
		await sut.PollOnceAsync();

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<ClipboardTextEntry>()
			.Which
			.Text
			.Should()
			.Be("hello");
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: CanIncludeInClipboardHistory = 1 (allowed)
	/// is not treated as sensitive — the entry is captured (e.g. content restored via Win+V).
	/// </summary>
	[Test]
	public async Task PollOnce_Captures_When_History_Flag_Is_Allowed()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		clipboard
			.GetDataFormatsAsync()
			.Returns([DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanIncludeInClipboardHistory)]);

		clipboard
			.TryGetValueAsync(Arg.Is<DataFormat<byte[]>>(format => format.Identifier == ClipboardSensitivityMarkers.CanIncludeInClipboardHistory))
			.Returns([1, 0, 0, 0]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		clipboard
			.TryGetTextAsync()
			.Returns("allowed");

		// Act
		await sut.PollOnceAsync();

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<ClipboardTextEntry>()
			.Which
			.Text
			.Should()
			.Be("allowed");
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: content re-published from Windows clipboard history
	/// (ClipboardHistoryItemId present) is captured, even though it carries the exclude marker (anti-loop, not secrecy).
	/// </summary>
	[Test]
	public async Task PollOnce_Captures_Win_V_Restored_Content_Despite_Exclude_Marker()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		// Order mirrors a real Win+V restore: the exclude marker precedes the history id.
		clipboard
			.GetDataFormatsAsync()
			.Returns(
			[
				DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.ExcludeFromMonitorProcessing),
				DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.ClipboardHistoryItemId)
			]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		clipboard
			.TryGetTextAsync()
			.Returns("restored from Win+V");

		// Act
		await sut.PollOnceAsync();

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<ClipboardTextEntry>()
			.Which
			.Text
			.Should()
			.Be("restored from Win+V");
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: an emptied clipboard drops the active highlight.
	/// </summary>
	[Test]
	public async Task PollOnce_Clears_Active_When_Clipboard_Emptied()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.GetDataFormatsAsync()
				.Returns([]);

			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry entry = TextEntry("a", [1]);

		// Capturing marks the entry active; the clipboard now holds nothing capturable.
		sut.HandleNewPayload([1], () => entry, isSensitive: false);

		entry.IsActive
			.Should()
			.BeTrue();

		// Act
		await sut.PollOnceAsync();

		// Assert
		entry.IsActive
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: files without an absolute path are skipped.
	/// </summary>
	[Test]
	public async Task PollOnce_Skips_Files_Without_Absolute_Path()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		clipboard
			.GetDataFormatsAsync()
			.Returns([]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		IStorageFile valid = Substitute.For<IStorageFile>();

		valid.Path.Returns(new Uri("file:///C:/dir/a.txt"));

		IStorageFile relative = Substitute.For<IStorageFile>();

		relative.Path.Returns(new Uri("a.txt", UriKind.Relative));

		clipboard
			.TryGetFilesAsync()
			.Returns([valid, relative]);

		// Act
		await sut.PollOnceAsync();

		// Assert
		ClipboardFilesEntry entry = sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<ClipboardFilesEntry>()
			.Subject;

		entry.FileSystemEntries
			.Should()
			.ContainSingle()
			.Which
			.IsFolder
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: a sensitivity marker skips the entry.
	/// </summary>
	[Test]
	public async Task PollOnce_Skips_Sensitive_Content()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		clipboard
			.GetDataFormatsAsync()
			.Returns([]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		clipboard
			.GetDataFormatsAsync()
			.Returns([DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.ExcludeFromMonitorProcessing)]);

		clipboard
			.TryGetTextAsync()
			.Returns("super-secret");

		// Act
		await sut.PollOnceAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: CanIncludeInClipboardHistory = 0 (exclude)
	/// is treated as sensitive — the entry is skipped.
	/// </summary>
	[Test]
	public async Task PollOnce_Skips_When_History_Flag_Excludes()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		clipboard
			.GetDataFormatsAsync()
			.Returns([DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanIncludeInClipboardHistory)]);

		clipboard
			.TryGetValueAsync(Arg.Is<DataFormat<byte[]>>(format => format.Identifier == ClipboardSensitivityMarkers.CanIncludeInClipboardHistory))
			.Returns([0, 0, 0, 0]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		clipboard
			.TryGetTextAsync()
			.Returns("excluded");

		// Act
		await sut.PollOnceAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Re-baseline path: a restored pinned entry keeps its pinned state on the replacement.
	/// </summary>
	[Test]
	public async Task Rebaseline_Carries_Pin_State()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry original = PinnedTextEntry("orig", [1]);

		sut.Entries.Add(original);

		await sut.RestoreAsync(original);

		ClipboardTextEntry rebaselined = TextEntry("rebased", [2]);

		// Act (the clipboard handed back a different representation -> different hash).
		sut.HandleNewPayload([2], () => rebaselined, isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(rebaselined);

		rebaselined.IsPinned
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RemoveAsync" />: a missing entry is a no-op and raises no notification.
	/// </summary>
	[Test]
	public async Task Remove_Missing_Entry_Is_NoOp()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		sut.Entries.Add(TextEntry("a", [1]));

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act (the entry was never added).
		await sut.RemoveAsync(TextEntry("absent", [9]));

		// Assert
		sut.Entries
			.Should()
			.ContainSingle();

		received
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RemoveAsync" />: removing the active entry empties the system clipboard.
	/// </summary>
	[Test]
	public async Task Remove_Of_Active_Entry_Empties_System_Clipboard()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry entry = TextEntry("a", [1]);

		// Capturing marks the entry active (its content is the one held in the system clipboard).
		sut.HandleNewPayload([1], () => entry, isSensitive: false);

		// Act
		await sut.RemoveAsync(entry);

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();

		await clipboard
			.Received()
			.ClearAsync();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RemoveAsync" />: after removing the active entry the emptied clipboard
	/// is not re-captured by the next poll tick.
	/// </summary>
	[Test]
	public async Task Remove_Of_Active_Entry_Is_Not_Recaptured_By_Next_Poll()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		clipboard
			.GetDataFormatsAsync()
			.Returns([]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry entry = TextEntry("a", [1]);

		sut.HandleNewPayload([1], () => entry, isSensitive: false);

		// Act (the content is gone from the clipboard, so the poll sees nothing capturable).
		await sut.RemoveAsync(entry);

		await sut.PollOnceAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RemoveAsync" />: removing a non-active entry leaves the system clipboard intact.
	/// </summary>
	[Test]
	public async Task Remove_Of_Inactive_Entry_Leaves_System_Clipboard()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry old = TextEntry("old", [2]);

		ClipboardTextEntry active = TextEntry("active", [1]);

		sut.Entries.Add(old);

		// The newest capture is the active one; "old" sits below it and is not on the system clipboard.
		sut.HandleNewPayload([1], () => active, isSensitive: false);

		// Act (remove the non-active entry).
		await sut.RemoveAsync(old);

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(active);

		await clipboard
			.DidNotReceive()
			.ClearAsync();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RemoveAsync" />: a pinned entry is removed and Updated is raised.
	/// </summary>
	[Test]
	public async Task Remove_Removes_Pinned_Entry_And_Raises_Updated()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry pinned = PinnedTextEntry("p", [1]);

		sut.Entries.Add(pinned);

		sut.Entries.Add(TextEntry("u", [2]));

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act
		await sut.RemoveAsync(pinned);

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Hash[0]
			.Should()
			.Be(2);

		received
			.Should()
			.Contain(ClipboardHistoryChangeKind.Updated);
	}

	/// <summary>
	/// Test of the re-baseline path when the restored entry is no longer present: it is inserted at the top.
	/// </summary>
	[Test]
	public async Task Restore_Of_Missing_Entry_Then_Differing_Capture_Inserts_Rebaselined()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry original = TextEntry("orig", [1]);

		sut.Entries.Add(original);

		await sut.RestoreAsync(original);

		// The restored entry is gone by the time the next capture arrives.
		sut.Entries.Clear();

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		ClipboardTextEntry rebaselined = TextEntry("rebased", [2]);

		// Act
		sut.HandleNewPayload([2], () => rebaselined, isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(rebaselined);

		received
			.Should()
			.Contain(ClipboardHistoryChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: restoring the top entry raises no notification.
	/// </summary>
	[Test]
	public async Task Restore_Of_Top_Entry_Raises_No_Notification()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry top = TextEntry("top", [1]);

		sut.Entries.Add(top);

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act (the entry is already at index 0).
		await sut.RestoreAsync(top);

		// Assert
		received
			.Should()
			.BeEmpty();

		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(top);
	}

	/// <summary>
	/// Test of the re-baseline path: a restored entry whose hash changed on the next capture is replaced.
	/// </summary>
	[Test]
	public async Task Restore_Then_Differing_Capture_Rebaselines_Entry()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry original = TextEntry("orig", [1]);

		sut.Entries.Add(original);

		await sut.RestoreAsync(original);

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		ClipboardTextEntry rebaselined = TextEntry("rebased", [2]);

		// Act (the clipboard handed back a different representation -> different hash).
		sut.HandleNewPayload([2], () => rebaselined, isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(rebaselined);

		received
			.Should()
			.Contain(ClipboardHistoryChangeKind.Updated);
	}

	/// <summary>
	/// Test of the re-baseline path: a restored entry observed with the same hash is left untouched.
	/// </summary>
	[Test]
	public async Task Restore_Then_Same_Capture_Keeps_Entry()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry original = TextEntry("orig", [1]);

		sut.Entries.Add(original);

		await sut.RestoreAsync(original);

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		bool built = false;

		// Act (same hash as the restored entry — nothing to re-baseline).
		sut.HandleNewPayload([1], () =>
		{
			built = true;

			return TextEntry("x", [1]);
		}, isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(original);

		built
			.Should()
			.BeFalse();

		received
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: the restored entry becomes the active one.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Marks_Entry_Active()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry other = TextEntry("other", [1]);

		ClipboardTextEntry target = TextEntry("target", [2]);

		sut.Entries.Add(other);

		sut.Entries.Add(target);

		// Act
		await sut.RestoreAsync(target);

		// Assert
		target.IsActive
			.Should()
			.BeTrue();

		other.IsActive
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: moves the entry to the top and raises Updated.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Moves_To_Top_And_Raises_Updated()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry target = TextEntry("target", [2]);

		sut.Entries.Add(TextEntry("other", [1]));

		sut.Entries.Add(target);

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act
		await sut.RestoreAsync(target);

		// Assert
		sut.Entries[0]
			.Should()
			.Be(target);

		received
			.Should()
			.Contain(ClipboardHistoryChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: keeping the position only highlights the entry, it does not move.
	/// </summary>
	[Test]
	public async Task RestoreAsync_With_KeepPosition_Marks_Active_Without_Moving()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry first = TextEntry("first", [1]);

		ClipboardTextEntry target = TextEntry("target", [2]);

		sut.Entries.Add(first);

		sut.Entries.Add(target);

		// Act
		await sut.RestoreAsync(target, keepPosition: true);

		// Assert
		sut.Entries[1]
			.Should()
			.Be(target);

		target.IsActive
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.StartAsync" />: a disposed service does not start.
	/// </summary>
	[Test]
	public async Task StartAsync_After_Dispose_Does_Not_Run()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		await sut.DisposeAsync();

		// Act
		await sut.StartAsync();

		// Assert
		sut.IsRunning
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.Stop" />: stopping without a running loop is a no-op.
	/// </summary>
	[Test]
	public void Stop_Without_Start_Is_NoOp()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		Action act = sut.Stop;

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.TogglePin" />: pinning the first entry lifts it to the very top.
	/// </summary>
	[Test]
	public void TogglePin_Pins_First_Entry_To_Top()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry c = TextEntry("c", [3]);

		sut.Entries.Add(TextEntry("a", [1]));

		sut.Entries.Add(TextEntry("b", [2]));

		sut.Entries.Add(c);

		// Act
		sut.TogglePin(c);

		// Assert
		sut.Entries[0]
			.Should()
			.Be(c);

		c.IsPinned
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.TogglePin" />: a further pin lands at the end of the pinned block.
	/// </summary>
	[Test]
	public void TogglePin_Pins_Next_Entry_To_End_Of_Pinned_Block()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry pinned = PinnedTextEntry("p", [1]);

		ClipboardTextEntry b = TextEntry("b", [3]);

		sut.Entries.Add(pinned);

		sut.Entries.Add(TextEntry("a", [2]));

		sut.Entries.Add(b);

		// Act (b becomes the second pinned entry).
		sut.TogglePin(b);

		// Assert
		sut.Entries
			.Select(static entry => entry.Hash[0])
			.Should()
			.Equal(1, 3, 2);

		b.IsPinned
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.TogglePin" />: unpinning drops the entry just below the remaining pins.
	/// </summary>
	[Test]
	public void TogglePin_Unpins_To_Top_Of_Unpinned_Block()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>());

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry p0 = PinnedTextEntry("p0", [1]);

		ClipboardTextEntry p1 = PinnedTextEntry("p1", [2]);

		sut.Entries.Add(p0);

		sut.Entries.Add(p1);

		sut.Entries.Add(TextEntry("u", [3]));

		// Act
		sut.TogglePin(p0);

		// Assert
		sut.Entries
			.Select(static entry => entry.Hash[0])
			.Should()
			.Equal(2, 1, 3);

		p0.IsPinned
			.Should()
			.BeFalse();

		p1.IsPinned
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Records the kinds of every <see cref="ClipboardHistoryChangedMessage" /> sent on <paramref name="messenger" />.
	/// </summary>
	private static List<ClipboardHistoryChangeKind> Capture(IMessenger messenger)
	{
		List<ClipboardHistoryChangeKind> received = [];

		messenger.Register<ClipboardHistoryChangedMessage>(
			received,
			static (recipient, message) => ((List<ClipboardHistoryChangeKind>)recipient).Add(message.Kind));

		return received;
	}

	/// <summary>
	/// A minimal pinned text entry with the given hash.
	/// </summary>
	private static ClipboardTextEntry PinnedTextEntry(string text, byte[] hash)
	{
		ClipboardTextEntry entry = TextEntry(text, hash);

		entry.IsPinned = true;

		return entry;
	}

	/// <summary>
	/// A minimal text entry with the given hash.
	/// </summary>
	private static ClipboardTextEntry TextEntry(string text, byte[] hash) => new()
	{
		Text = text,
		Html = null,
		Rtf = null,
		Hash = hash
	};
	#endregion
}
