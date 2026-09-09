using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using NSubstitute;
using System.Linq;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(KeyValueInputView)}"" type")]
internal class KeyValueInputViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="KeyValueInputViewModel.IsSensitive" />: the declaration reaches the behavior of both input fields.
	/// </summary>
	[AvaloniaTest]
	public void IsSensitive_Reaches_The_Copy_Behavior_Of_Both_Inputs([Values] bool isSensitive)
	{
		// Arrange
		KeyValueInputViewModel viewModel = new(
			Application.Current!,
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
