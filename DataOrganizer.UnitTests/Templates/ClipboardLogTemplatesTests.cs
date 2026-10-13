using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Text;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;
using System.Linq;

namespace DataOrganizer.UnitTests.Templates;

[TestFixture(Description = @"Tests of ""ClipboardLogTemplates.axaml"" resources")]
internal class ClipboardLogTemplatesTests
{
	#region Data
	/// <summary>
	/// Key of the template of an entry of the clipboard log.
	/// </summary>
	private const string EntryTemplateKey = "ClipboardEntryItemTemplate";
	#endregion

	#region Methods
	/// <summary>
	/// <c>ClipboardEntryItemTemplate</c>: a link shows its text, as a plain text does.
	/// </summary>
	[AvaloniaTest]
	public void ClipboardEntryItemTemplate_Shows_The_Text_Of_A_Link()
	{
		// Arrange
		ClipboardUrlEntry link = ClipboardEntryFactory.CreateUrlEntry("https://example.com/page");

		ContentControl host = new()
		{
			Content = link,
			ContentTemplate = (IDataTemplate)Application.Current!.FindResource(EntryTemplateKey)!
		};

		Window window = new()
		{
			Content = host
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		string?[] shownTexts = [.. host
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.SelectMany(static x => Interaction.GetBehaviors(x).OfType<SearchHighlightBehavior>())
			.Select(static x => x.SourceText)];

		shownTexts
			.Should()
			.Equal("https://example.com/page");
	}
	#endregion
}
