using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using Shared.Properties;
using System;
using System.Linq;

namespace DataOrganizer.Views;

/// <summary>
/// Language of a text for the syntax highlighting, with a list to choose another one from, searched by name and extension.
/// </summary>
internal sealed partial class SyntaxLanguageSelector : UserControl
{
	#region Properties
	/// <summary>
	/// Language that the text takes when none is chosen; <c>null</c> for plain text.
	/// </summary>
	public string? DefaultSyntaxLanguage
	{
		get => GetValue(DefaultSyntaxLanguageProperty);
		set => SetValue(DefaultSyntaxLanguageProperty, value);
	}

	/// <summary>
	/// Language of the text for the syntax highlighting; <c>null</c> for plain text.
	/// </summary>
	public string? SyntaxLanguage
	{
		get => GetValue(SyntaxLanguageProperty);
		set => SetValue(SyntaxLanguageProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="DefaultSyntaxLanguage" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> DefaultSyntaxLanguageProperty = AvaloniaProperty
		.Register<SyntaxLanguageSelector, string?>(name: nameof(DefaultSyntaxLanguage));

	/// <summary>
	/// Identifies the <see cref="SyntaxLanguage" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> SyntaxLanguageProperty = AvaloniaProperty
		.Register<SyntaxLanguageSelector, string?>(name: nameof(SyntaxLanguage));
	#endregion

	#region Data
	/// <summary>
	/// List of the languages that the button opens.
	/// </summary>
	private readonly PopupFlyoutBase _flyout;

	/// <summary>
	/// Languages of the list, plain text first, as they were when it opened.
	/// </summary>
	private SyntaxLanguageChoice[] _choices = [];

	/// <summary>
	/// Element that had the focus before the list opened.
	/// </summary>
	private IInputElement? _focusedElement;
	#endregion

	#region Constructors
	public SyntaxLanguageSelector()
	{
		InitializeComponent();

		// The markup gives the flyout no field of its own.
		_flyout = (PopupFlyoutBase)CurrentLanguage.Flyout!;

		_flyout.Opening += Flyout_Opening;

		_flyout.Closed += Flyout_Closed;

		// The keys work in the search box and in the list alike, and the arrows do not move the caret of the search.
		ChoicesHost.AddHandler(
			KeyDownEvent,
			ChoicesHost_KeyDown,
			RoutingStrategies.Tunnel);

		LanguagesList.Tapped += LanguagesList_Tapped;

		SearchInput
			.GetObservable(TextBox.TextProperty)
			.Subscribe(TextProperty_Changed);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.KeyDownEvent" /> tunnel handler of <see cref="ChoicesHost" />.
	/// </summary>
	private void ChoicesHost_KeyDown(object? sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
			case Key.Down:
				MoveSelection(1);
				break;

			case Key.Up:
				MoveSelection(-1);
				break;

			case Key.Enter:
				Choose(LanguagesList.SelectedItem as SyntaxLanguageChoice);
				break;

			default:
				return;
		}

		e.Handled = true;
	}

	/// <summary>
	/// <see cref="FlyoutBase.Closed" /> event handler of the list of the languages.
	/// </summary>
	private void Flyout_Closed(object? sender, EventArgs e)
	{
		// The search box took the focus from the text, which gets it back.
		_focusedElement?.Focus();

		_focusedElement = null;
	}

	/// <summary>
	/// <see cref="PopupFlyoutBase.Opening" /> event handler of the list of the languages.
	/// </summary>
	private void Flyout_Opening(object? sender, EventArgs e)
	{
		_focusedElement = TopLevel
			.GetTopLevel(this)?
			.FocusManager?
			.GetFocusedElement();

		string? defaultLanguage = DefaultSyntaxLanguage;

		_choices =
		[
			new SyntaxLanguageChoice
			{
				Extensions = [],
				Id = null,
				IsDefault = defaultLanguage is null,
				Name = Strings.PlainText
			},
			.. SyntaxRegistry
				.Instance
				.Languages
				.Select(x => x with
				{
					IsDefault = x.Id == defaultLanguage
				})
		];

		// A search left from the last time would hide the other languages.
		SearchInput.Text = null;

		ShowChoices();
	}

	/// <summary>
	/// <see cref="InputElement.Tapped" /> event handler of <see cref="LanguagesList" />.
	/// </summary>
	private void LanguagesList_Tapped(object? sender, TappedEventArgs e)
	{
		// A tap on the scroll bar of the list takes no language.
		if ((e.Source as Visual)?
			.FindAncestorOfType<ListBoxItem>(includeSelf: true) is not { DataContext: SyntaxLanguageChoice choice })
		{
			return;
		}

		Choose(choice);
	}

	/// <summary>
	/// <see cref="TextBox.TextProperty" /> changed handler of <see cref="SearchInput" />.
	/// </summary>
	private void TextProperty_Changed(string? value) => ShowChoices();
	#endregion

	#region Helpers
	/// <summary>
	/// Returns how well a language matches the search, where a lower rank is better; <c>null</c> when it does not match.
	/// </summary>
	private static int? Rank(SyntaxLanguageChoice choice, string search)
	{
		if (choice
			.Extensions
			.Any(x => x.AsSpan().TrimStart('.').Equals(search, StringComparison.OrdinalIgnoreCase)))
		{
			return 0;
		}

		if (choice
			.Name
			.StartsWith(search, StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}

		if (choice
			.Extensions
			.Any(x => x.AsSpan().TrimStart('.').StartsWith(search, StringComparison.OrdinalIgnoreCase)))
		{
			return 2;
		}

		return choice
			.Name
			.Contains(search, StringComparison.OrdinalIgnoreCase) ? 3 : null;
	}

	/// <summary>
	/// Takes the language for the text and closes the list.
	/// </summary>
	private void Choose(SyntaxLanguageChoice? choice)
	{
		// Enter over an empty search result keeps the list open.
		if (choice is null)
		{
			return;
		}

		SetCurrentValue(SyntaxLanguageProperty, choice.Id);

		_flyout.Hide();
	}

	/// <summary>
	/// Moves the selection of the list by a number of languages, stopping at its ends.
	/// </summary>
	private void MoveSelection(int step)
	{
		int count = LanguagesList.ItemCount;

		if (count == 0)
		{
			return;
		}

		LanguagesList.SelectedIndex = Math.Clamp(
			LanguagesList.SelectedIndex + step,
			0,
			count - 1);
	}

	/// <summary>
	/// Shows the languages that match the search and selects the one that Enter takes.
	/// </summary>
	private void ShowChoices()
	{
		// An extension may be typed with its dot.
		string search = SearchInput.Text?.Trim().TrimStart('.') ?? string.Empty;

		if (search.Length == 0)
		{
			LanguagesList.ItemsSource = _choices;

			// The list opens at the language of the text.
			LanguagesList.SelectedItem = _choices.FirstOrDefault(x => x.Id == SyntaxLanguage) ?? _choices.FirstOrDefault();

			return;
		}

		// The best matches come first, so that Enter takes the language the search most likely means.
		SyntaxLanguageChoice[] matches = [.. _choices
			.Select(x => (Choice: x, Rank: Rank(x, search)))
			.Where(static x => x.Rank is not null)
			.OrderBy(static x => x.Rank)
			.Select(static x => x.Choice)];

		LanguagesList.ItemsSource = matches;

		LanguagesList.SelectedItem = matches.FirstOrDefault();
	}
	#endregion
}
