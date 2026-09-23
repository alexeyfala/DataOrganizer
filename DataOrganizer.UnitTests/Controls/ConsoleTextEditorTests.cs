using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Styling;
using DataOrganizer.Controls;
using DataOrganizer.Helpers.Text;
using Serilog.Events;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(ConsoleTextEditor)}"" type")]
internal class ConsoleTextEditorTests
{
	#region Data
	/// <summary>
	/// Name of the scroll viewer in the template of the text editor.
	/// </summary>
	private const string ScrollViewerName = "PART_ScrollViewer";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ConsoleTextEditor" />: text appended to the document brings the end of the text into view.
	/// </summary>
	[AvaloniaTest]
	public void Follows_The_End_When_Text_Is_Appended()
	{
		// Arrange
		TextDocument document = new();

		ConsoleTextEditor sut = new()
		{
			Document = document
		};

		Show(sut);

		// Act
		document.Insert(
			document.TextLength,
			string.Join('\n', Enumerable
				.Range(1, 1000)
				.Select(static x => $"Line {x:D4}")));

		Dispatcher.UIThread.RunJobs();

		// Assert
		ScrollViewer scrollViewer = GetScrollViewer(sut);

		scrollViewer.Offset.Y
			.Should()
			.Be(scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
	}

	/// <summary>
	/// <see cref="ConsoleTextEditor" />: every log level has a colorizer of its own.
	/// </summary>
	[AvaloniaTest]
	public void Paints_Every_Log_Level()
	{
		// Act
		ConsoleTextEditor sut = new();

		// Assert
		sut.TextArea.TextView.LineTransformers
			.OfType<WordOccurrenceColorizer>()
			.Should()
			.HaveCount(Enum.GetValues<LogEventLevel>().Length);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the scroll viewer of the editor.
	/// </summary>
	private static ScrollViewer GetScrollViewer(ConsoleTextEditor editor)
	{
		return editor
			.GetVisualDescendants()
			.OfType<ScrollViewer>()
			.First(static x => x.Name == ScrollViewerName);
	}

	/// <summary>
	/// Shows the editor in its theme in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static void Show(ConsoleTextEditor editor)
	{
		Interaction
			.GetBehaviors(editor)
			.Add(new FluentThemeBehavior());

		Window window = new()
		{
			Content = editor,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();
	}
	#endregion
}
