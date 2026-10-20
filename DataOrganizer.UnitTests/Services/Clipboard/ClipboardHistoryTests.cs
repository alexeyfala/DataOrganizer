using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Messages.Clipboard;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.UnitTests.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardHistory)}"" type")]
internal class ClipboardHistoryTests
{
	#region Data
	/// <summary>
	/// Path of the first item of a list of files.
	/// </summary>
	private static readonly string FirstItemPath = TestPath.Combine("a");

	/// <summary>
	/// Path of the second item of a list of files.
	/// </summary>
	private static readonly string SecondItemPath = TestPath.Combine("b");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ClipboardHistory.Clear" />: the payload still on the clipboard is captured again after a clear.
	/// </summary>
	[Test]
	public void Clear_Lets_The_Same_Payload_Be_Captured_Again()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry capturedAgain = ClipboardEntryFactory.CreateTextEntry("a", [1]);

		sut.HandleNewPayload([1], () => ClipboardEntryFactory.CreateTextEntry("a", [1]), isSensitive: false);

		// Act (the same content is still on the clipboard when the logging resumes).
		sut.Clear(preservePinned: false);

		sut.HandleNewPayload([1], () => capturedAgain, isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(capturedAgain);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.ComputeTextEntryHash" />: plain and formatted text hash differently.
	/// </summary>
	[Test]
	public void ComputeTextEntryHash_Differs_Between_Plain_And_Formatted()
	{
		// Arrange
		byte[] plain = ClipboardHistory.ComputeTextEntryHash("t", null, null);

		byte[] withHtml = ClipboardHistory.ComputeTextEntryHash("t", "<b>x</b>", null);

		byte[] withRtf = ClipboardHistory.ComputeTextEntryHash("t", null, @"{\rtf1 x}");

		// Act, Assert
		plain
			.Should()
			.NotEqual(withHtml);

		plain
			.Should()
			.NotEqual(withRtf);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.ComputeTextEntryHash" />: only the presence of companion
	/// formats matters, not their (delayed-rendered) payloads.
	/// </summary>
	[Test]
	public void ComputeTextEntryHash_Ignores_Companion_Payload_Differences()
	{
		// Act, Assert
		ClipboardHistory.ComputeTextEntryHash("t", "<a>one</a>", null)
			.Should()
			.Equal(ClipboardHistory.ComputeTextEntryHash("t", "<b>two</b>", null));
	}

	/// <summary>
	/// <see cref="ClipboardHistory.ComputeTextEntryHash" />: the same inputs hash identically.
	/// </summary>
	[Test]
	public void ComputeTextEntryHash_Is_Deterministic()
	{
		// Act, Assert
		ClipboardHistory.ComputeTextEntryHash("t", "<b>x</b>", null)
			.Should()
			.Equal(ClipboardHistory.ComputeTextEntryHash("t", "<b>x</b>", null));
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleClipboardCleared" />: a restored entry keeps its highlight while the clipboard has not
	/// handed it back yet.
	/// </summary>
	[Test]
	public void HandleClipboardCleared_Keeps_A_Restored_Entry_Active()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry restored = ClipboardEntryFactory.CreateTextEntry("restored", [1]);

		sut.Entries.Add(restored);

		sut.MarkRestored(restored, keepPosition: false);

		// Act (the clipboard reads empty before it hands the restored content back).
		sut.HandleClipboardCleared();

		// Assert
		restored.IsActive
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleNewPayload" />: a restored pinned entry keeps its pinned state on the replacement.
	/// </summary>
	[Test]
	public void HandleNewPayload_After_Restore_Carries_The_Pin_State()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry original = ClipboardEntryFactory.CreatePinnedTextEntry("orig", [1]);

		sut.Entries.Add(original);

		sut.MarkRestored(original, keepPosition: false);

		ClipboardTextEntry rebaselined = ClipboardEntryFactory.CreateTextEntry("rebased", [2]);

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
	/// <see cref="ClipboardHistory.HandleNewPayload" />: a restored entry that is no longer present is inserted at the top.
	/// </summary>
	[Test]
	public void HandleNewPayload_After_Restore_Inserts_A_Missing_Rebaselined_Entry()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry original = ClipboardEntryFactory.CreateTextEntry("orig", [1]);

		sut.Entries.Add(original);

		sut.MarkRestored(original, keepPosition: false);

		// The restored entry is gone by the time the next capture arrives.
		sut.Entries.Clear();

		List<ClipboardLogChangeKind> received = Capture(messenger);

		ClipboardTextEntry rebaselined = ClipboardEntryFactory.CreateTextEntry("rebased", [2]);

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
			.Contain(ClipboardLogChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleNewPayload" />: a restored entry observed with the same hash is left untouched.
	/// </summary>
	[Test]
	public void HandleNewPayload_After_Restore_Keeps_An_Unchanged_Entry()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry original = ClipboardEntryFactory.CreateTextEntry("orig", [1]);

		sut.Entries.Add(original);

		sut.MarkRestored(original, keepPosition: false);

		List<ClipboardLogChangeKind> received = Capture(messenger);

		bool built = false;

		// Act (same hash as the restored entry — nothing to re-baseline).
		sut.HandleNewPayload([1], () =>
		{
			built = true;

			return ClipboardEntryFactory.CreateTextEntry("x", [1]);
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
	/// <see cref="ClipboardHistory.HandleNewPayload" />: the replacement of a restored entry stays the active one.
	/// </summary>
	[Test]
	public void HandleNewPayload_After_Restore_Keeps_The_Replacement_Active()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry original = ClipboardEntryFactory.CreateTextEntry("orig", [1]);

		sut.Entries.Add(original);

		sut.MarkRestored(original, keepPosition: false);

		ClipboardTextEntry rebaselined = ClipboardEntryFactory.CreateTextEntry("rebased", [2]);

		// Act (the clipboard handed back a different representation -> different hash).
		sut.HandleNewPayload([2], () => rebaselined, isSensitive: false);

		// Assert
		rebaselined.IsActive
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleNewPayload" />: a restored entry whose hash changed on the next capture is replaced.
	/// </summary>
	[Test]
	public void HandleNewPayload_After_Restore_Rebaselines_A_Differing_Entry()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry original = ClipboardEntryFactory.CreateTextEntry("orig", [1]);

		sut.Entries.Add(original);

		sut.MarkRestored(original, keepPosition: false);

		List<ClipboardLogChangeKind> received = Capture(messenger);

		ClipboardTextEntry rebaselined = ClipboardEntryFactory.CreateTextEntry("rebased", [2]);

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
			.Contain(ClipboardLogChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleNewPayload" />: capture enforces the history cap.
	/// </summary>
	[Test]
	public void HandleNewPayload_Enforces_History_Cap()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WeakReferenceMessenger>()
			.As<IMessenger>());

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		for (int i = 0; i < 100; i++)
		{
			sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry($"e{i}", [(byte)i]));
		}

		// Act
		sut.HandleNewPayload([201], () => ClipboardEntryFactory.CreateTextEntry("new", [201]), isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.HaveCount(100);

		sut.Entries[0].Hash
			.Should()
			.Equal([201]);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleNewPayload" />: an unchanged payload is ignored.
	/// </summary>
	[Test]
	public void HandleNewPayload_Ignores_Unchanged_Payload()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		sut.HandleNewPayload([1], () => ClipboardEntryFactory.CreateTextEntry("a", [1]), isSensitive: false);

		List<ClipboardLogChangeKind> received = Capture(messenger);

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
	/// <see cref="ClipboardHistory.HandleNewPayload" />: a new payload is inserted at the top.
	/// </summary>
	[Test]
	public void HandleNewPayload_Inserts_New_Entry_At_Top()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		List<ClipboardLogChangeKind> received = Capture(messenger);

		ClipboardTextEntry entry = ClipboardEntryFactory.CreateTextEntry("a", [1]);

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
			.Equal(ClipboardLogChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleNewPayload" />: the payload seen again leaves its entry in place, even one pinned
	/// below other pins.
	/// </summary>
	[Test]
	public void HandleNewPayload_Leaves_An_Unchanged_Entry_In_Place()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry pinned = ClipboardEntryFactory.CreatePinnedTextEntry("p", [9]);

		ClipboardTextEntry entry = ClipboardEntryFactory.CreateTextEntry("a", [1]);

		sut.Entries.Add(pinned);

		sut.HandleNewPayload([1], () => entry, isSensitive: false);

		// Pinning the captured entry puts it at the end of the pinned block, below the older pin.
		sut.TogglePin(entry);

		// Act (the clipboard still holds the same payload).
		sut.HandleNewPayload([1], static () => throw new InvalidOperationException("Factory should not be invoked."), isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.Equal(pinned, entry);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleNewPayload" />: the captured entry becomes the active one.
	/// </summary>
	[Test]
	public void HandleNewPayload_Marks_Entry_Active_Clearing_Previous()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry first = ClipboardEntryFactory.CreateTextEntry("a", [1]);

		ClipboardTextEntry second = ClipboardEntryFactory.CreateTextEntry("b", [2]);

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
	/// <see cref="ClipboardHistory.HandleNewPayload" />: a matching hash moves an unpinned entry up to just below the pins.
	/// </summary>
	[Test]
	public void HandleNewPayload_Moves_An_Existing_Entry_Below_The_Pins()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry pinned = ClipboardEntryFactory.CreatePinnedTextEntry("pinned", [9]);

		ClipboardTextEntry top = ClipboardEntryFactory.CreateTextEntry("top", [8]);

		ClipboardTextEntry target = ClipboardEntryFactory.CreateTextEntry("target", [1]);

		sut.Entries.Add(pinned);

		sut.Entries.Add(top);

		sut.Entries.Add(target);

		// Act (the factory must not run — the entry already exists).
		sut.HandleNewPayload([1], static () => throw new InvalidOperationException("Factory should not be invoked."), isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.Equal(pinned, target, top);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleNewPayload" />: a matching hash moves a pinned entry to the very top.
	/// </summary>
	[Test]
	public void HandleNewPayload_Moves_An_Existing_Pinned_Entry_To_The_Very_Top()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry first = ClipboardEntryFactory.CreatePinnedTextEntry("first", [9]);

		ClipboardTextEntry target = ClipboardEntryFactory.CreatePinnedTextEntry("target", [1]);

		ClipboardTextEntry unpinned = ClipboardEntryFactory.CreateTextEntry("unpinned", [8]);

		sut.Entries.Add(first);

		sut.Entries.Add(target);

		sut.Entries.Add(unpinned);

		// Act (the factory must not run — the entry already exists).
		sut.HandleNewPayload([1], static () => throw new InvalidOperationException("Factory should not be invoked."), isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.Equal(target, first, unpinned);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleNewPayload" />: a matching hash moves the existing entry up.
	/// </summary>
	[Test]
	public void HandleNewPayload_Moves_Existing_Entry_To_Top()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry target = ClipboardEntryFactory.CreateTextEntry("target", [1]);

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("top", [9]));

		sut.Entries.Add(target);

		List<ClipboardLogChangeKind> received = Capture(messenger);

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
			.Contain(ClipboardLogChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleNewPayload" />: a new entry raises a change of the entry count.
	/// </summary>
	[Test]
	public void HandleNewPayload_Raises_A_Count_Change_For_A_New_Entry()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		List<ClipboardLogEntryCountChangedMessage> received = CaptureCountChanges(messenger);

		// Act
		sut.HandleNewPayload([1], () => ClipboardEntryFactory.CreateTextEntry("a", [1]), isSensitive: false);

		// Assert
		received
			.Should()
			.ContainSingle();
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HandleNewPayload" />: pinned entries are exempt from the cap.
	/// </summary>
	[Test]
	public void HandleNewPayload_Trims_Only_Unpinned_Keeping_Pinned()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry pinned = ClipboardEntryFactory.CreatePinnedTextEntry("pinned", [200]);

		sut.Entries.Add(pinned);

		for (int i = 0; i < 100; i++)
		{
			sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry($"e{i}", [(byte)i]));
		}

		// Act (cap applies to the 100 unpinned only, so the pinned entry survives).
		sut.HandleNewPayload([201], () => ClipboardEntryFactory.CreateTextEntry("new", [201]), isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.HaveCount(101);

		sut.Entries
			.Should()
			.Contain(pinned);

		sut.Entries[0]
			.Should()
			.Be(pinned);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HashFiles" />: same path as a folder vs a file hashes differently.
	/// </summary>
	[Test]
	public void HashFiles_Distinguishes_Folder_From_File()
	{
		// Arrange
		string path = TestPath.Combine("x");

		byte[] asFolder = ClipboardHistory.HashFiles([new ClipboardFileSystemEntry(path, IsFolder: true)]);

		byte[] asFile = ClipboardHistory.HashFiles([new ClipboardFileSystemEntry(path, IsFolder: false)]);

		// Act, Assert
		asFolder
			.Should()
			.NotEqual(asFile);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HashFiles" />: the same list hashes identically.
	/// </summary>
	[Test]
	public void HashFiles_Is_Deterministic()
	{
		// Arrange
		ClipboardFileSystemEntry[] entries =
		[
			new(FirstItemPath, IsFolder: false),
			new(SecondItemPath, IsFolder: true)
		];

		// Act, Assert
		ClipboardHistory.HashFiles(entries)
			.Should()
			.Equal(ClipboardHistory.HashFiles(entries));
	}

	/// <summary>
	/// <see cref="ClipboardHistory.HashFiles" />: ordering of the items affects the hash.
	/// </summary>
	[Test]
	public void HashFiles_Is_Order_Sensitive()
	{
		// Arrange
		ClipboardFileSystemEntry a = new(FirstItemPath, IsFolder: false);

		ClipboardFileSystemEntry b = new(SecondItemPath, IsFolder: false);

		// Act, Assert
		ClipboardHistory.HashFiles([a, b])
			.Should()
			.NotEqual(ClipboardHistory.HashFiles([b, a]));
	}

	/// <summary>
	/// <see cref="ClipboardHistory.Merge" />: appends below current, dedupes by hash, no message.
	/// </summary>
	[Test]
	public void Merge_Appends_Below_Current_Skipping_Duplicates()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("C", [2]));

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("D", [9]));

		List<ClipboardLogChangeKind> received = Capture(messenger);

		// Act ("B" duplicates current "C" by hash [2] and is skipped).
		sut.Merge([ClipboardEntryFactory.CreateTextEntry("A", [1]), ClipboardEntryFactory.CreateTextEntry("B", [2])]);

		// Assert
		sut.Entries.Cast<ClipboardTextEntry>().Select(x => x.Text)
			.Should()
			.Equal("C", "D", "A");

		received
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardHistory.Merge" />: the history cap is enforced.
	/// </summary>
	[Test]
	public void Merge_Enforces_History_Cap()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WeakReferenceMessenger>()
			.As<IMessenger>());

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		for (int i = 0; i < 100; i++)
		{
			sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry($"cur{i}", [(byte)i]));
		}

		// Act
		sut.Merge([ClipboardEntryFactory.CreateTextEntry("overflow", [200])]);

		// Assert
		sut.Entries
			.Should()
			.HaveCount(100);

		sut.Entries.Cast<ClipboardTextEntry>()
			.Should()
			.NotContain(x => x.Text == "overflow");
	}

	/// <summary>
	/// <see cref="ClipboardHistory.Merge" />: a current entry saved as pinned is pinned, at the end of the pinned block.
	/// </summary>
	[Test]
	public void Merge_Pins_A_Current_Entry_Saved_As_Pinned()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry pinned = ClipboardEntryFactory.CreatePinnedTextEntry("p", [9]);

		ClipboardTextEntry unpinned = ClipboardEntryFactory.CreateTextEntry("u", [2]);

		ClipboardTextEntry current = ClipboardEntryFactory.CreateTextEntry("a", [1]);

		sut.Entries.Add(pinned);

		sut.Entries.Add(unpinned);

		sut.Entries.Add(current);

		// Act (the saved copy of "a" is pinned).
		sut.Merge([ClipboardEntryFactory.CreatePinnedTextEntry("a", [1])]);

		// Assert
		sut.Entries
			.Should()
			.Equal(pinned, current, unpinned);

		current.IsPinned
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardHistory.Merge" />: pinned entries are placed atop, keeping the invariant.
	/// </summary>
	[Test]
	public void Merge_Places_Pinned_Atop_Preserving_Invariant()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("newText", [9]));

		// Act
		sut.Merge(
		[
			ClipboardEntryFactory.CreatePinnedTextEntry("p0", [1]),
			ClipboardEntryFactory.CreatePinnedTextEntry("p1", [2]),
			ClipboardEntryFactory.CreateTextEntry("u", [3])
		]);

		// Assert
		sut.Entries.Select(static entry => entry.Hash[0])
			.Should()
			.Equal(1, 2, 9, 3);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.Remove" />: an entry removed before the clipboard handed it back is no longer awaited, so a
	/// secret copied next is still skipped.
	/// </summary>
	[Test]
	public void Remove_Of_A_Restored_Entry_Still_Skips_The_Next_Secret()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry restored = ClipboardEntryFactory.CreateTextEntry("restored", [1]);

		sut.Entries.Add(restored);

		sut.MarkRestored(restored, keepPosition: false);

		// Act (a secret is copied next).
		sut.Remove(restored);

		sut.HandleNewPayload([2], () => ClipboardEntryFactory.CreateTextEntry("secret", [2]), isSensitive: true);

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardHistory.Remove" />: after the active entry is removed, the same payload copied again is captured
	/// again.
	/// </summary>
	[Test]
	public void Remove_Of_The_Active_Entry_Lets_Its_Payload_Be_Captured_Again()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry entry = ClipboardEntryFactory.CreateTextEntry("a", [1]);

		ClipboardTextEntry copiedAgain = ClipboardEntryFactory.CreateTextEntry("a", [1]);

		sut.HandleNewPayload([1], () => entry, isSensitive: false);

		// Act (the same content is copied again before a poll sees the emptied clipboard).
		sut.Remove(entry);

		sut.HandleNewPayload([1], () => copiedAgain, isSensitive: false);

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(copiedAgain);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.TogglePin" />: pinning the first entry lifts it to the very top.
	/// </summary>
	[Test]
	public void TogglePin_Pins_First_Entry_To_Top()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry c = ClipboardEntryFactory.CreateTextEntry("c", [3]);

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("a", [1]));

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("b", [2]));

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
	/// <see cref="ClipboardHistory.TogglePin" />: a further pin lands at the end of the pinned block.
	/// </summary>
	[Test]
	public void TogglePin_Pins_Next_Entry_To_End_Of_Pinned_Block()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry pinned = ClipboardEntryFactory.CreatePinnedTextEntry("p", [1]);

		ClipboardTextEntry b = ClipboardEntryFactory.CreateTextEntry("b", [3]);

		sut.Entries.Add(pinned);

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("a", [2]));

		sut.Entries.Add(b);

		// Act (b becomes the second pinned entry).
		sut.TogglePin(b);

		// Assert
		sut.Entries.Select(static entry => entry.Hash[0])
			.Should()
			.Equal(1, 3, 2);

		b.IsPinned
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardHistory.TogglePin" />: a change of the pin raises Updated.
	/// </summary>
	[Test]
	public void TogglePin_Raises_Updated()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry entry = ClipboardEntryFactory.CreateTextEntry("a", [1]);

		sut.Entries.Add(entry);

		List<ClipboardLogChangeKind> received = Capture(messenger);

		// Act
		sut.TogglePin(entry);

		// Assert
		received
			.Should()
			.Equal(ClipboardLogChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardHistory.TogglePin" />: unpinning drops the entry just below the remaining pins.
	/// </summary>
	[Test]
	public void TogglePin_Unpins_To_Top_Of_Unpinned_Block()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardHistory sut = mock.Create<ClipboardHistory>();

		ClipboardTextEntry p0 = ClipboardEntryFactory.CreatePinnedTextEntry("p0", [1]);

		ClipboardTextEntry p1 = ClipboardEntryFactory.CreatePinnedTextEntry("p1", [2]);

		sut.Entries.Add(p0);

		sut.Entries.Add(p1);

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("u", [3]));

		// Act
		sut.TogglePin(p0);

		// Assert
		sut.Entries.Select(static entry => entry.Hash[0])
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
	/// Records the kinds of every <see cref="ClipboardLogChangedMessage" /> sent on <paramref name="messenger" />.
	/// </summary>
	private static List<ClipboardLogChangeKind> Capture(IMessenger messenger)
	{
		List<ClipboardLogChangeKind> received = [];

		messenger.Register<ClipboardLogChangedMessage>(
			received,
			static (recipient, message) => ((List<ClipboardLogChangeKind>)recipient).Add(message.Kind));

		return received;
	}

	/// <summary>
	/// Records every <see cref="ClipboardLogEntryCountChangedMessage" /> sent on <paramref name="messenger" />.
	/// </summary>
	private static List<ClipboardLogEntryCountChangedMessage> CaptureCountChanges(IMessenger messenger)
	{
		List<ClipboardLogEntryCountChangedMessage> received = [];

		messenger.Register<ClipboardLogEntryCountChangedMessage>(
			received,
			static (recipient, message) => ((List<ClipboardLogEntryCountChangedMessage>)recipient).Add(message));

		return received;
	}
	#endregion
}
