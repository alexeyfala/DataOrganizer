using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.ViewModels;
using NSubstitute;
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
	#endregion

	#region Helpers
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
