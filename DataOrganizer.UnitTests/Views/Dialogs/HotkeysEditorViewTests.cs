using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.ViewModels.Dialogs;
using DataOrganizer.Views.Dialogs;
using NSubstitute;

namespace DataOrganizer.UnitTests.Views.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(HotkeysEditorView)}"" type")]
internal class HotkeysEditorViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="HotkeysEditorViewModel.SaveAndCloseCommand" />: a press of Enter does not save, the save waits for the
	/// release.
	/// </summary>
	[AvaloniaTest]
	public void Enter_Down_Leaves_The_Dialog_Open()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel viewModel = mock.Create<HotkeysEditorViewModel>();

		HotkeysEditorView view = new(viewModel);

		// Act
		RaiseKey(view, InputElement.KeyDownEvent, Key.Enter, KeyModifiers.None);

		// Assert
		viewModel.IsSaved
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.SaveAndCloseCommand" />: the release of Enter saves the hotkeys and closes the
	/// dialog.
	/// </summary>
	[AvaloniaTest]
	public void Enter_Up_Saves_And_Closes()
	{
		// Arrange
		IDialogHostCloser dialogHostCloser = Substitute.For<IDialogHostCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogHostCloser));

		HotkeysEditorViewModel viewModel = mock.Create<HotkeysEditorViewModel>();

		HotkeysEditorView view = new(viewModel);

		// Act
		RaiseKey(view, InputElement.KeyUpEvent, Key.Enter, KeyModifiers.None);

		// Assert
		viewModel.IsSaved
			.Should()
			.BeTrue();

		dialogHostCloser
			.Received(1)
			.Close();
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.SaveAndCloseCommand" />: the release of Enter with a modifier leaves the dialog
	/// open.
	/// </summary>
	[AvaloniaTest]
	public void Enter_Up_With_A_Modifier_Leaves_The_Dialog_Open(
		[Values(KeyModifiers.Alt, KeyModifiers.Control, KeyModifiers.Shift)] KeyModifiers modifiers)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel viewModel = mock.Create<HotkeysEditorViewModel>();

		HotkeysEditorView view = new(viewModel);

		// Act
		RaiseKey(view, InputElement.KeyUpEvent, Key.Enter, modifiers);

		// Assert
		viewModel.IsSaved
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.SaveAndCloseCommand" />: the release of a key other than Enter leaves the dialog
	/// open.
	/// </summary>
	[AvaloniaTest]
	public void Key_Up_Of_Another_Key_Leaves_The_Dialog_Open()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel viewModel = mock.Create<HotkeysEditorViewModel>();

		HotkeysEditorView view = new(viewModel);

		// Act
		RaiseKey(view, InputElement.KeyUpEvent, Key.A, KeyModifiers.None);

		// Assert
		viewModel.IsSaved
			.Should()
			.BeFalse();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Raises a key event on a shown view.
	/// </summary>
	private static void RaiseKey(
		HotkeysEditorView view,
		RoutedEvent<KeyEventArgs> routedEvent,
		Key key,
		KeyModifiers modifiers)
	{
		Window window = new()
		{
			Content = view
		};

		window.Show();

		view.RaiseEvent(new KeyEventArgs
		{
			Key = key,
			KeyModifiers = modifiers,
			RoutedEvent = routedEvent
		});

		window.Close();
	}
	#endregion
}
