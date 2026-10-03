using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using Material.Styles.Controls;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that the copy of Material's side sheet theme still fits Material's side sheet")]
internal class ScrollableSideSheetThemeTests
{
	#region Data
	/// <summary>
	/// Name of the template part that dims the content behind an opened sheet.
	/// </summary>
	private const string ScrimPartName = "PART_Scrim";

	/// <summary>
	/// Name of the template part that holds the sheet.
	/// </summary>
	private const string SheetPartName = "PART_SideSheet";

	/// <summary>
	/// Resource key of the theme under test.
	/// </summary>
	private const string ThemeKey = "ScrollableSideSheetTheme";

	/// <summary>
	/// Path of the resource file of the theme within the application.
	/// </summary>
	private const string ThemePath = "/Styling/SideSheetControlTheme.axaml";
	#endregion

	#region Methods
	/// <summary>
	/// A closed sheet moves out to the right, beyond the edge of the control.
	/// </summary>
	[AvaloniaTest]
	public void Closed_Sheet_Moves_Out_Of_View()
	{
		// Arrange
		SideSheet sut = new()
		{
			SideSheetOpened = false,
			Theme = GetTheme()
		};

		// Act
		Show(sut);

		// Assert
		Border sheet = GetPart(sut, SheetPartName);

		double? left = sheet.TranslatePoint(default, sut)?.X;

		left
			.Should()
			.Be(sut.Bounds.Width);
	}

	/// <summary>
	/// The content of an opened sheet gets the height of the sheet, so its scroll viewer scrolls a taller content.
	/// </summary>
	[AvaloniaTest]
	public void Content_Scrolls_Within_The_Sheet()
	{
		// Arrange
		ScrollViewer content = new()
		{
			Content = new Border
			{
				Height = 2000.0
			}
		};

		SideSheet sut = new()
		{
			SideSheetContent = content,
			SideSheetOpened = true,
			Theme = GetTheme()
		};

		// Act
		Show(sut);

		// Assert
		content.Viewport.Height
			.Should()
			.BeLessThan(content.Extent.Height);
	}

	/// <summary>
	/// An opened sheet stands at the right edge of the control, where the desktop variant of the theme docks it.
	/// </summary>
	[AvaloniaTest]
	public void Opened_Sheet_Stands_At_The_Right_Edge()
	{
		// Arrange
		SideSheet sut = new()
		{
			SideSheetOpened = true,
			Theme = GetTheme()
		};

		// Act
		Show(sut);

		// Assert
		Border sheet = GetPart(sut, SheetPartName);

		double? right = sheet.TranslatePoint(new(sheet.Bounds.Width, 0.0), sut)?.X;

		right
			.Should()
			.Be(sut.Bounds.Width);
	}

	/// <summary>
	/// The scrim stays hidden behind an opened sheet on the desktop, where it would dim the content and catch its clicks.
	/// </summary>
	[AvaloniaTest]
	public void Scrim_Stays_Hidden_On_The_Desktop()
	{
		// Arrange
		SideSheet sut = new()
		{
			SideSheetOpened = true,
			Theme = GetTheme()
		};

		// Act
		Show(sut);

		// Assert
		GetPart(sut, ScrimPartName).IsVisible
			.Should()
			.BeFalse();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the part of the template of the sheet with the name.
	/// </summary>
	private static Border GetPart(SideSheet sheet, string name)
	{
		return sheet
			.GetVisualDescendants()
			.OfType<Border>()
			.Single(x => x.Name == name);
	}

	/// <summary>
	/// Returns the theme under test, read from its resource file.
	/// </summary>
	private static ControlTheme GetTheme()
	{
		Uri source = new($"avares://{typeof(App).Assembly.GetName().Name}{ThemePath}");

		ResourceDictionary resources = (ResourceDictionary)AvaloniaXamlLoader.Load(source);

		return (ControlTheme)resources[ThemeKey]!;
	}

	/// <summary>
	/// Shows the sheet in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static void Show(SideSheet sheet)
	{
		Window window = new()
		{
			Content = sheet,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();
	}
	#endregion
}
