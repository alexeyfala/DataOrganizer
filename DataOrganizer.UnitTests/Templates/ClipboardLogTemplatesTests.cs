using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Text;
using DataOrganizer.Helpers.Views;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Templates;

[TestFixture(Description = @"Tests of ""ClipboardLogTemplates.axaml"" resources")]
internal class ClipboardLogTemplatesTests
{
	#region Data
	/// <summary>
	/// Name of the badge that marks the kind of an entry.
	/// </summary>
	private const string BadgeName = "TypeGlyphBadge";

	/// <summary>
	/// Key of the template of an entry of the clipboard log.
	/// </summary>
	private const string EntryTemplateKey = "ClipboardEntryItemTemplate";

	/// <summary>
	/// Key of the template of the contents of a files entry.
	/// </summary>
	private const string FilesTemplateKey = "ClipboardFilesEntryTemplate";
	#endregion

	#region Methods
	/// <summary>
	/// <c>ClipboardEntryItemTemplate</c>: the tooltip of the entry of an image tells its size.
	/// </summary>
	[AvaloniaTest]
	public void ClipboardEntryItemTemplate_Puts_The_Content_ToolTip_On_The_Entry()
	{
		// Arrange
		ClipboardImageEntry image = ClipboardEntryFactory.CreateImageEntry(ImageFactory.CreatePng());

		ContentControl host = new()
		{
			Content = image,
			ContentTemplate = (IDataTemplate)Application.Current!.FindResource(EntryTemplateKey)!
		};

		Window window = new()
		{
			Content = host
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert (the root of the template is the button of the entry).
		Button button = host
			.GetVisualDescendants()
			.OfType<Button>()
			.First();

		ToolTip.GetTip(button)
			.Should()
			.Be("1 × 1");
	}

	/// <summary>
	/// <c>ClipboardEntryItemTemplate</c>: the tooltip of the badge counts the folders, the files and all items of a list.
	/// </summary>
	[AvaloniaTest]
	public void ClipboardEntryItemTemplate_Puts_The_Type_ToolTip_On_The_Badge()
	{
		// Arrange
		ClipboardFilesEntry files = ClipboardEntryFactory.CreateFilesEntry(ClipboardEntryFactory.CreateFiles(2));

		ContentControl host = new()
		{
			Content = files,
			ContentTemplate = (IDataTemplate)Application.Current!.FindResource(EntryTemplateKey)!
		};

		Window window = new()
		{
			Content = host
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert (a line per kind: folders, files, and the total).
		Border badge = host
			.GetVisualDescendants()
			.OfType<Border>()
			.Single(static x => x.Name == BadgeName);

		string[] lines = ((string)ToolTip.GetTip(badge)!).Split(Environment.NewLine);

		lines
			.Should()
			.HaveCount(3);

		lines[^1]
			.Should()
			.Be("Σ: 2");
	}

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

	/// <summary>
	/// <c>ClipboardEntryItemTemplate</c>: the badge shows the glyph of the kind of the entry.
	/// </summary>
	[AvaloniaTest]
	public void ClipboardEntryItemTemplate_Shows_The_Type_Glyph_On_The_Badge()
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
		Border badge = host
			.GetVisualDescendants()
			.OfType<Border>()
			.Single(static x => x.Name == BadgeName);

		((TextBlock)badge.Child!).Text
			.Should()
			.Be(Glyphs.Link);
	}

	/// <summary>
	/// <c>ClipboardFilesEntryTemplate</c>: the entry of a list of files shows the counts and a line per file.
	/// </summary>
	[AvaloniaTest]
	public void ClipboardFilesEntryTemplate_Shows_The_Preview_Of_The_List()
	{
		// Arrange
		ClipboardFilesEntry files = ClipboardEntryFactory.CreateFilesEntry(ClipboardEntryFactory.CreateFiles(2));

		ContentControl host = new()
		{
			Content = files,
			ContentTemplate = (IDataTemplate)Application.Current!.FindResource(FilesTemplateKey)!
		};

		Window window = new()
		{
			Content = host
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert (the counts and both files).
		string[] lines = host
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Single()
			.Text!
			.Split(Environment.NewLine);

		lines
			.Should()
			.HaveCount(3);

		lines[^1]
			.Should()
			.EndWith("file1.txt");
	}
	#endregion
}
