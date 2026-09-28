using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Views;
using System.Linq;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(SyntaxLanguageSelector)}"" type")]
internal class SyntaxLanguageSelectorTests
{
	#region Data
	/// <summary>
	/// Name of the button with the language of the text in the markup.
	/// </summary>
	private const string CurrentLanguageName = "CurrentLanguage";

	/// <summary>
	/// Name of the list of the languages in the markup.
	/// </summary>
	private const string LanguagesListName = "LanguagesList";

	/// <summary>
	/// Language of PowerShell scripts.
	/// </summary>
	private const string PowerShellLanguage = "powershell";

	/// <summary>
	/// Name of the search box of the list in the markup.
	/// </summary>
	private const string SearchInputName = "SearchInput";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SyntaxLanguageSelector.DefaultSyntaxLanguage" />: the list marks the language that the text takes by default,
	/// plain text included.
	/// </summary>
	[AvaloniaTest]
	[TestCase(PowerShellLanguage)]
	[TestCase(null)]
	public void DefaultSyntaxLanguage_Marks_Its_Language_In_The_Flyout(string? language)
	{
		// Arrange
		SyntaxLanguageSelector sut = new()
		{
			DefaultSyntaxLanguage = language,
			SyntaxLanguage = language
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		ListBox list = sut.GetControl<ListBox>(LanguagesListName);

		list.Items.Cast<SyntaxLanguageChoice>().Where(static x => x.IsDefault).Select(static x => x.Id)
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(language);

		// The list opens at the marked language, which is laid out with its neighbours.
		Control marked = list.ContainerFromIndex(list.SelectedIndex)!;

		GetShownTexts(marked)
			.Should()
			.HaveCount(2);

		GetShownTexts(list.GetRealizedContainers().First(x => x != marked))
			.Should()
			.ContainSingle();
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector" />: a search left in the list is gone when it opens again.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Clears_The_Search_When_Opened_Again()
	{
		// Arrange
		SyntaxLanguageSelector sut = new();

		Window window = Show(sut);

		Open(window, sut);

		sut.GetControl<TextBox>(SearchInputName).Text = "bat";

		Press(window, PhysicalKey.Escape);

		// Act
		Open(window, sut);

		// Assert
		sut.GetControl<TextBox>(SearchInputName).Text
			.Should()
			.BeNullOrEmpty();

		sut.GetControl<ListBox>(LanguagesListName).ItemCount
			.Should()
			.Be(SyntaxRegistry.Instance.Languages.Count + 1);
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector" />: the element that had the focus before the list opened gets it back once the list closes.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Gives_The_Focus_Back_When_Closed()
	{
		// Arrange
		SyntaxLanguageSelector sut = new();

		Window window = Show(sut);

		TextBox text = ((DockPanel)window.Content!).Children
			.OfType<TextBox>()
			.Single();

		text.Focus();

		Open(window, sut);

		// Act
		Press(window, PhysicalKey.Escape);

		// Assert
		text.IsFocused
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector.SyntaxLanguage" />: Escape closes the list and keeps the language of the text.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Keeps_The_Language_On_Escape()
	{
		// Arrange
		SyntaxLanguageSelector sut = new()
		{
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		Open(window, sut);

		Press(window, PhysicalKey.ArrowDown);

		// Act
		Press(window, PhysicalKey.Escape);

		// Assert
		sut.SyntaxLanguage
			.Should()
			.Be(PowerShellLanguage);

		IsOpen(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector" />: the list offers plain text first and then every language with a grammar.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Lists_Plain_Text_First_And_Then_Every_Language()
	{
		// Arrange
		SyntaxLanguageSelector sut = new();

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		string?[] expected = [null, .. SyntaxRegistry.Instance.Languages.Select(static x => x.Id)];

		sut.GetControl<ListBox>(LanguagesListName).Items.Cast<SyntaxLanguageChoice>().Select(static x => x.Id)
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector" />: the arrow keys move the selection of the list, which stops at its ends.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Moves_The_Selection_With_The_Arrow_Keys()
	{
		// Arrange
		SyntaxLanguageSelector sut = new();

		Window window = Show(sut);

		Open(window, sut);

		ListBox list = sut.GetControl<ListBox>(LanguagesListName);

		// Act
		Press(window, PhysicalKey.ArrowDown);

		Press(window, PhysicalKey.ArrowDown);

		Press(window, PhysicalKey.ArrowUp);

		int moved = list.SelectedIndex;

		Press(window, PhysicalKey.ArrowUp);

		Press(window, PhysicalKey.ArrowUp);

		// Assert
		moved
			.Should()
			.Be(1);

		list.SelectedIndex
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector.SyntaxLanguage" />: the list opens at the language of the text, scrolled into view.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Opens_At_The_Language_Of_The_Text()
	{
		// Arrange
		SyntaxLanguageSelector sut = new()
		{
			SyntaxLanguage = "yaml"
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		ListBox list = sut.GetControl<ListBox>(LanguagesListName);

		// A local keeps the assertion from being skipped by the null-conditional operator when nothing is selected.
		string? language = (list.SelectedItem as SyntaxLanguageChoice)?.Id;

		language
			.Should()
			.Be("yaml");

		list.ContainerFromIndex(list.SelectedIndex)
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector" />: the search box takes the focus once the list opens, so the keys search at once.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Puts_The_Focus_Into_The_Search()
	{
		// Arrange
		SyntaxLanguageSelector sut = new();

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		sut.GetControl<TextBox>(SearchInputName).IsFocused
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector.SyntaxLanguage" />: Enter over a search that finds nothing keeps the list open.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Stays_Open_On_Enter_Without_A_Match()
	{
		// Arrange
		SyntaxLanguageSelector sut = new()
		{
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		Open(window, sut);

		sut.GetControl<TextBox>(SearchInputName).Text = "no such language";

		// Act
		Press(window, PhysicalKey.Enter);

		// Assert
		IsOpen(sut)
			.Should()
			.BeTrue();

		sut.SyntaxLanguage
			.Should()
			.Be(PowerShellLanguage);
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector.SyntaxLanguage" />: a click on a language takes it and closes the list.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Takes_A_Clicked_Language()
	{
		// Arrange
		SyntaxLanguageSelector sut = new();

		Window window = Show(sut);

		Open(window, sut);

		ListBox list = sut.GetControl<ListBox>(LanguagesListName);

		Control row = list.ContainerFromItem(list.Items.Cast<SyntaxLanguageChoice>().Single(static x => x.Id == "bat"))!;

		// Act
		Click(window, row);

		// Assert
		sut.SyntaxLanguage
			.Should()
			.Be("bat");

		IsOpen(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector.SyntaxLanguage" />: Enter takes the selected language and closes the list.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Takes_The_Selected_Language_On_Enter()
	{
		// Arrange
		SyntaxLanguageSelector sut = new();

		Window window = Show(sut);

		Open(window, sut);

		sut.GetControl<TextBox>(SearchInputName).Text = "bat";

		// Act
		Press(window, PhysicalKey.Enter);

		// Assert
		sut.SyntaxLanguage
			.Should()
			.Be("bat");

		IsOpen(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector" />: the search finds a language by an extension of its files, with or without the dot.
	/// </summary>
	[AvaloniaTest]
	public void Search_Finds_A_Language_By_An_Extension([Values("ps1", ".ps1", "PS1")] string search)
	{
		// Arrange
		SyntaxLanguageSelector sut = new();

		Window window = Show(sut);

		Open(window, sut);

		// Act
		sut.GetControl<TextBox>(SearchInputName).Text = search;

		// Assert
		GetNames(sut.GetControl<ListBox>(LanguagesListName))
			.Should()
			.Contain("PowerShell")
			.And
			.NotContain("Batch");
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector" />: the search finds a language by any part of its name, whatever its case.
	/// </summary>
	[AvaloniaTest]
	public void Search_Finds_A_Language_By_Its_Name()
	{
		// Arrange
		SyntaxLanguageSelector sut = new();

		Window window = Show(sut);

		Open(window, sut);

		// Act
		sut.GetControl<TextBox>(SearchInputName).Text = "SHELL";

		// Assert
		GetNames(sut.GetControl<ListBox>(LanguagesListName))
			.Should()
			.Contain("PowerShell")
			.And
			.Contain("Shell Script")
			.And
			.NotContain("Batch");
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector" />: a whole extension comes first, then the start of a name, then the start
	/// of an extension, then a part of a name, and the best match is selected.
	/// </summary>
	[AvaloniaTest]
	public void Search_Puts_The_Best_Matches_First()
	{
		// Arrange
		SyntaxLanguageSelector sut = new();

		Window window = Show(sut);

		Open(window, sut);

		ListBox list = sut.GetControl<ListBox>(LanguagesListName);

		// Act
		sut.GetControl<TextBox>(SearchInputName).Text = "sh";

		// Assert
		// ".sh" of Shell Script, "ShaderLab", ".shtml" of HTML and "PowerShell".
		GetNames(list)
			.Should()
			.StartWith("Shell Script")
			.And
			.ContainInOrder("Shell Script", "ShaderLab", "HTML", "PowerShell");

		list.SelectedIndex
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="SyntaxLanguageSelector.SyntaxLanguage" />: the button shows the name of the language of the text.
	/// </summary>
	[AvaloniaTest]
	public void SyntaxLanguage_Shows_Its_Name()
	{
		// Arrange
		SyntaxLanguageSelector sut = new()
		{
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<Button>(CurrentLanguageName).Content
			.Should()
			.Be("PowerShell");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Clicks the middle of a control with the left button.
	/// </summary>
	private static void Click(Window window, Visual target)
	{
		Point point = target.TranslatePoint(
			new(
				target.Bounds.Width / 2.0,
				target.Bounds.Height / 2.0),
			window) ?? default;

		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();
	}

	/// <summary>
	/// Returns the names of the languages in the list.
	/// </summary>
	private static string[] GetNames(ListBox list) => [.. list.Items.Cast<SyntaxLanguageChoice>().Select(static x => x.Name)];

	/// <summary>
	/// Returns the texts shown in a row of the list.
	/// </summary>
	private static string?[] GetShownTexts(Control? row)
	{
		return [.. row?
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Where(static x => x.IsEffectivelyVisible)
			.Select(static x => x.Text) ?? []];
	}

	/// <summary>
	/// <c>True</c> while the list is open.
	/// </summary>
	private static bool IsOpen(SyntaxLanguageSelector selector) => selector.GetControl<Button>(CurrentLanguageName).Flyout!.IsOpen;

	/// <summary>
	/// Opens the list with a click on the button.
	/// </summary>
	private static void Open(Window window, SyntaxLanguageSelector selector) => Click(window, selector.GetControl<Button>(CurrentLanguageName));

	/// <summary>
	/// Presses and releases a key.
	/// </summary>
	private static void Press(Window window, PhysicalKey key)
	{
		window.KeyPressQwerty(key, RawInputModifiers.None);

		window.KeyReleaseQwerty(key, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();
	}

	/// <summary>
	/// Shows the selector at the bottom of a window of a fixed size, under a text box that can hold the focus,
	/// and lets the layout settle.
	/// </summary>
	private static Window Show(SyntaxLanguageSelector selector)
	{
		// The list opens above the selector, so it needs room there.
		DockPanel.SetDock(selector, Dock.Bottom);

		selector.HorizontalAlignment = HorizontalAlignment.Left;

		Window window = new()
		{
			Content = new DockPanel
			{
				Children =
				{
					selector,
					new TextBox()
				}
			},
			Height = 600.0,
			Width = 400.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion
}
