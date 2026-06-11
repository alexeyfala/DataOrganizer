using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.ViewModels;
using NSubstitute;
using Serilog;
using System.Collections.ObjectModel;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(CustomClipboardViewModel)}"" type")]
internal class CustomClipboardViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="CustomClipboardViewModel" />: Clear is disabled when only pinned entries remain.
	/// </summary>
	[Test]
	public void ClearCommand_Is_Disabled_When_Only_Pinned()
	{
		// Arrange
		CustomClipboardViewModel sut = NewViewModel(PinnedTextEntry("p", [1]));

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
		CustomClipboardViewModel sut = NewViewModel(PinnedTextEntry("p", [1]), TextEntry("u", [2]));

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
		IClipboardHistoryService service = Substitute.For<IClipboardHistoryService>();

		service.Entries.Returns([]);

		CustomClipboardViewModel sut = new(service, Substitute.For<ILogger>());

		ClipboardTextEntry entry = TextEntry("a", [1]);

		// Act
		sut.TogglePinCommand.Execute(entry);

		// Assert
		service
			.Received(1)
			.TogglePin(entry);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds a view model whose history service exposes the supplied entries.
	/// </summary>
	private static CustomClipboardViewModel NewViewModel(params ClipboardHistoryEntryBase[] entries)
	{
		IClipboardHistoryService service = Substitute.For<IClipboardHistoryService>();

		service.Entries.Returns(new ObservableCollection<ClipboardHistoryEntryBase>(entries));

		return new CustomClipboardViewModel(service, Substitute.For<ILogger>());
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
