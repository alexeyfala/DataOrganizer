using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DataOrganizer.Behaviors.Input;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using System.Linq;
using System.Windows.Input;

namespace DataOrganizer.UnitTests.Behaviors.Input;

[TestFixture(Description = $@"Tests of ""{nameof(ExpanderToggledByUserBehavior)}"" type")]
internal class ExpanderToggledByUserBehaviorTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExpanderToggledByUserBehavior.Command" />: a command that cannot execute is not run when the user expands
	/// the expander.
	/// </summary>
	[AvaloniaTest]
	public void Command_Is_Skipped_When_It_Cannot_Execute()
	{
		// Arrange
		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(false);

		Expander expander = new()
		{
			Content = new TextBlock
			{
				Text = "Content"
			},
			Header = "Header"
		};

		Window window = new()
		{
			Content = expander,
			Height = 200.0,
			Width = 300.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		ExpanderToggledByUserBehavior sut = new()
		{
			Command = command
		};

		sut.Attach(expander);

		ToggleButton header = expander
			.GetVisualDescendants()
			.OfType<ToggleButton>()
			.First();

		Point point = window.CenterOf(header);

		window.MouseMove(point);

		// Act
		window.Click(point);

		window.Close();

		// Assert
		command
			.DidNotReceiveWithAnyArgs()
			.Execute(default);
	}
	#endregion
}
