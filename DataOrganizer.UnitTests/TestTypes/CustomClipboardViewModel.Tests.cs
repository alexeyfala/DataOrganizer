using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
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
	/// <see cref="CustomClipboardViewModel" />: a blank query matches every entry, including
	/// pinned entries and ones without searchable text (e.g. images).
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	public void BuildPredicate_Blank_Query_Matches_Everything(string? query)
	{
		// Arrange
		Func<ClipboardHistoryEntryBase, bool> predicate = CustomClipboardViewModel.BuildPredicate(query);

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
	public void BuildPredicate_NonBlank_Query_Matches_By_SearchableText()
	{
		// Arrange
		Func<ClipboardHistoryEntryBase, bool> predicate = CustomClipboardViewModel.BuildPredicate("App");

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
	/// <see cref="CustomClipboardViewModel" />: Clear is disabled when only pinned entries remain.
	/// </summary>
	[Test]
	public void ClearCommand_Is_Disabled_When_Only_Pinned()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardHistoryService history = Substitute.For<IClipboardHistoryService>();

			history
				.Entries
				.Returns([PinnedTextEntry("p", [1])]);

			builder.RegisterInstance(history);
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
			IClipboardHistoryService history = Substitute.For<IClipboardHistoryService>();

			history
				.Entries
				.Returns([PinnedTextEntry("p", [1]), TextEntry("u", [2])]);

			builder.RegisterInstance(history);
		});

		CustomClipboardViewModel sut = mock.Create<CustomClipboardViewModel>();

		// Act, Assert
		sut.ClearCommand
			.CanExecute(null)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: toggling a pin delegates to the history service.
	/// </summary>
	[Test]
	public void TogglePin_Delegates_To_Service()
	{
		// Arrange
		IClipboardHistoryService history = Substitute.For<IClipboardHistoryService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			history
				.Entries
				.Returns([]);

			builder.RegisterInstance(history);
		});

		CustomClipboardViewModel sut = mock.Create<CustomClipboardViewModel>();

		ClipboardTextEntry entry = TextEntry("a", [1]);

		// Act
		sut.TogglePinCommand.Execute(entry);

		// Assert
		history
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
			IClipboardHistoryService history = Substitute.For<IClipboardHistoryService>();

			history
				.Entries
				.Returns([TextEntry("apple", [1]), TextEntry("banana", [2]), ImageEntry([3])]);

			builder.RegisterInstance(history);
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

		ObservableCollection<ClipboardHistoryEntryBase> entries = [pinned, older];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardHistoryService history = Substitute.For<IClipboardHistoryService>();

			history
				.Entries
				.Returns(entries);

			builder.RegisterInstance(history);
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
	#endregion
}
