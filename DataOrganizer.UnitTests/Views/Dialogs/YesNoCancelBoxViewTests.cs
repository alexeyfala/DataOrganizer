using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.ViewModels.Dialogs;
using DataOrganizer.Views.Dialogs;
using NSubstitute;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Views.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(YesNoCancelBoxView)}"" type")]
internal class YesNoCancelBoxViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="YesNoCancelBoxViewModel.GetResultAsync" />: the Escape key answers with the button
	/// that handles it in the shown variant.
	/// </summary>
	/// <remarks>
	/// The answer rests on Avalonia wiring <see cref="Button.IsCancel" /> to the key, so nothing but
	/// a test through the real input pipeline covers it.
	/// </remarks>
	[AvaloniaTest]
	public async Task Escape_Answers_With_The_Button_That_Handles_It([Values] YesNoCancelButtons variant)
	{
		// Arrange
		YesNoCancelBoxViewModel viewModel = new(
			Application.Current!,
			Substitute.For<ITaskExceptionHandler>());

		// The view carries the buttons on its own; a dialog host would only add a registry shared
		// with every other test.
		Window window = new()
		{
			Content = new YesNoCancelBoxView(viewModel)
		};

		window.Show();

		Task<YesNoCancelAnswer> answer = viewModel.GetResultAsync(variant);

		Dispatcher.UIThread.RunJobs();

		window.UpdateLayout();

		// Act
		window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		YesNoCancelAnswer actual = await answer;

		// Closed before the assertion, otherwise a failure leaves the window to the following tests.
		window.Close();

		// Assert
		YesNoCancelAnswer expected = variant == YesNoCancelButtons.YesNo
			? YesNoCancelAnswer.No
			: YesNoCancelAnswer.Cancel;

		actual
			.Should()
			.Be(expected);
	}
	#endregion
}
