using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using NSubstitute;
using System.Linq;

namespace DataOrganizer.UnitTests;

[TestFixture(Description = $@"Tests of ""{nameof(MultilineTextEditView)}"" type")]
internal class MultilineTextEditViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="MultilineTextEditViewModel.IsSensitive" />: the declaration reaches the behavior that writes the copy.
	/// </summary>
	[AvaloniaTest]
	public void IsSensitive_Reaches_The_Copy_Behavior([Values] bool isSensitive)
	{
		// Arrange
		MultilineTextEditViewModel viewModel = new(
			Application.Current!,
			Substitute.For<ITaskExceptionHandler>())
		{
			IsSensitive = isSensitive
		};

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
