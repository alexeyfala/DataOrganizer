using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.UI.Behaviors.Security;
using DataOrganizer.UI.ViewModels.Dialogs;
using DataOrganizer.UI.Views.Dialogs;
using System.Linq;

namespace DataOrganizer.UI.UnitTests.Views.Dialogs;

/// <summary>
/// Tests of <see cref="MultilineTextEditView" /> type.
/// </summary>
[TestFixture]
internal sealed class MultilineTextEditViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="MultilineTextEditViewModel.IsSensitive" />: the declaration reaches the behavior that writes the copy.
	/// </summary>
	[AvaloniaTest]
	public void IsSensitive_Reaches_The_Copy_Behavior([Values] bool isSensitive)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		MultilineTextEditViewModel viewModel = mock.Create<MultilineTextEditViewModel>();

		viewModel.IsSensitive = isSensitive;

		MultilineTextEditView view = new(viewModel);

		// Act
		TextBox input = view
			.GetLogicalDescendants()
			.OfType<TextBox>()
			.Single();

		SensitiveCopyBehavior behavior = Interaction
			.GetBehaviors(input)
			.OfType<SensitiveCopyBehavior>()
			.Single();

		// Assert
		behavior.IsSensitive
			.Should()
			.Be(isSensitive);
	}
	#endregion
}
