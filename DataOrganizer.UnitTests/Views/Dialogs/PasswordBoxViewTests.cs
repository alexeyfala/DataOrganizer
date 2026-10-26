using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.ViewModels.Dialogs;
using DataOrganizer.Views.Dialogs;
using Diagnostics.Interfaces;
using NSubstitute;
using System.Linq;

namespace DataOrganizer.UnitTests.Views.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(PasswordBoxView)}"" type")]
internal class PasswordBoxViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="PasswordBoxView" />: neither the password nor its confirmation is given to UI Automation as a value, with
	/// the eye open or closed.
	/// </summary>
	[AvaloniaTest]
	public void Inputs_Give_No_Password_As_A_Value([Values] bool isRevealed)
	{
		// Arrange
		PasswordBoxViewModel viewModel = new(
			Substitute.For<IDialogCloser>(),
			Substitute.For<ITaskExceptionHandler>())
		{
			IsConfirmationVisible = true
		};

		PasswordBoxView view = new(viewModel);

		Window window = new()
		{
			Content = view
		};

		window.Show();

		TextBox[] inputs = [view.PasswordInput, view.ConfirmationInput];

		foreach (TextBox input in inputs)
		{
			input.Text = "Password1!";

			input.RevealPassword = isRevealed;
		}

		Dispatcher.UIThread.RunJobs();

		// Act
		string?[] values =
		[
			.. inputs.Select(x => ControlAutomationPeer
				.CreatePeerForElement(x)
				.GetProvider<IValueProvider>()!
				.Value)
		];

		// Assert
		values
			.Should()
			.HaveCount(2)
			.And
			.OnlyContain(x => string.IsNullOrEmpty(x));
	}
	#endregion
}
