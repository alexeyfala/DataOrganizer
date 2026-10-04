using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Views;
using System.Linq;
using System.Text;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(ChoiceSelector)}"" type")]
internal class ChoiceSelectorTests
{
	#region Data
	/// <summary>
	/// Name of the panel of the list and its search in the markup.
	/// </summary>
	private const string ChoicesHostName = "ChoicesHost";

	/// <summary>
	/// Name of the list of the items in the markup.
	/// </summary>
	private const string ChoicesListName = "ChoicesList";

	/// <summary>
	/// Name of the button with the chosen item in the markup.
	/// </summary>
	private const string CurrentChoiceName = "CurrentChoice";

	/// <summary>
	/// Language of PowerShell scripts.
	/// </summary>
	private const string PowerShellLanguage = "powershell";

	/// <summary>
	/// Name of the search box of the list in the markup.
	/// </summary>
	private const string SearchInputName = "SearchInput";

	/// <summary>
	/// Tip over an item that cannot be chosen.
	/// </summary>
	private const string UnavailableTip = "Cannot be chosen";

	/// <summary>
	/// Languages of the syntax highlighting, plain text first, as the status bar offers them.
	/// </summary>
	private static readonly SelectorChoice[] Languages =
	[
		new SelectorChoice
		{
			Id = null,
			Name = "Plain Text",
			SearchTerms = []
		},
		.. SyntaxRegistry.Instance.Languages
	];

