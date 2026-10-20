using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Clipboard;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardLogService)}"" type")]
internal class ClipboardLogServiceTests
{
	#region Data
	/// <summary>
	/// Name of the folder that holds the items of the clipboard.
	/// </summary>
	private const string FolderName = "dir";

	/// <summary>
	/// Path of the file on the clipboard.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine(FolderName, "a.txt");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ClipboardLogService.ClearAllAsync" />: the active highlight is cleared from a surviving pin.
	/// </summary>
	[Test]
	public async Task ClearAllAsync_Clears_Active_On_Surviving_Pinned()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry pinned = ClipboardEntryFactory.CreatePinnedTextEntry("p", [1]);

		sut.Entries.Add(pinned);

		// Restoring marks it active.
		await sut.RestoreAsync(pinned);

		// Act
		await sut.ClearAllAsync();

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
	/// <see cref="ClipboardLogService.ClearAllAsync" />: the system clipboard is emptied too, so the cleared content is not
	/// captured again.
	/// </summary>
	[Test]
	public async Task ClearAllAsync_Empties_The_System_Clipboard()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.ClearAllAsync();

		// Assert
		await clipboard
			.Received(1)
			.ClearAsync();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.ClearAllAsync" />: pinned entries survive and Updated is raised.
	/// </summary>
	[Test]
	public async Task ClearAllAsync_Preserves_Pinned_And_Raises_Updated()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry pinned = ClipboardEntryFactory.CreatePinnedTextEntry("p", [1]);

