using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using System.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that the buttons Material puts into a text box have tips of the app, not the English ones of their themes")]
internal class TextBoxButtonTipTests
{
	#region Data
	/// <summary>
	/// Text of a box, which the clear button can clear.
	/// </summary>
	private const string BoxText = "Text";

	/// <summary>
	/// Style class that shows the clear button of a text box.
	/// </summary>
	private const string ClearButtonClass = "clearButton";

	/// <summary>
	/// Name of the clear button in the template of a text box.
	/// </summary>
	private const string ClearButtonName = "PART_ClearButton";

	/// <summary>
	/// Style class that shows the toggle revealing the content of a text box.
	/// </summary>
	private const string RevealToggleClass = "revealPasswordButton";

	/// <summary>
	/// Name of the toggle revealing the content in the template of a text box.
	/// </summary>
	private const string RevealToggleName = "PART_PasswordRevealToggle";
	#endregion

	#region Methods
	/// <summary>
	/// The clear button of a text box has another tip than the one its theme gives it, whether the box is shown with its
	/// text or the text is typed into the box later.
	/// </summary>
	[AvaloniaTest]
	public void Clear_Button_Has_Another_Tip_Than_Its_Theme([Values] bool isTypedLater)
	{
		// Arrange
		TextBox textBox = new()
		{
			Classes =
			{
				ClearButtonClass
			}
		};

		// The theme builds the clear button only while the box has text to clear.
		if (!isTypedLater)
		{
			textBox.Text = BoxText;
		}

		StackPanel panel = new()
		{
			Children =
			{
				textBox
			}
		};

		Show(panel);

		// Text typed into a shown box makes the theme build the button then, apart from the template of the box.
		if (isTypedLater)
		{
			textBox.Text = BoxText;

			Dispatcher.UIThread.RunJobs();
		}

		Button clearButton = FindPart<Button>(textBox, ClearButtonName);

		Button themed = new()
		{
			Theme = clearButton.Theme
		};

		panel.Children.Add(themed);

		Dispatcher.UIThread.RunJobs();

		// Act
		object? tip = ToolTip.GetTip(clearButton);

		// Assert
		tip
			.Should()
			.NotBeNull()
			.And
			.NotBe(ToolTip.GetTip(themed));
	}

	/// <summary>
	/// The toggle that reveals the content of a text box has another tip than the one its theme gives it, whether the
	/// content is shown or hidden, and whether the box is shown with the toggle or gets it later.
	/// </summary>
	[AvaloniaTest]
	public void Reveal_Toggle_Has_Another_Tip_Than_Its_Theme([Values] bool isRevealed, [Values] bool isMaskedLater)
	{
		// Arrange
		TextBox textBox = new();

		if (!isMaskedLater)
		{
			textBox.Classes.Add(RevealToggleClass);
		}

		StackPanel panel = new()
		{
			Children =
			{
				textBox
			}
		};

		Show(panel);

		// A class bound to a state, as in the dialog of a key and a value, may come after the box is shown, and the
		// theme then builds the toggle apart from the template of the box.
		if (isMaskedLater)
		{
			textBox.Classes.Add(RevealToggleClass);

			Dispatcher.UIThread.RunJobs();
		}

		ToggleButton toggle = FindPart<ToggleButton>(textBox, RevealToggleName);

		toggle.IsChecked = isRevealed;

		ToggleButton themed = new()
		{
			IsChecked = isRevealed,
			Theme = toggle.Theme
		};

		panel.Children.Add(themed);

		Dispatcher.UIThread.RunJobs();

		// Act
		object? tip = ToolTip.GetTip(toggle);

		// Assert
		tip
			.Should()
			.NotBeNull()
			.And
			.NotBe(ToolTip.GetTip(themed));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the part of the template of a text box that has the name.
	/// </summary>
	private static T FindPart<T>(TextBox textBox, string name) where T : Control
	{
		return textBox
			.GetVisualDescendants()
			.OfType<T>()
			.Single(x => x.Name == name);
	}

	/// <summary>
	/// Shows the content in a window and lets the layout settle.
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
