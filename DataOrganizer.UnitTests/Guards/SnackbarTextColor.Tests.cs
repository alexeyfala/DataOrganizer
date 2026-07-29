using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Enums;
using DataOrganizer.Messages;
using Material.Ripple;
using Material.Styles.Controls;
using Material.Styles.Models;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that snackbar text is coloured by its own template instead of the host's inherited Foreground")]
internal class SnackbarTextColorTests
{
	#region Data
	// Attribute that must not be set on SnackbarHost: it is inherited by the whole window subtree.
	private const string ForegroundAttributeName = "Foreground";

	private const string SnackbarHostElementName = "SnackbarHost";
	#endregion

	#region Methods
	/// <summary>
	/// No view sets Foreground on SnackbarHost, so a null brush cannot be inherited by the window subtree.
	/// </summary>
	[Test]
	public void No_View_Sets_Foreground_On_SnackbarHost()
	{
		// Act
		XElement[] hosts = [.. EnumerateProjectMarkup()
			.SelectMany(document => document.Descendants())
			.Where(element => element.Name.LocalName == SnackbarHostElementName)];

		// Assert
		hosts
			.Should()
			.NotBeEmpty();

		hosts
			.Should()
			.AllSatisfy(element => element
				.Attribute(ForegroundAttributeName)
				.Should()
				.BeNull());
	}

	/// <summary>
	/// A posted <see cref="ShowSnackbarMessage" /> is rendered by the application template as text coloured by its level.
	/// </summary>
	[AvaloniaTest]
	[TestCase(SnackbarMessageLevel.Information, "")]
	[TestCase(SnackbarMessageLevel.Warning, "Warning")]
	[TestCase(SnackbarMessageLevel.Error, "Error")]
	public void Posted_Message_Is_Rendered_With_Level_Color(SnackbarMessageLevel level, string expectedClass)
	{
		// Arrange
		const string text = "Snackbar text";

		SnackbarHost host = new()
		{
			HostName = $"{nameof(SnackbarHost)}{level}"
		};

		Window window = new()
		{
			Content = host
		};

		window.Show();

		// Act
		SnackbarHost.Post(
			new SnackbarModel(new ShowSnackbarMessage(text, level), TimeSpan.FromSeconds(5.0)),
			host.HostName,
			DispatcherPriority.Normal);

		Dispatcher.UIThread.RunJobs();

		window.UpdateLayout();

		TextBlock? textBlock = host
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.FirstOrDefault(x => x.Text == text);

		// Assert
		textBlock
			.Should()
			.NotBeNull();

		textBlock!
			.Classes
			.Should()
			.Contain("SnackbarTextBlockStyle");

		textBlock
			.Foreground
			.Should()
			.BeAssignableTo<ISolidColorBrush>()
			.Which
			.Color
			.Should()
			.Be(ExpectedColor(level));

		if (expectedClass.Length > 0)
		{
			textBlock
				.Classes
				.Should()
				.Contain(expectedClass);
		}
	}

	/// <summary>
	/// Overflow arrows of a scrollable tab control keep a ripple brush when hosted inside a SnackbarHost.
	/// </summary>
	[AvaloniaTest]
	public void Tab_Scroller_Arrow_Keeps_Ripple_Brush()
	{
		// Arrange
		Application application = Application.Current!;

		application
			.TryGetResource("ScrollableTabControl", application.ActualThemeVariant, out object? theme)
			.Should()
			.BeTrue();

		TabControl tabControl = new()
		{
			ItemsSource = Enumerable
				.Range(0, 30)
				.Select(index => $"Tab header {index}")
				.ToArray(),
			Theme = (ControlTheme)theme!
		};

		SnackbarHost host = new()
		{
			Content = tabControl,
			HostName = $"{nameof(SnackbarHost)}Scroller"
		};

		Window window = new()
		{
			Content = host,
			Height = 200,
			Width = 300
		};

		// Act
		window.Show();

		// The scroller decides on arrow visibility from the previous layout pass.
		window.UpdateLayout();

		Dispatcher.UIThread.RunJobs();

		window.UpdateLayout();

		RippleEffect[] ripples = [.. tabControl
			.GetVisualDescendants()
			.OfType<Button>()
			.Where(button => button.Name is "PART_PageUpButton" or "PART_PageDownButton")
			.SelectMany(button => button.GetVisualDescendants().OfType<RippleEffect>())];

		// Assert
		ripples
			.Should()
			.NotBeEmpty();

		ripples
			.Should()
			.AllSatisfy(ripple => ripple
				.RippleFill
				.Should()
				.NotBeNull());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Parses every markup file of the application project.
	/// </summary>
	private static XDocument[] EnumerateProjectMarkup()
	{
		string root = Path.Combine(LocateRepositoryRoot(), "DataOrganizer");

		return [.. Directory
			.EnumerateFiles(root, "*.axaml", SearchOption.AllDirectories)
			.Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			.Select(XDocument.Load)];
	}

	/// <summary>
	/// Colour the snackbar styles assign to the given level.
	/// </summary>
	private static Color ExpectedColor(SnackbarMessageLevel level)
	{
		return level switch
		{
			SnackbarMessageLevel.Warning => Colors.Orange,
			SnackbarMessageLevel.Error => ResourceColor("WarningBrush"),
			_ => ResourceColor("MaterialBodyBrush")
		};
	}

	/// <summary>
	/// Walks up from the test output directory to the folder containing Directory.Build.props.
	/// </summary>
	private static string LocateRepositoryRoot()
	{
		DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);

		while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
		{
			directory = directory.Parent;
		}

		return directory?.FullName
			?? throw new DirectoryNotFoundException("Could not locate the repository root (Directory.Build.props not found).");
	}

	/// <summary>
	/// Resolves a brush resource of the running application and returns its colour.
	/// </summary>
	private static Color ResourceColor(string key)
	{
		Application application = Application.Current!;

		application
			.TryGetResource(key, application.ActualThemeVariant, out object? resource)
			.Should()
			.BeTrue();

		return resource
			.Should()
			.BeAssignableTo<ISolidColorBrush>()
			.Which
			.Color;
	}
	#endregion
}
