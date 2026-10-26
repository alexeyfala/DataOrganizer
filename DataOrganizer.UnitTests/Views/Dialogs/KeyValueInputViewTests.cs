using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Security;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.ViewModels.Dialogs;
using DataOrganizer.Views.Dialogs;
using Diagnostics.Interfaces;
using NSubstitute;
using System.Linq;

namespace DataOrganizer.UnitTests.Views.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(KeyValueInputView)}"" type")]
internal class KeyValueInputViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="KeyValueInputViewModel.IsKeyMasked" />, <see cref="KeyValueInputViewModel.IsValueMasked" />: a masked key
	/// or value is not given to UI Automation as a value, with the eye open or closed, and an open one is.
	/// </summary>
	[AvaloniaTest]
	public void IsKeyMasked_Hides_The_Masked_Inputs_From_UI_Automation(
		[Values] bool isMasked,
		[Values] bool isRevealed)
	{
		// Arrange
		const string key = "Key";

		const string value = "Value";

		KeyValueInputViewModel viewModel = new(
			Substitute.For<IDialogCloser>(),
			Substitute.For<ITaskExceptionHandler>())
		{
			IsKeyMasked = isMasked,
			IsValueInputVisible = true,
			IsValueMasked = isMasked,
			Key = key,
			Value = value
		};

		KeyValueInputView view = new(viewModel);

		Window window = new()
		{
			Content = view
		};

		window.Show();

		TextBox[] inputs =
		[
			.. view
				.GetLogicalDescendants()
				.OfType<TextBox>()
		];

		foreach (TextBox input in inputs)
		{
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
		string?[] expected = isMasked
			? [string.Empty, string.Empty]
			: [key, value];

		values
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="KeyValueInputViewModel.IsSensitive" />: the declaration reaches the behavior of both input fields.
	/// </summary>
	[AvaloniaTest]
	public void IsSensitive_Reaches_The_Copy_Behavior_Of_Both_Inputs([Values] bool isSensitive)
	{
		// Arrange
		KeyValueInputViewModel viewModel = new(
			Substitute.For<IDialogCloser>(),
			Substitute.For<ITaskExceptionHandler>())
		{
			IsSensitive = isSensitive
		};

		KeyValueInputView view = new(viewModel);

		// Act
		SensitiveCopyBehavior[] behaviors =
		[
			.. view
				.GetLogicalDescendants()
				.OfType<TextBox>()
				.Select(x => Interaction
					.GetBehaviors(x)
					.OfType<SensitiveCopyBehavior>()
					.Single())
		];

		// Assert
		behaviors
			.Should()
			.HaveCount(2)
			.And
			.OnlyContain(x => x.IsSensitive == isSensitive);
	}
	#endregion
}
