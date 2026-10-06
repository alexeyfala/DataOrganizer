using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Views;
using System.Linq;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetFieldView)}"" type")]
internal class DatasetFieldViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="DatasetFieldView.IsHidden" />: the toggle keeps the value while the view is out of the tree.
	/// </summary>
	[AvaloniaTest]
	public void IsHidden_Keeps_The_Toggle_On_While_The_View_Is_Out_Of_The_Tree()
	{
		// Arrange
		DatasetFieldView sut = new()
		{
			IsHidden = true
		};

		Window window = new()
		{
			Content = sut
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		ToggleButton toggle = sut
			.GetVisualDescendants()
			.OfType<ToggleButton>()
			.Single();

		// Act
		window.Content = null;

		Dispatcher.UIThread.RunJobs();

		// Assert
		toggle.IsChecked
			.Should()
			.BeTrue();
	}
	#endregion
}