	/// <summary>
	/// The Unicode encodings, few enough for every row of the list to be laid out.
	/// </summary>
	private static readonly SelectorChoice[] UnicodeEncodings = [.. FileTextCodec.EncodingChoices.Take(5)];
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ChoiceSelector.Caption" />: the button shows the caption.
	/// </summary>
	[AvaloniaTest]
	public void Caption_Reaches_The_Button()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Caption = "PowerShell"
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<Button>(CurrentChoiceName).Content
			.Should()
			.Be("PowerShell");
	}

	/// <summary>
	/// <see cref="ChoiceSelector.DefaultChoice" />: the list marks the item taken when none is chosen, the one without an
	/// id included.
	/// </summary>
	[AvaloniaTest]
	[TestCase(PowerShellLanguage)]
	[TestCase(null)]
	public void DefaultChoice_Marks_Its_Item_In_The_Flyout(string? id)
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages,
			DefaultChoice = id,
			DefaultMark = "(by extension)",
			SelectedChoice = id
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

		list.Items.Cast<SelectorChoice>().Where(static x => x.IsDefault).Select(static x => x.Id)
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(id);

		// The list opens at the marked item, which is laid out with its neighbours.
		Control marked = list.ContainerFromIndex(list.SelectedIndex)!;

		GetShownTexts(marked)
			.Should()
			.HaveCount(2);

		GetShownTexts(list.GetRealizedContainers().First(x => x != marked))
			.Should()
			.ContainSingle();
	}

	/// <summary>
	/// <see cref="ChoiceSelector.FlyoutOpeningCommand" />: the command runs as the list opens, before the list takes the
	/// items that cannot be chosen, so the ones it finds are marked at once.
	/// </summary>
	[AvaloniaTest]
	public void FlyoutOpeningCommand_Runs_Before_The_List_Takes_Its_Items()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = UnicodeEncodings
		};

		sut.FlyoutOpeningCommand = new RelayCommand(() => sut.UnavailableChoices = [Encoding.UTF32.WebName]);

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		GetNames(sut.GetControl<ListBox>(ChoicesListName))
			.Should()
			.Equal("UTF-8", "UTF-16 LE", "UTF-16 BE", "UTF-32 BE", "UTF-32 LE");
	}

	/// <summary>
	/// <see cref="ChoiceSelector.FlyoutPlacement" />: the list takes the placement against the button.
	/// </summary>
	[AvaloniaTest]
	public void FlyoutPlacement_Reaches_The_Flyout()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			FlyoutPlacement = PlacementMode.TopEdgeAlignedRight
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		((PopupFlyoutBase)sut.GetControl<Button>(CurrentChoiceName).Flyout!).Placement
			.Should()
			.Be(PlacementMode.TopEdgeAlignedRight);
	}

	/// <summary>
	/// <see cref="ChoiceSelector.FlyoutWidth" />: the list takes the width.
	/// </summary>
	[AvaloniaTest]
	public void FlyoutWidth_Reaches_The_List()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages,
			FlyoutWidth = 360.0
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		sut.GetControl<DockPanel>(ChoicesHostName).Bounds.Width
			.Should()
			.Be(360.0);
	}

	/// <summary>
	/// <see cref="ChoiceSelector" />: a search left in the list is gone when it opens again.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Clears_The_Search_When_Opened_Again()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages
		};

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

		sut.GetControl<ListBox>(ChoicesListName).ItemCount
			.Should()
			.Be(Languages.Length);
	}

	/// <summary>
	/// <see cref="ChoiceSelector" />: the element that had the focus before the list opened gets it back once the list closes.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Gives_The_Focus_Back_When_Closed()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages
		};

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
	/// <see cref="ChoiceSelector.SelectedChoice" />: Escape closes the list and keeps the chosen item.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Keeps_The_Choice_On_Escape()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages,
			SelectedChoice = PowerShellLanguage
		};

		Window window = Show(sut);

		Open(window, sut);

		Press(window, PhysicalKey.ArrowDown);

		// Act
		Press(window, PhysicalKey.Escape);

		// Assert
		sut.SelectedChoice
			.Should()
			.Be(PowerShellLanguage);

		IsOpen(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ChoiceSelector.Choices" />: the list offers every item in its order.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Lists_Every_Choice_In_Its_Order()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		string?[] expected = [.. Languages.Select(static x => x.Id)];

		sut.GetControl<ListBox>(ChoicesListName).Items.Cast<SelectorChoice>().Select(static x => x.Id)
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="ChoiceSelector" />: the arrow keys move the selection of the list, which stops at its ends.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Moves_The_Selection_With_The_Arrow_Keys()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages
		};

		Window window = Show(sut);

		Open(window, sut);

		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

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
	/// <see cref="ChoiceSelector.SelectedChoice" />: the list opens at the chosen item, scrolled into view.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Opens_At_The_Chosen_Item()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages,
			SelectedChoice = "yaml"
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

		// A local keeps the assertion from being skipped by the null-conditional operator when nothing is selected.
		string? id = (list.SelectedItem as SelectorChoice)?.Id;

		id
			.Should()
			.Be("yaml");

		list.ContainerFromIndex(list.SelectedIndex)
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="ChoiceSelector" />: the search box takes the focus once the list opens, so the keys search at once.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Puts_The_Focus_Into_The_Search()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		sut.GetControl<TextBox>(SearchInputName).IsFocused
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SelectorChoice.Description" />: a row of the list shows the description of its item after the name.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Shows_The_Description_After_The_Name()
	{
		// Arrange
		SelectorChoice choice = FileTextCodec
			.EncodingChoices
			.Single(static x => x.Id == "cp866");

		ChoiceSelector sut = new()
		{
			Choices = FileTextCodec.EncodingChoices,
			SelectedChoice = choice.Id
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

		GetShownTexts(list.ContainerFromIndex(list.SelectedIndex))
			.Should()
			.Equal(choice.Name, choice.Description);
	}

	/// <summary>
	/// <see cref="ChoiceSelector.SelectedChoice" />: Enter over a search that finds nothing keeps the list open.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Stays_Open_On_Enter_Without_A_Match()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages,
			SelectedChoice = PowerShellLanguage
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

		sut.SelectedChoice
			.Should()
			.Be(PowerShellLanguage);
	}

	/// <summary>
	/// <see cref="ChoiceSelector.SelectedChoice" />: a click on an item takes it and closes the list.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Takes_A_Clicked_Item()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages
		};

		Window window = Show(sut);

		Open(window, sut);

		Control row = GetRow(sut.GetControl<ListBox>(ChoicesListName), "bat");

		// Act
		Click(window, row);

		// Assert
		sut.SelectedChoice
			.Should()
			.Be("bat");

		IsOpen(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ChoiceSelector.SelectedChoice" />: Enter takes the selected item and closes the list.
	/// </summary>
	[AvaloniaTest]
	public void Flyout_Takes_The_Selected_Item_On_Enter()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages
		};

		Window window = Show(sut);

		Open(window, sut);

		sut.GetControl<TextBox>(SearchInputName).Text = "bat";

		// Act
		Press(window, PhysicalKey.Enter);

		// Assert
		sut.SelectedChoice
			.Should()
			.Be("bat");

		IsOpen(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ChoiceSelector" />: the search finds an encoding by its code page and puts it first.
	/// </summary>
	[AvaloniaTest]
	public void Search_Finds_An_Encoding_By_Its_Code_Page()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = FileTextCodec.EncodingChoices
		};

		Window window = Show(sut);

		Open(window, sut);

		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

		// Act
		sut.GetControl<TextBox>(SearchInputName).Text = "866";

		// Assert
		// A local keeps the assertion from being skipped by the null-conditional operator when nothing is selected.
		string? id = (list.SelectedItem as SelectorChoice)?.Id;

		id
			.Should()
			.Be("cp866");
	}

	/// <summary>
	/// <see cref="ChoiceSelector" />: the search finds an item by a word of its search terms, with or without the dot of an
	/// extension, whatever its case.
	/// </summary>
	[AvaloniaTest]
	public void Search_Finds_An_Item_By_A_Search_Term([Values("ps1", ".ps1", "PS1")] string search)
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages
		};

		Window window = Show(sut);

		Open(window, sut);

		// Act
		sut.GetControl<TextBox>(SearchInputName).Text = search;

		// Assert
		GetNames(sut.GetControl<ListBox>(ChoicesListName))
			.Should()
			.Contain("PowerShell")
			.And
			.NotContain("Batch");
	}

	/// <summary>
	/// <see cref="ChoiceSelector" />: the search finds an item by the start or any other part of its description.
	/// </summary>
	[AvaloniaTest]
	public void Search_Finds_An_Item_By_Its_Description()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = FileTextCodec.EncodingChoices
		};

		Window window = Show(sut);

		Open(window, sut);

		// Act
		sut.GetControl<TextBox>(SearchInputName).Text = "cyrillic";

		// Assert
		// "Cyrillic (DOS)" starts with the search, and "OEM Cyrillic" holds it further on.
		GetNames(sut.GetControl<ListBox>(ChoicesListName))
			.Should()
			.Contain("CP866")
			.And
			.Contain("IBM855")
			.And
			.NotContain("UTF-8");
	}

	/// <summary>
	/// <see cref="ChoiceSelector" />: the search finds an item by any part of its name, whatever its case.
	/// </summary>
	[AvaloniaTest]
	public void Search_Finds_An_Item_By_Its_Name()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages
		};

		Window window = Show(sut);

		Open(window, sut);

		// Act
		sut.GetControl<TextBox>(SearchInputName).Text = "SHELL";

		// Assert
		GetNames(sut.GetControl<ListBox>(ChoicesListName))
			.Should()
			.Contain("PowerShell")
			.And
			.Contain("Shell Script")
			.And
			.NotContain("Batch");
	}

	/// <summary>
	/// <see cref="ChoiceSelector" />: a whole search term comes first, then the start of a name, then the start of a search
	/// term, then a part of a name, and the best match is selected.
	/// </summary>
	[AvaloniaTest]
	public void Search_Puts_The_Best_Matches_First()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages
		};

		Window window = Show(sut);

		Open(window, sut);

		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

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
	/// <see cref="ChoiceSelector.UnavailableChoices" />: the rows of the items that cannot be chosen are disabled, which
	/// grays them out, and carry the tip, while the other rows have none.
	/// </summary>
	[AvaloniaTest]
	public void UnavailableChoices_Are_Disabled_With_Their_Tip()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = UnicodeEncodings,
			UnavailableChoices = [Encoding.UTF32.WebName],
			UnavailableTip = UnavailableTip
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

		Control unavailable = GetRow(list, Encoding.UTF32.WebName);

		Control available = GetRow(list, Encoding.UTF8.WebName);

		unavailable.IsEffectivelyEnabled
			.Should()
			.BeFalse();

		ToolTip.GetTip(unavailable)
			.Should()
			.Be(UnavailableTip);

		available.IsEffectivelyEnabled
			.Should()
			.BeTrue();

		ToolTip.GetTip(available)
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ChoiceSelector.UnavailableChoices" />: a click on an item that cannot be chosen takes nothing and keeps
	/// the list open.
	/// </summary>
	[AvaloniaTest]
	public void UnavailableChoices_Are_Not_Taken_By_A_Click()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = UnicodeEncodings,
			SelectedChoice = Encoding.UTF8.WebName,
			UnavailableChoices = [Encoding.UTF32.WebName]
		};

		Window window = Show(sut);

		Open(window, sut);

		Control row = GetRow(sut.GetControl<ListBox>(ChoicesListName), Encoding.UTF32.WebName);

		// Act
		Click(window, row);

		// Assert
		sut.SelectedChoice
			.Should()
			.Be(Encoding.UTF8.WebName);

		IsOpen(sut)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ChoiceSelector.UnavailableChoices" />: Enter does not take an item that cannot be chosen, even when it
	/// is selected.
	/// </summary>
	[AvaloniaTest]
	public void UnavailableChoices_Are_Not_Taken_By_Enter()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = UnicodeEncodings,
			SelectedChoice = Encoding.UTF8.WebName,
			UnavailableChoices = [Encoding.UTF32.WebName]
		};

		Window window = Show(sut);

		Open(window, sut);

		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

		list.SelectedItem = list.Items.Cast<SelectorChoice>().Single(static x => x.Id == Encoding.UTF32.WebName);

		// Act
		Press(window, PhysicalKey.Enter);

		// Assert
		sut.SelectedChoice
			.Should()
			.Be(Encoding.UTF8.WebName);

		IsOpen(sut)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ChoiceSelector.UnavailableChoices" />: the items that cannot be chosen come after the others, each part
	/// in its order.
	/// </summary>
	[AvaloniaTest]
	public void UnavailableChoices_Come_Last_In_The_List()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = UnicodeEncodings,
			UnavailableChoices = [Encoding.Unicode.WebName, Encoding.UTF32.WebName]
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		GetNames(sut.GetControl<ListBox>(ChoicesListName))
			.Should()
			.Equal("UTF-8", "UTF-16 BE", "UTF-32 BE", "UTF-16 LE", "UTF-32 LE");
	}

	/// <summary>
	/// <see cref="ChoiceSelector.UnavailableChoices" />: the items that cannot be chosen come after the other matches of a
	/// search, whatever their rank, and the best of the others is selected.
	/// </summary>
	[AvaloniaTest]
	public void UnavailableChoices_Come_Last_In_The_Search_Results()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages,
			UnavailableChoices = ["shellscript"]
		};

		Window window = Show(sut);

		Open(window, sut);

		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

		// Act
		sut.GetControl<TextBox>(SearchInputName).Text = "sh";

		// Assert
		// ".sh" of Shell Script matches best, yet it cannot be chosen.
		GetNames(list)
			.Should()
			.StartWith("ShaderLab")
			.And
			.EndWith("Shell Script");

		list.SelectedIndex
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="ChoiceSelector.UnavailableChoices" />: the chosen item stays available in its place, as taking it again
	/// changes nothing, and the list opens at it.
	/// </summary>
	[AvaloniaTest]
	public void UnavailableChoices_Keep_The_Chosen_Item_Available()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = UnicodeEncodings,
			SelectedChoice = Encoding.Unicode.WebName,
			UnavailableChoices = [Encoding.Unicode.WebName]
		};

		Window window = Show(sut);

		// Act
		Open(window, sut);

		// Assert
		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

		GetNames(list)
			.Should()
			.Equal("UTF-8", "UTF-16 LE", "UTF-16 BE", "UTF-32 LE", "UTF-32 BE");

		list.SelectedIndex
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="ChoiceSelector.UnavailableChoices" />: a search that finds only items that cannot be chosen selects
	/// none, and Enter keeps the list open and the choice as it was.
	/// </summary>
	[AvaloniaTest]
	public void UnavailableChoices_Leave_Nothing_To_Take_On_Enter()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = Languages,
			SelectedChoice = "bat",
			UnavailableChoices = [PowerShellLanguage]
		};

		Window window = Show(sut);

		Open(window, sut);

		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

		sut.GetControl<TextBox>(SearchInputName).Text = "ps1";

		// Act
		Press(window, PhysicalKey.Enter);

		// Assert
		GetNames(list)
			.Should()
			.Equal("PowerShell");

		list.SelectedItem
			.Should()
			.BeNull();

		sut.SelectedChoice
			.Should()
			.Be("bat");

		IsOpen(sut)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ChoiceSelector.UnavailableChoices" />: the tip of an item that cannot be chosen opens when the pointer
	/// rests on it, though the item is disabled.
	/// </summary>
	[AvaloniaTest]
	public void UnavailableChoices_Show_Their_Tip_On_Hover()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = UnicodeEncodings,
			UnavailableChoices = [Encoding.UTF32.WebName],
			UnavailableTip = UnavailableTip
		};

		Window window = Show(sut);

		Open(window, sut);

		Control row = GetRow(sut.GetControl<ListBox>(ChoicesListName), Encoding.UTF32.WebName);

		// The tip opens at once rather than after the delay of the theme.
		ToolTip.SetShowDelay(row, 0);

		// Act
		Hover(window, row);

		// Assert
		ToolTip.GetIsOpen(row)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ChoiceSelector.UnavailableChoices" />: the arrow keys stop at the last item that can be chosen, before
	/// the ones that cannot.
	/// </summary>
	[AvaloniaTest]
	public void UnavailableChoices_Stop_The_Arrow_Keys()
	{
		// Arrange
		ChoiceSelector sut = new()
		{
			Choices = UnicodeEncodings,
			UnavailableChoices = [Encoding.Unicode.WebName]
		};

		Window window = Show(sut);

		Open(window, sut);

		ListBox list = sut.GetControl<ListBox>(ChoicesListName);

		// Act
		for (int i = 0; i < UnicodeEncodings.Length; i++)
		{
			Press(window, PhysicalKey.ArrowDown);
		}

		// Assert
		// UTF-16 LE goes last, so the selection stops at UTF-32 BE before it.
		list.SelectedIndex
			.Should()
			.Be(UnicodeEncodings.Length - 2);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the middle of a control in the coordinates of the window.
	/// </summary>
	private static Point Center(Window window, Visual target)
	{
		return target.TranslatePoint(
			new(
				target.Bounds.Width / 2.0,
				target.Bounds.Height / 2.0),
			window) ?? default;
	}

	/// <summary>
	/// Clicks the middle of a control with the left button.
	/// </summary>
	private static void Click(Window window, Visual target)
	{
		Point point = Center(window, target);

		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();
	}

	/// <summary>
	/// Returns the names of the items in the list.
	/// </summary>
	private static string[] GetNames(ListBox list) => [.. list.Items.Cast<SelectorChoice>().Select(static x => x.Name)];

	/// <summary>
	/// Returns the row of the list that shows an item.
	/// </summary>
	private static Control GetRow(ListBox list, string? id)
	{
		SelectorChoice choice = list
			.Items
			.Cast<SelectorChoice>()
			.Single(x => x.Id == id);

		return list.ContainerFromItem(choice)!;
	}

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
	/// Rests the pointer on the middle of a control.
	/// </summary>
	private static void Hover(Window window, Visual target)
	{
		window.MouseMove(Center(window, target));

		Dispatcher.UIThread.RunJobs();
	}

	/// <summary>
	/// <c>True</c> while the list is open.
	/// </summary>
	private static bool IsOpen(ChoiceSelector selector) => selector.GetControl<Button>(CurrentChoiceName).Flyout!.IsOpen;

	/// <summary>
	/// Opens the list with a click on the button.
	/// </summary>
	private static void Open(Window window, ChoiceSelector selector) => Click(window, selector.GetControl<Button>(CurrentChoiceName));

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
	private static Window Show(ChoiceSelector selector)
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
