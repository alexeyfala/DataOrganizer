using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Controls;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(PathTextBlock)}"" type")]
internal class PathTextBlockTests
{
	#region Data
	/// <summary>
	/// Width that the path cannot fit on one line.
	/// </summary>
	private const double NarrowWidth = 160.0;

	/// <summary>
	/// A path longer than <see cref="NarrowWidth" />, whose names have no spaces to break at.
	/// </summary>
	private static readonly string LongPath = TestPath.Combine(
		"Users",
		"alexey",
		"AppData",
		"Local",
		"DataOrganizer_Debug",
		"Instance",
		"Database",
		"DataOrganizer.sqlite");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="AutomationPeer.GetName" />: the path names the text for UI Automation as it is, without the marks it
	/// wraps at.
	/// </summary>
	[AvaloniaTest]
	public void AutomationPeer_Is_Named_By_The_Path()
	{
		// Arrange
		PathTextBlock sut = new()
		{
			Path = LongPath
		};

		Show(sut);

		// Act
		string result = ControlAutomationPeer
			.CreatePeerForElement(sut)
			.GetName();

		// Assert
		result
			.Should()
			.Be(LongPath);
	}

	/// <summary>
	/// <see cref="SelectableTextBlock.Copy" />: the copied path is the one shown, with nothing the wrapping added to it.
	/// </summary>
	[AvaloniaTest]
	public async Task Copy_Puts_The_Path_On_The_Clipboard_As_It_Is()
	{
		// Arrange
		PathTextBlock sut = new()
		{
			Path = LongPath,
			TextWrapping = TextWrapping.Wrap,
			Width = NarrowWidth
		};

		Show(sut);

		sut.SelectAll();

		// Act
		sut.Copy();

		Dispatcher.UIThread.RunJobs();

		string? result = await TopLevel
			.GetTopLevel(sut)!
			.Clipboard!
			.TryGetTextAsync();

		// Assert
		result
			.Should()
			.Be(LongPath);
	}

	/// <summary>
	/// <see cref="PathTextBlock.Path" />: a path too long for one line breaks after its separators, not inside its names.
	/// </summary>
	[AvaloniaTest]
	public void Path_Wraps_After_Its_Separators()
	{
		// Arrange
		PathTextBlock sut = new()
		{
			Path = LongPath,
			TextWrapping = TextWrapping.Wrap,
			Width = NarrowWidth
		};

		// Act
		Show(sut);

		// Assert
		string[] lines = [.. sut.TextLayout.TextLines.Select(x => sut.Text!.Substring(x.FirstTextSourceIndex, x.Length))];

		lines
			.Should()
			.HaveCountGreaterThan(1);

		lines.SkipLast(1).Select(static x => x.Last(static c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.Format))
			.Should()
			.OnlyContain(static x => x == '\\' || x == '/');
	}

	/// <summary>
	/// <see cref="PathTextBlock" />: the styles of a selectable text block reach it.
	/// </summary>
	[AvaloniaTest]
	public void StyleKeyOverride_Lets_The_Styles_Of_A_Selectable_Text_Block_Reach_It()
	{
		// Arrange
		const double fontSize = 31.0;

		PathTextBlock sut = new()
		{
			Classes =
			{
				"probe"
			}
		};

		Window window = new()
		{
			Content = sut,
			Styles =
			{
				new Style(static x => x
					.OfType<SelectableTextBlock>()
					.Class("probe"))
				{
					Setters =
					{
						new Setter(TextBlock.FontSizeProperty, fontSize)
					}
				}
			}
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.FontSize
			.Should()
			.Be(fontSize);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Shows the control in a window and lets the layout settle.
	/// </summary>
	private static void Show(Control content)
	{
		Window window = new()
		{
			Content = content
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();
	}
	#endregion
}
