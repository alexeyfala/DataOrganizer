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
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardHistoryService)}"" type")]
internal class ClipboardHistoryServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardHistoryService.BuildTextEntry" />: plain text becomes a text entry.
	/// </summary>
	[Test]
	public void BuildTextEntry_Builds_Text_Entry_For_Plain_Text()
	{
		// Act
		ClipboardHistoryEntryBase entry = ClipboardHistoryService.BuildTextEntry("just text", "<b>x</b>", null, [1]);

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
	/// <see cref="ClipboardHistoryService.BuildTextEntry" />: whole-string URL text becomes a URL entry (trimmed).
	/// </summary>
	[Test]
	public void BuildTextEntry_Builds_Url_Entry_For_Url_Text()
	{
		// Act
		ClipboardHistoryEntryBase entry = ClipboardHistoryService.BuildTextEntry("  https://example.com/x  ", null, null, [1]);

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
	/// <see cref="ClipboardHistoryService.HandleNewPayload" />: the captured entry becomes the active one.
	/// </summary>
	[Test]
	public void Capture_Marks_Entry_Active_Clearing_Previous()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

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
	/// <see cref="ClipboardHistoryService.HandleNewPayload" />: pinned entries are exempt from the cap.
	/// </summary>
	[Test]
	public void Capture_Trims_Only_Unpinned_Keeping_Pinned()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

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
	/// <see cref="ClipboardHistoryService.ClearAsync" />: the active highlight is cleared from a surviving pin.
	/// </summary>
	[Test]
	public async Task ClearAsync_Clears_Active_On_Surviving_Pinned()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

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
	/// <see cref="ClipboardHistoryService.ClearAsync" />: pinned entries survive and Updated is raised.
	/// </summary>
	[Test]
	public async Task ClearAsync_Preserves_Pinned_And_Raises_Updated()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		(ClipboardHistoryService sut, _) = NewService(messenger);

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
	/// <see cref="ClipboardHistoryService.ClearAsync" />: clears entries and raises ClearedByUser.
	/// </summary>
	[Test]
	public async Task ClearAsync_Raises_ClearedByUser()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = CreateMock(messenger);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

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
	/// <see cref="ClipboardHistoryService.ClearEntriesAsync" />: clears entries and raises ClearedForStop.
	/// </summary>
	[Test]
	public async Task ClearEntriesAsync_Raises_ClearedForStop()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = CreateMock(messenger);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

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
	/// <see cref="ClipboardHistoryService.ComputeTextEntryHash" />: plain and formatted text hash differently.
	/// </summary>
	[Test]
	public void ComputeTextEntryHash_Differs_Between_Plain_And_Formatted()
	{
		// Arrange
		byte[] plain = ClipboardHistoryService.ComputeTextEntryHash("t", null, null);

		byte[] withHtml = ClipboardHistoryService.ComputeTextEntryHash("t", "<b>x</b>", null);

		byte[] withRtf = ClipboardHistoryService.ComputeTextEntryHash("t", null, @"{\rtf1 x}");

		// Act, Assert
		plain
			.Should()
			.NotEqual(withHtml);

		plain
			.Should()
			.NotEqual(withRtf);
	}

	/// <summary>
	/// <see cref="ClipboardHistoryService.ComputeTextEntryHash" />: only the presence of companion
	/// formats matters, not their (delayed-rendered) payloads.
	/// </summary>
	[Test]
	public void ComputeTextEntryHash_Ignores_Companion_Payload_Differences()
	{
		// Act, Assert
		ClipboardHistoryService.ComputeTextEntryHash("t", "<a>one</a>", null)
			.Should()
			.Equal(ClipboardHistoryService.ComputeTextEntryHash("t", "<b>two</b>", null));
	}

	/// <summary>
	/// <see cref="ClipboardHistoryService.ComputeTextEntryHash" />: the same inputs hash identically.
	/// </summary>
	[Test]
	public void ComputeTextEntryHash_Is_Deterministic()
	{
		// Act, Assert
		ClipboardHistoryService.ComputeTextEntryHash("t", "<b>x</b>", null)
			.Should()
			.Equal(ClipboardHistoryService.ComputeTextEntryHash("t", "<b>x</b>", null));
	}

	/// <summary>
	/// <see cref="ClipboardHistoryService.DisposeAsync" />: disposing twice is safe.
	/// </summary>
	[Test]
	public async Task DisposeAsync_Is_Idempotent()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

		await sut.DisposeAsync();

		// Act
		Func<Task> act = async () => await sut.DisposeAsync();

		// Assert
		await act
			.Should()
			.NotThrowAsync();
	}

	/// <summary>
	/// <see cref="ClipboardHistoryService.HandleNewPayload" />: capture enforces the history cap.
	/// </summary>
	[Test]
	public void HandleNewPayload_Enforces_History_Cap()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		(ClipboardHistoryService sut, _) = NewService(messenger);

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
	/// <see cref="ClipboardHistoryService.HandleNewPayload" />: an unchanged payload is ignored.
	/// </summary>
	[Test]
	public void HandleNewPayload_Ignores_Unchanged_Payload()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		(ClipboardHistoryService sut, _) = NewService(messenger);

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
	/// <see cref="ClipboardHistoryService.HandleNewPayload" />: a new payload is inserted at the top.
	/// </summary>
	[Test]
	public void HandleNewPayload_Inserts_New_Entry_At_Top()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		(ClipboardHistoryService sut, _) = NewService(messenger);

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
	/// <see cref="ClipboardHistoryService.HandleNewPayload" />: a matching hash moves the existing entry up.
	/// </summary>
	[Test]
	public void HandleNewPayload_Moves_Existing_Entry_To_Top()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		(ClipboardHistoryService sut, _) = NewService(messenger);

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
	/// <see cref="ClipboardHistoryService.HashFiles" />: same path as a folder vs a file hashes differently.
	/// </summary>
	[Test]
	public void HashFiles_Distinguishes_Folder_From_File()
	{
		// Arrange
		byte[] asFolder = ClipboardHistoryService.HashFiles([new ClipboardFileSystemEntry("C:\\x", IsFolder: true)]);

		byte[] asFile = ClipboardHistoryService.HashFiles([new ClipboardFileSystemEntry("C:\\x", IsFolder: false)]);

		// Act, Assert
		asFolder
			.Should()
			.NotEqual(asFile);
	}

	/// <summary>
	/// <see cref="ClipboardHistoryService.HashFiles" />: the same list hashes identically.
	/// </summary>
	[Test]
	public void HashFiles_Is_Deterministic()
	{
		// Arrange
		ClipboardFileSystemEntry[] list = [new("C:\\a", IsFolder: false), new("C:\\b", IsFolder: true)];

		// Act, Assert
		ClipboardHistoryService.HashFiles(list)
			.Should()
			.Equal(ClipboardHistoryService.HashFiles(list));
	}

	/// <summary>
	/// <see cref="ClipboardHistoryService.HashFiles" />: ordering of the items affects the hash.
	/// </summary>
	[Test]
	public void HashFiles_Is_Order_Sensitive()
	{
		// Arrange
		ClipboardFileSystemEntry a = new("C:\\a", IsFolder: false);

		ClipboardFileSystemEntry b = new("C:\\b", IsFolder: false);

		// Act, Assert
		ClipboardHistoryService.HashFiles([a, b])
			.Should()
			.NotEqual(ClipboardHistoryService.HashFiles([b, a]));
	}

	/// <summary>
	/// <see cref="ClipboardHistoryService.StartAsync" /> / <see cref="ClipboardHistoryService.DisposeAsync" />:
	/// the running flag toggles around the loop's lifetime.
	/// </summary>
	[Test]
	public async Task IsRunning_Toggles_With_Start_And_Dispose()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

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
	/// <see cref="ClipboardHistoryService.Merge" />: appends below current, dedupes by hash, no message.
	/// </summary>
	[Test]
	public void Merge_Appends_Below_Current_Skipping_Duplicates()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = CreateMock(messenger);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

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
	/// <see cref="ClipboardHistoryService.Merge" />: the history cap is enforced.
	/// </summary>
	[Test]
	public void Merge_Enforces_History_Cap()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = CreateMock(messenger);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

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
	/// <see cref="ClipboardHistoryService.Merge" />: pinned entries are placed atop, keeping the invariant.
	/// </summary>
	[Test]
	public void Merge_Places_Pinned_Atop_Preserving_Invariant()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

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
	/// <see cref="ClipboardHistoryService.PollOnceAsync" />: files are captured with folders sorted first.
	/// </summary>
	[Test]
	public async Task PollOnce_Captures_Files_Entry_Folders_First()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		(ClipboardHistoryService sut, IClipboardAccessor clipboard) = NewService(messenger);

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
	/// <see cref="ClipboardHistoryService.PollOnceAsync" />: plain text is captured as a text entry.
	/// </summary>
	[Test]
	public async Task PollOnce_Captures_Text_Entry()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		(ClipboardHistoryService sut, IClipboardAccessor clipboard) = NewService(messenger);

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
	/// <see cref="ClipboardHistoryService.PollOnceAsync" />: files without an absolute path are skipped.
	/// </summary>
	[Test]
	public async Task PollOnce_Skips_Files_Without_Absolute_Path()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		(ClipboardHistoryService sut, IClipboardAccessor clipboard) = NewService(messenger);

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
	/// <see cref="ClipboardHistoryService.PollOnceAsync" />: a sensitivity marker skips the entry.
	/// </summary>
	[Test]
	public async Task PollOnce_Skips_Sensitive_Content()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		(ClipboardHistoryService sut, IClipboardAccessor clipboard) = NewService(messenger);

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
	/// Re-baseline path: a restored pinned entry keeps its pinned state on the replacement.
	/// </summary>
	[Test]
	public async Task Rebaseline_Carries_Pin_State()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

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
	/// Test of the re-baseline path when the restored entry is no longer present: it is inserted at the top.
	/// </summary>
	[Test]
	public async Task Restore_Of_Missing_Entry_Then_Differing_Capture_Inserts_Rebaselined()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		(ClipboardHistoryService sut, _) = NewService(messenger);

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
	/// <see cref="ClipboardHistoryService.RestoreAsync" />: restoring the top entry raises no notification.
	/// </summary>
	[Test]
	public async Task Restore_Of_Top_Entry_Raises_No_Notification()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		(ClipboardHistoryService sut, _) = NewService(messenger);

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

		(ClipboardHistoryService sut, _) = NewService(messenger);

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

		(ClipboardHistoryService sut, _) = NewService(messenger);

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
	/// <see cref="ClipboardHistoryService.RestoreAsync" />: the restored entry becomes the active one.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Marks_Entry_Active()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

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
	/// <see cref="ClipboardHistoryService.RestoreAsync" />: moves the entry to the top and raises Updated.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Moves_To_Top_And_Raises_Updated()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = CreateMock(messenger);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

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
	/// <see cref="ClipboardHistoryService.StartAsync" />: a disposed service does not start.
	/// </summary>
	[Test]
	public async Task StartAsync_After_Dispose_Does_Not_Run()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

		await sut.DisposeAsync();

		// Act
		await sut.StartAsync();

		// Assert
		sut.IsRunning
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardHistoryService.Stop" />: stopping without a running loop is a no-op.
	/// </summary>
	[Test]
	public void Stop_Without_Start_Is_NoOp()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

		// Act
		Action act = sut.Stop;

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="ClipboardHistoryService.TogglePin" />: pinning the first entry lifts it to the very top.
	/// </summary>
	[Test]
	public void TogglePin_Pins_First_Entry_To_Top()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

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
	/// <see cref="ClipboardHistoryService.TogglePin" />: a further pin lands at the end of the pinned block.
	/// </summary>
	[Test]
	public void TogglePin_Pins_Next_Entry_To_End_Of_Pinned_Block()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

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
	/// <see cref="ClipboardHistoryService.TogglePin" />: unpinning drops the entry just below the remaining pins.
	/// </summary>
	[Test]
	public void TogglePin_Unpins_To_Top_Of_Unpinned_Block()
	{
		// Arrange
		(ClipboardHistoryService sut, _) = NewService(new WeakReferenceMessenger());

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
	/// Builds an auto-mock container with a synchronous dispatcher and the supplied messenger.
	/// </summary>
	private static AutoMock CreateMock(IMessenger messenger)
	{
		return AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(Substitute.For<IClipboardAccessor>());

			builder.RegisterInstance(Substitute.For<IStorageAccessor>());

			builder.RegisterInstance(messenger);
		});
	}

	/// <summary>
	/// Builds a service via its constructor with a synchronous dispatcher and substituted collaborators,
	/// returning the clipboard accessor for per-test setup.
	/// </summary>
	private static (ClipboardHistoryService Sut, IClipboardAccessor Clipboard) NewService(IMessenger messenger)
	{
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		clipboard
			.GetDataFormatsAsync()
			.Returns([]);

		ClipboardHistoryService sut = new(
			clipboard,
			new InlineDispatcherAccessor(),
			Substitute.For<ILogger>(),
			messenger,
			Substitute.For<IStorageAccessor>());

		return (sut, clipboard);
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