		sut.Entries.Add(pinned);

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("u", [2]));

		List<ClipboardLogChangeKind> received = Capture(messenger);

		// Act
		await sut.ClearAllAsync();

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(pinned);

		received
			.Should()
			.Equal(ClipboardLogChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.ClearAllAsync" />: clears entries and raises ClearedByUser.
	/// </summary>
	[Test]
	public async Task ClearAllAsync_Raises_ClearedByUser()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("a", [1]));

		List<ClipboardLogChangeKind> received = Capture(messenger);

		// Act
		await sut.ClearAllAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();

		received
			.Should()
			.Equal(ClipboardLogChangeKind.ClearedByUser);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.ClearEntriesAsync" />: the system clipboard keeps its content.
	/// </summary>
	[Test]
	public async Task ClearEntriesAsync_Leaves_The_System_Clipboard()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.ClearEntriesAsync();

		// Assert
		await clipboard
			.DidNotReceive()
			.ClearAsync();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.ClearEntriesAsync" />: clears entries and raises ClearedOnStop.
	/// </summary>
	[Test]
	public async Task ClearEntriesAsync_Raises_ClearedOnStop()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("a", [1]));

		List<ClipboardLogChangeKind> received = Capture(messenger);

		// Act
		await sut.ClearEntriesAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();

		received
			.Should()
			.Equal(ClipboardLogChangeKind.ClearedOnStop);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.DisposeAsync" />: disposing twice is safe.
	/// </summary>
	[Test]
	public async Task DisposeAsync_Is_Idempotent()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<InlineDispatcherAccessor>()
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
	/// <see cref="ClipboardLogService.StartAsync" /> / <see cref="ClipboardLogService.DisposeAsync" />:
	/// the running flag toggles around the loop's lifetime.
	/// </summary>
	[Test]
	public async Task IsRunning_Toggles_With_Start_And_Dispose()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<InlineDispatcherAccessor>()
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
	/// <see cref="ClipboardLogService.Merge" />: the entries go to the history.
	/// </summary>
	[Test]
	public void Merge_Hands_The_Entries_To_The_History()
	{
		// Arrange
		ClipboardLogEntryBase[] entries = [ClipboardEntryFactory.CreateTextEntry("a", [1])];

		IClipboardHistory history = Substitute.For<IClipboardHistory>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(history));

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		sut.Merge(entries);

		// Assert
		history
			.Received(1)
			.Merge(entries);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: a text that is a whole link is captured as a link entry.
	/// </summary>
	[Test]
	public async Task PollOnceAsync_Captures_A_Link_As_A_Link_Entry()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.TryReadTextAsync()
				.Returns(" https://example.com/page ");

			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.PollOnceAsync();

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<ClipboardUrlEntry>()
			.Which
			.Url
			.Should()
			.Be("https://example.com/page");
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: a text that looks like a secret is captured as a sensitive entry.
	/// </summary>
	[Test]
	public async Task PollOnceAsync_Captures_A_Secret_As_A_Sensitive_Entry()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.TryReadTextAsync()
				.Returns("Xk7#pQ2!mZ");

			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.PollOnceAsync();

		// Assert
		sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.IsSensitive
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: an image is captured with its PNG and the hash of the PNG.
	/// </summary>
	[Test]
	public async Task PollOnceAsync_Captures_An_Image()
	{
		// Arrange
		byte[] png = ImageFactory.CreatePng();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.TryReadImagePngAsync()
				.Returns(png);

			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.PollOnceAsync();

		// Assert
		ClipboardImageEntry entry = sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<ClipboardImageEntry>()
			.Subject;

		entry.OriginalPng
			.Should()
			.Equal(png);

		entry.Hash
			.Should()
			.Equal(SHA256.HashData(png));
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: plain text is captured as a text entry.
	/// </summary>
	[Test]
	public async Task PollOnceAsync_Captures_Text_Entry()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.TryReadTextAsync()
				.Returns("hello");

			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

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
	/// <see cref="ClipboardLogService.PollOnceAsync" />: files and folders are captured in the order the clipboard gives
	/// them, with the hash of the list.
	/// </summary>
	[Test]
	public async Task PollOnceAsync_Captures_The_Files()
	{
		// Arrange
		ClipboardFileSystemEntry[] entries =
		[
			new(TestPath.Combine(FolderName, "sub"), IsFolder: true),
			new(FilePath, IsFolder: false)
		];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.TryReadFilesAsync()
				.Returns(entries);

			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

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
			.Equal(entries);

		entry.Hash
			.Should()
			.Equal(ClipboardHistory.HashFiles(entries));
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: the HTML and the RTF of a text go into its entry and its hash.
	/// </summary>
	[Test]
	public async Task PollOnceAsync_Captures_The_Html_And_Rtf_Of_A_Text()
	{
		// Arrange
		const string html = "<b>hello</b>";

		const string rtf = @"{\rtf1 hello}";

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.TryReadTextAsync()
				.Returns("hello");

			clipboard
				.TryReadHtmlAsync()
				.Returns(html);

			clipboard
				.TryReadRtfAsync()
				.Returns(rtf);

			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.PollOnceAsync();

		// Assert
		ClipboardTextEntry entry = sut.Entries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<ClipboardTextEntry>()
			.Subject;

		entry.Html
			.Should()
			.Be(html);

		entry.Rtf
			.Should()
			.Be(rtf);

		entry.Hash
			.Should()
			.Equal(ClipboardHistory.ComputeTextEntryHash("hello", html, rtf));
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: an emptied clipboard drops the active highlight.
	/// </summary>
	[Test]
	public async Task PollOnceAsync_Clears_Active_When_Clipboard_Emptied()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry entry = ClipboardEntryFactory.CreateTextEntry("a", [1]);

		// The entry was on the clipboard; the clipboard now holds nothing capturable.
		sut.Entries.Add(entry);

		entry.IsActive = true;

		// Act
		await sut.PollOnceAsync();

		// Assert
		entry.IsActive
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: an image marked as a secret is skipped.
	/// </summary>
	[Test]
	public async Task PollOnceAsync_Skips_A_Sensitive_Image()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.ContainsSensitivityMarkerAsync()
				.Returns(true);

			clipboard
				.TryReadImagePngAsync()
				.Returns(ImageFactory.CreatePng());

			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.PollOnceAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: content marked as a secret is skipped.
	/// </summary>
	[Test]
	public async Task PollOnceAsync_Skips_Sensitive_Content()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.ContainsSensitivityMarkerAsync()
				.Returns(true);

			clipboard
				.TryReadTextAsync()
				.Returns("super-secret");

			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.PollOnceAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.PollOnceAsync" />: files marked as a secret are skipped.
	/// </summary>
	[Test]
	public async Task PollOnceAsync_Skips_Sensitive_Files()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.ContainsSensitivityMarkerAsync()
				.Returns(true);

			clipboard
				.TryReadFilesAsync()
				.Returns([new ClipboardFileSystemEntry(FilePath, IsFolder: false)]);

			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.PollOnceAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RemoveAsync" />: a missing entry is a no-op and raises no notification.
	/// </summary>
	[Test]
	public async Task RemoveAsync_Missing_Entry_Is_NoOp()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("a", [1]));

		List<ClipboardLogChangeKind> received = Capture(messenger);

		// Act (the entry was never added).
		await sut.RemoveAsync(ClipboardEntryFactory.CreateTextEntry("absent", [9]));

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
	public async Task RemoveAsync_Of_Active_Entry_Empties_System_Clipboard()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry entry = ClipboardEntryFactory.CreateTextEntry("a", [1]);

		// The active entry's content is the one held in the system clipboard.
		sut.Entries.Add(entry);

		entry.IsActive = true;

		// Act
		await sut.RemoveAsync(entry);

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();

		await clipboard
			.Received(1)
			.ClearAsync();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RemoveAsync" />: after removing the active entry the emptied clipboard
	/// is not re-captured by the next poll tick.
	/// </summary>
	[Test]
	public async Task RemoveAsync_Of_Active_Entry_Is_Not_Recaptured_By_Next_Poll()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry entry = ClipboardEntryFactory.CreateTextEntry("a", [1]);

		sut.Entries.Add(entry);

		entry.IsActive = true;

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
	public async Task RemoveAsync_Of_Inactive_Entry_Leaves_System_Clipboard()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry active = ClipboardEntryFactory.CreateTextEntry("active", [1]);

		ClipboardTextEntry old = ClipboardEntryFactory.CreateTextEntry("old", [2]);

		// The newest entry is the active one; "old" sits below it and is not on the system clipboard.
		sut.Entries.Add(active);

		sut.Entries.Add(old);

		active.IsActive = true;

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
	public async Task RemoveAsync_Removes_Pinned_Entry_And_Raises_Updated()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry pinned = ClipboardEntryFactory.CreatePinnedTextEntry("p", [1]);

		sut.Entries.Add(pinned);

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("u", [2]));

		List<ClipboardLogChangeKind> received = Capture(messenger);

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
			.Contain(ClipboardLogChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: restoring a sensitive entry arms the auto-clear.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Arms_AutoClear_For_Sensitive_Entry()
	{
		// Arrange
		const string secret = "Xk7#pQ2!mZ";

		IClipboardAutoClear autoClear = Substitute.For<IClipboardAutoClear>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.SetTextAsync(secret, html: null, rtf: null, isSensitive: true)
				.Returns(true);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(autoClear);

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.RestoreAsync(ClipboardEntryFactory.CreateTextEntry(secret, isSensitive: true));

		// Assert
		autoClear
			.Received(1)
			.Arm();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: restoring a non-sensitive entry does not arm the auto-clear.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Does_Not_Arm_AutoClear_For_Non_Sensitive_Entry()
	{
		// Arrange
		const string text = "just some text";

		IClipboardAutoClear autoClear = Substitute.For<IClipboardAutoClear>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			// The text reaches the clipboard, so only its kind keeps the auto-clear off.
			clipboard
				.SetTextAsync(text, html: null, rtf: null, isSensitive: false)
				.Returns(true);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(autoClear);

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.RestoreAsync(ClipboardEntryFactory.CreateTextEntry(text));

		// Assert
		autoClear
			.DidNotReceive()
			.Arm();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: a sensitive entry the clipboard refuses does not arm the auto-clear.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Does_Not_Arm_AutoClear_When_The_Write_Fails()
	{
		// Arrange
		const string secret = "Xk7#pQ2!mZ";

		IClipboardAutoClear autoClear = Substitute.For<IClipboardAutoClear>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			// The clipboard refuses the text.
			clipboard
				.SetTextAsync(secret, html: null, rtf: null, isSensitive: true)
				.Returns(false);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(autoClear);

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.RestoreAsync(ClipboardEntryFactory.CreateTextEntry(secret, isSensitive: true));

		// Assert
		autoClear
			.DidNotReceive()
			.Arm();
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: a sensitive text goes back to the clipboard marked as a secret.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Marks_A_Sensitive_Text_As_Secret_On_The_Clipboard()
	{
		// Arrange
		const string secret = "Xk7#pQ2!mZ";

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.RestoreAsync(ClipboardEntryFactory.CreateTextEntry(secret, isSensitive: true));

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync(secret, html: null, rtf: null, isSensitive: true);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: the restored entry becomes the active one.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Marks_Entry_Active()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry other = ClipboardEntryFactory.CreateTextEntry("other", [1]);

		ClipboardTextEntry target = ClipboardEntryFactory.CreateTextEntry("target", [2]);

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
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry target = ClipboardEntryFactory.CreateTextEntry("target", [2]);

		sut.Entries.Add(ClipboardEntryFactory.CreateTextEntry("other", [1]));

		sut.Entries.Add(target);

		List<ClipboardLogChangeKind> received = Capture(messenger);

		// Act
		await sut.RestoreAsync(target);

		// Assert
		sut.Entries[0]
			.Should()
			.Be(target);

		received
			.Should()
			.Contain(ClipboardLogChangeKind.Updated);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: restoring the top entry raises no notification.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Of_Top_Entry_Raises_No_Notification()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry top = ClipboardEntryFactory.CreateTextEntry("top", [1]);

		sut.Entries.Add(top);

		List<ClipboardLogChangeKind> received = Capture(messenger);

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
	/// <see cref="ClipboardLogService.RestoreAsync" />: an image goes back to the clipboard as its PNG.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Puts_An_Image_On_The_Clipboard()
	{
		// Arrange
		byte[] png = ImageFactory.CreatePng();

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.RestoreAsync(ClipboardEntryFactory.CreateImageEntry(png));

		// Assert
		await clipboard
			.Received(1)
			.SetImageAsync(png);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: the files and folders of an entry go back to the clipboard.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Puts_The_Files_On_The_Clipboard()
	{
		// Arrange
		ClipboardFilesEntry entry = ClipboardEntryFactory.CreateFilesEntry();

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.RestoreAsync(entry);

		// Assert
		await clipboard
			.Received(1)
			.SetFilesAsync(entry.FileSystemEntries);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: a text goes back to the clipboard with its HTML and RTF.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Puts_The_Html_And_Rtf_Of_A_Text_On_The_Clipboard()
	{
		// Arrange
		const string html = "<b>hello</b>";

		const string rtf = @"{\rtf1 hello}";

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.RestoreAsync(ClipboardEntryFactory.CreateTextEntry("hello", html: html, rtf: rtf));

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync("hello", html, rtf, isSensitive: false);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: restoring a link puts its text back on the clipboard.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Puts_The_Text_Of_A_Link_On_The_Clipboard()
	{
		// Arrange
		const string url = "https://example.com/page";

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(clipboard);
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		await sut.RestoreAsync(ClipboardEntryFactory.CreateUrlEntry(url, [1]));

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync(url, html: null, rtf: null, isSensitive: false);
	}

	/// <summary>
	/// <see cref="ClipboardLogService.RestoreAsync" />: keeping the position only highlights the entry, it does not move.
	/// </summary>
	[Test]
	public async Task RestoreAsync_With_KeepPosition_Marks_Active_Without_Moving()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ClipboardHistory>()
				.As<IClipboardHistory>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		ClipboardTextEntry first = ClipboardEntryFactory.CreateTextEntry("first", [1]);

		ClipboardTextEntry target = ClipboardEntryFactory.CreateTextEntry("target", [2]);

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
			.RegisterType<InlineDispatcherAccessor>()
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
			.RegisterType<InlineDispatcherAccessor>()
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
	/// <see cref="ClipboardLogService.TogglePin" />: the entry goes to the history.
	/// </summary>
	[Test]
	public void TogglePin_Hands_The_Entry_To_The_History()
	{
		// Arrange
		ClipboardTextEntry entry = ClipboardEntryFactory.CreateTextEntry("a", [1]);

		IClipboardHistory history = Substitute.For<IClipboardHistory>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(history));

		ClipboardLogService sut = mock.Create<ClipboardLogService>();

		// Act
		sut.TogglePin(entry);

		// Assert
		history
			.Received(1)
			.TogglePin(entry);
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
	#endregion
}
