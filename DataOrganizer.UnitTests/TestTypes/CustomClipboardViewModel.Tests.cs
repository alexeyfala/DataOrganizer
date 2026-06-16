using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.ViewModels;
using NSubstitute;
using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(CustomClipboardViewModel)}"" type")]
internal class CustomClipboardViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: the type filter and the search query combine (AND).
	/// </summary>
	[Test]
	public void ActiveFilter_Combines_With_Search()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService log = Substitute.For<IClipboardLogService>();

			log
				.Entries
				.Returns([TextEntry("apple", [1]), UrlEntry("https://apple.com", [2]), ImageEntry([3])]);

			builder.RegisterInstance(log);
		});

		CustomClipboardViewModel sut = mock.Create<CustomClipboardViewModel>();

		// Act — set the query first (its trigger is debounced, so it does not refresh synchronously),
		// then flip the type filter to force a single refresh that reads both predicates.
		sut.SearchText = "banana";

		sut.ActiveFilter = ClipboardLogEntryFilter.Text;

		// Assert — the only text entry ("apple") fails the "banana" query, so nothing matches both.
		sut.VisibleEntries
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: setting <c>ActiveFilter</c> narrows <c>VisibleEntries</c>
	/// to the matching payload type.
	/// </summary>
	[Test]
	public void ActiveFilter_Narrows_VisibleEntries_To_Matching_Type()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService log = Substitute.For<IClipboardLogService>();

			log
				.Entries
				.Returns([TextEntry("apple", [1]), UrlEntry("https://apple.com", [2]), ImageEntry([3])]);

			builder.RegisterInstance(log);
		});

		CustomClipboardViewModel sut = mock.Create<CustomClipboardViewModel>();

		// Act — show only text entries.
		sut.ActiveFilter = ClipboardLogEntryFilter.Text;

		// Assert — the URL and image are filtered out.
		sut.VisibleEntries
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<ClipboardTextEntry>();
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: a blank query matches every entry, including
	/// pinned entries and ones without searchable text (e.g. images).
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	public void BuildSearchPredicate_Blank_Query_Matches_Everything(string? query)
	{
		// Arrange
		Func<ClipboardLogEntryBase, bool> predicate = CustomClipboardViewModel.BuildSearchPredicate(query);

		// Act, Assert
		predicate(TextEntry("anything", [1]))
			.Should()
			.BeTrue();

		predicate(PinnedTextEntry("pinned", [2]))
			.Should()
			.BeTrue();

		predicate(ImageEntry([3]))
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: a non-blank query matches by <c>SearchableText</c>
	/// case-insensitively, applies to pinned entries, and excludes entries without searchable text.
	/// </summary>
	[Test]
	public void BuildSearchPredicate_NonBlank_Query_Matches_By_SearchableText()
	{
		// Arrange
		Func<ClipboardLogEntryBase, bool> predicate = CustomClipboardViewModel.BuildSearchPredicate("App");

		// Act, Assert
		predicate(TextEntry("Application", [1]))
			.Should()
			.BeTrue();

		// Case-insensitive.
		predicate(TextEntry("an apple", [2]))
			.Should()
			.BeTrue();

		predicate(TextEntry("banana", [3]))
			.Should()
			.BeFalse();

		// The filter applies to pinned entries too.
		predicate(PinnedTextEntry("banana", [4]))
			.Should()
			.BeFalse();

		// Images have no searchable text, so they are hidden while searching.
		predicate(ImageEntry([5]))
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: <see cref="ClipboardLogEntryFilter.All" /> matches every payload type.
	/// </summary>
	[Test]
	public void BuildTypePredicate_All_Matches_Everything()
	{
		// Arrange
		Func<ClipboardLogEntryBase, bool> predicate = CustomClipboardViewModel.BuildTypePredicate(ClipboardLogEntryFilter.All);

		// Act, Assert
		predicate(TextEntry("text", [1]))
			.Should()
			.BeTrue();

		predicate(UrlEntry("https://example.com", [2]))
			.Should()
			.BeTrue();

		predicate(ImageEntry([3]))
			.Should()
			.BeTrue();

		predicate(FilesEntry([4]))
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: each non-text filter matches only its own payload type.
	/// </summary>
	[Test]
	public void BuildTypePredicate_Matches_Only_Its_Own_Type()
	{
		// Arrange
		ClipboardLogEntryBase url = UrlEntry("https://example.com", [1]);

		ClipboardLogEntryBase image = ImageEntry([2]);

		ClipboardLogEntryBase files = FilesEntry([3]);

		// Act, Assert
		CustomClipboardViewModel.BuildTypePredicate(ClipboardLogEntryFilter.Url)(url)
			.Should()
			.BeTrue();

		CustomClipboardViewModel.BuildTypePredicate(ClipboardLogEntryFilter.Url)(image)
			.Should()
			.BeFalse();

		CustomClipboardViewModel.BuildTypePredicate(ClipboardLogEntryFilter.Image)(image)
			.Should()
			.BeTrue();

		CustomClipboardViewModel.BuildTypePredicate(ClipboardLogEntryFilter.Image)(files)
			.Should()
			.BeFalse();

		CustomClipboardViewModel.BuildTypePredicate(ClipboardLogEntryFilter.Files)(files)
			.Should()
			.BeTrue();

		CustomClipboardViewModel.BuildTypePredicate(ClipboardLogEntryFilter.Files)(url)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: the <see cref="ClipboardLogEntryFilter.Text" /> filter matches plain text
	/// but excludes URLs (which have their own filter).
	/// </summary>
	[Test]
	public void BuildTypePredicate_Text_Excludes_Url()
	{
		// Arrange
		Func<ClipboardLogEntryBase, bool> predicate = CustomClipboardViewModel.BuildTypePredicate(ClipboardLogEntryFilter.Text);

		// Act, Assert
		predicate(TextEntry("text", [1]))
			.Should()
			.BeTrue();

		predicate(UrlEntry("https://example.com", [2]))
			.Should()
			.BeFalse();

		predicate(ImageEntry([3]))
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: Clear is disabled when only pinned entries remain.
	/// </summary>
	[Test]
	public void ClearCommand_Is_Disabled_When_Only_Pinned()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService log = Substitute.For<IClipboardLogService>();

			log
				.Entries
				.Returns([PinnedTextEntry("p", [1])]);

			builder.RegisterInstance(log);
		});

		CustomClipboardViewModel sut = mock.Create<CustomClipboardViewModel>();

		// Act, Assert
		sut.ClearCommand
			.CanExecute(null)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: Clear is enabled while an unpinned entry exists.
	/// </summary>
	[Test]
	public void ClearCommand_Is_Enabled_With_Unpinned()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService log = Substitute.For<IClipboardLogService>();

			log
				.Entries
				.Returns([PinnedTextEntry("p", [1]), TextEntry("u", [2])]);

			builder.RegisterInstance(log);
		});

		CustomClipboardViewModel sut = mock.Create<CustomClipboardViewModel>();

		// Act, Assert
		sut.ClearCommand
			.CanExecute(null)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: search is disabled for image and file filters (no searchable text)
	/// and enabled for the rest.
	/// </summary>
	[TestCase(ClipboardLogEntryFilter.All, true)]
	[TestCase(ClipboardLogEntryFilter.Text, true)]
	[TestCase(ClipboardLogEntryFilter.Url, true)]
	[TestCase(ClipboardLogEntryFilter.Image, false)]
	[TestCase(ClipboardLogEntryFilter.Files, false)]
	public void IsSearchEnabled_Reflects_Whether_Filter_Has_Searchable_Text(ClipboardLogEntryFilter filter, bool expected)
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService log = Substitute.For<IClipboardLogService>();

			log
				.Entries
				.Returns([]);

			builder.RegisterInstance(log);
		});

		CustomClipboardViewModel sut = mock.Create<CustomClipboardViewModel>();

		// Act
		sut.ActiveFilter = filter;

		// Assert
		sut.IsSearchEnabled
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: switching to a non-searchable filter stashes and empties the query
	/// (so a leftover search does not leave the list empty), then restores it on return to a searchable filter.
	/// </summary>
	[Test]
	public void Switching_To_NonSearchable_Filter_Stashes_And_Restores_SearchText()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService log = Substitute.For<IClipboardLogService>();

			log
				.Entries
				.Returns([]);

			builder.RegisterInstance(log);
		});

		CustomClipboardViewModel sut = mock.Create<CustomClipboardViewModel>();

		sut.SearchText = "query";

		// Act — enter a non-searchable filter: the query is stashed and the box emptied.
		sut.ActiveFilter = ClipboardLogEntryFilter.Image;

		// Assert
		sut.SearchText
			.Should()
			.BeNull();

		// Act — a second non-searchable filter must not overwrite the stash.
		sut.ActiveFilter = ClipboardLogEntryFilter.Files;

		// Act — return to a searchable filter: the query is restored.
		sut.ActiveFilter = ClipboardLogEntryFilter.Text;

		// Assert
		sut.SearchText
			.Should()
			.Be("query");
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: toggling a pin delegates to the history service.
	/// </summary>
	[Test]
	public void TogglePin_Delegates_To_Service()
	{
		// Arrange
		IClipboardLogService log = Substitute.For<IClipboardLogService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			log
				.Entries
				.Returns([]);

			builder.RegisterInstance(log);
		});

		CustomClipboardViewModel sut = mock.Create<CustomClipboardViewModel>();

		ClipboardTextEntry entry = TextEntry("a", [1]);

		// Act
		sut.TogglePinCommand.Execute(entry);

		// Assert
		log
			.Received(1)
			.TogglePin(entry);
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: with no search query, <c>VisibleEntries</c> mirrors all history entries.
	/// </summary>
	[Test]
	public void VisibleEntries_Mirrors_All_Entries_Without_Search()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService log = Substitute.For<IClipboardLogService>();

			log
				.Entries
				.Returns([TextEntry("apple", [1]), TextEntry("banana", [2]), ImageEntry([3])]);

			builder.RegisterInstance(log);
		});

		// Act
		CustomClipboardViewModel sut = mock.Create<CustomClipboardViewModel>();

		// Assert
		sut.VisibleEntries
			.Should()
			.HaveCount(3);
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: a new entry inserted below the pinned block keeps its
	/// source position in <c>VisibleEntries</c> (regression: live inserts must not jump to the bottom).
	/// </summary>
	[Test]
	public void VisibleEntries_New_Entry_Below_Pinned_Keeps_Source_Order()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		ClipboardTextEntry pinned = PinnedTextEntry("pinned", [1]);

		ClipboardTextEntry older = TextEntry("older", [2]);

		ObservableCollection<ClipboardLogEntryBase> entries = [pinned, older];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService log = Substitute.For<IClipboardLogService>();

			log
				.Entries
				.Returns(entries);

			builder.RegisterInstance(log);
		});

		CustomClipboardViewModel sut = mock.Create<CustomClipboardViewModel>();

		// Act — mimic the service: insert the new entry just below the pinned block.
		ClipboardTextEntry fresh = TextEntry("fresh", [3]);

		entries.Insert(1, fresh);

		// Assert
		sut.VisibleEntries
			.Should()
			.ContainInOrder(pinned, fresh, older);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// A minimal files entry with a single file and the given hash.
	/// </summary>
	private static ClipboardFilesEntry FilesEntry(byte[] hash) => new()
	{
		FileSystemEntries = [new ClipboardFileSystemEntry(@"C:\file.txt", IsFolder: false)],
		Hash = hash
	};

	/// <summary>
	/// A minimal image entry (no searchable text) with the given hash.
	/// </summary>
	private static ClipboardImageEntry ImageEntry(byte[] hash) => new()
	{
		OriginalPng = [],
		Hash = hash
	};

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

	/// <summary>
	/// A minimal URL entry with the given hash.
	/// </summary>
	private static ClipboardUrlEntry UrlEntry(string url, byte[] hash) => new()
	{
		Text = url,
		Html = null,
		Rtf = null,
		Url = url,
		Hash = hash
	};
	#endregion
}
