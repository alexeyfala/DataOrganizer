using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Controls;
using System.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that the cards of the Material menus are outlined, as a white card blends into the light page")]
internal class FlyoutOutlineTests
{
	#region Data
	/// <summary>
	/// Key of the brush of the outline.
	/// </summary>
	private const string OutlineBrushKey = "MaterialDividerBrush";
	#endregion

	#region Methods
	/// <summary>
	/// The card of a <see cref="Flyout" />, the base of the <see cref="FlyoutButton" /> menus, has an outline.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Card_Has_An_Outline()
	{
		// Arrange
		Flyout flyout = new()
		{
			Content = new TextBlock
			{
				Text = "Item"
			}
		};

		// Act
		Control presenter = Open<FlyoutPresenter>(flyout);

		// Assert
		GetOutlines(presenter)
			.Should()
			.NotBeEmpty();
	}

	/// <summary>
	/// The card of a <see cref="MenuFlyout" /> has an outline.
	/// </summary>
	[AvaloniaTest]
	public void MenuFlyout_Card_Has_An_Outline()
	{
		// Arrange
		MenuFlyout flyout = new()
		{
			Items =
			{
				new MenuItem
				{
					Header = "Item"
				}
			}
		};

		// Act
		Control presenter = Open<MenuFlyoutPresenter>(flyout);

		// Assert
		GetOutlines(presenter)
			.Should()
			.NotBeEmpty();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the borders drawn with the outline within a presenter.
	/// </summary>
	private static Border[] GetOutlines(Control presenter)
	{
		object? brush = Application.Current!.FindResource(OutlineBrushKey);

		return [.. presenter
			.GetVisualDescendants()
			.OfType<Border>()
			.Where(x => x.BorderThickness == new Thickness(1.0) && ReferenceEquals(x.BorderBrush, brush))];
	}

	/// <summary>
	/// Opens a flyout in a window and returns its presenter.
	/// </summary>
	private static TPresenter Open<TPresenter>(FlyoutBase flyout) where TPresenter : Control
	{
		Border target = new()
		{
			Height = 300.0,
			Width = 300.0
		};

		Window window = new()
		{
			Content = target,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		flyout.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		return window
			.GetVisualDescendants()
			.OfType<TPresenter>()
			.Single();
	}
	#endregion
}
