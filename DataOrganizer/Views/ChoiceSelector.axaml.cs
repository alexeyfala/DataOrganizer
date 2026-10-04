using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DataOrganizer.Dto.Documents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Views;

/// <summary>
/// Button with the chosen item of a list, which it opens to choose another one, searched by name and by other words.
/// </summary>
internal sealed partial class ChoiceSelector : UserControl
{
	#region Properties
	/// <summary>
	/// Text of the button.
	/// </summary>
	public string? Caption
	{
		get => GetValue(CaptionProperty);
		set => SetValue(CaptionProperty, value);
	}

	/// <summary>
	/// Items of the list.
	/// </summary>
	public IReadOnlyList<SelectorChoice>? Choices
	{
		get => GetValue(ChoicesProperty);
		set => SetValue(ChoicesProperty, value);
	}

	/// <summary>
	/// Id of the item taken when none is chosen.
	/// </summary>
	public string? DefaultChoice
	{
		get => GetValue(DefaultChoiceProperty);
		set => SetValue(DefaultChoiceProperty, value);
	}

	/// <summary>
	/// Mark that the list shows after the item taken when none is chosen.
	/// </summary>
	public string? DefaultMark
	{
		get => GetValue(DefaultMarkProperty);
		set => SetValue(DefaultMarkProperty, value);
	}

	/// <summary>
	/// Placement of the list against the button.
	/// </summary>
	public PlacementMode FlyoutPlacement
	{
		get => GetValue(FlyoutPlacementProperty);
		set => SetValue(FlyoutPlacementProperty, value);
	}

	/// <summary>
	/// Width of the list.
	/// </summary>
	public double FlyoutWidth
	{
		get => GetValue(FlyoutWidthProperty);
		set => SetValue(FlyoutWidthProperty, value);
	}

	/// <summary>
	/// Id of the chosen item.
	/// </summary>
	public string? SelectedChoice
	{
		get => GetValue(SelectedChoiceProperty);
		set => SetValue(SelectedChoiceProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Caption" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> CaptionProperty = AvaloniaProperty
		.Register<ChoiceSelector, string?>(name: nameof(Caption));

	/// <summary>
	/// Identifies the <see cref="Choices" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<IReadOnlyList<SelectorChoice>?> ChoicesProperty = AvaloniaProperty
		.Register<ChoiceSelector, IReadOnlyList<SelectorChoice>?>(name: nameof(Choices));

	/// <summary>
	/// Identifies the <see cref="DefaultChoice" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> DefaultChoiceProperty = AvaloniaProperty
		.Register<ChoiceSelector, string?>(name: nameof(DefaultChoice));

	/// <summary>
	/// Identifies the <see cref="DefaultMark" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> DefaultMarkProperty = AvaloniaProperty
		.Register<ChoiceSelector, string?>(name: nameof(DefaultMark));

	/// <summary>
	/// Identifies the <see cref="FlyoutPlacement" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<PlacementMode> FlyoutPlacementProperty = AvaloniaProperty
		.Register<ChoiceSelector, PlacementMode>(
			name: nameof(FlyoutPlacement),
			defaultValue: PlacementMode.TopEdgeAlignedLeft);

	/// <summary>
	/// Identifies the <see cref="FlyoutWidth" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> FlyoutWidthProperty = AvaloniaProperty
		.Register<ChoiceSelector, double>(
			name: nameof(FlyoutWidth),
			defaultValue: 280.0);

	/// <summary>
	/// Identifies the <see cref="SelectedChoice" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> SelectedChoiceProperty = AvaloniaProperty
		.Register<ChoiceSelector, string?>(name: nameof(SelectedChoice));
	#endregion

	#region Data
	/// <summary>
	/// List of the items that the button opens.
	/// </summary>
	private readonly PopupFlyoutBase _flyout;

	/// <summary>
	/// Items of the list, the default one marked, as they were when it opened.
	/// </summary>
	private SelectorChoice[] _choices = [];

	/// <summary>
	/// Element that had the focus before the list opened.
	/// </summary>
	private IInputElement? _focusedElement;
	#endregion

	#region Constructors
	public ChoiceSelector()
	{
		InitializeComponent();

		// The markup gives the flyout no field of its own.
		_flyout = (PopupFlyoutBase)CurrentChoice.Flyout!;

		_flyout.Opening += Flyout_Opening;

		_flyout.Closed += Flyout_Closed;

		// The keys work in the search box and in the list alike, and the arrows do not move the caret of the search.
		ChoicesHost.AddHandler(
			KeyDownEvent,
			ChoicesHost_KeyDown,
			RoutingStrategies.Tunnel);

		ChoicesList.Tapped += ChoicesList_Tapped;

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
				Choose(ChoicesList.SelectedItem as SelectorChoice);
				break;

			default:
				return;
		}

		e.Handled = true;
	}

	/// <summary>
	/// <see cref="InputElement.Tapped" /> event handler of <see cref="ChoicesList" />.
	/// </summary>
	private void ChoicesList_Tapped(object? sender, TappedEventArgs e)
	{
		// A tap on the scroll bar of the list takes no item.
		if ((e.Source as Visual)?
			.FindAncestorOfType<ListBoxItem>(includeSelf: true) is not { DataContext: SelectorChoice choice })
		{
			return;
		}

		Choose(choice);
	}

	/// <summary>
	/// <see cref="FlyoutBase.Closed" /> event handler of the list of the items.
	/// </summary>
	private void Flyout_Closed(object? sender, EventArgs e)
	{
		// The search box took the focus from the text, which gets it back.
		_focusedElement?.Focus();

		_focusedElement = null;
	}

	/// <summary>
	/// <see cref="PopupFlyoutBase.Opening" /> event handler of the list of the items.
	/// </summary>
	private void Flyout_Opening(object? sender, EventArgs e)
	{
		_focusedElement = TopLevel
			.GetTopLevel(this)?
			.FocusManager?
			.GetFocusedElement();

		string? defaultChoice = DefaultChoice;

		string? defaultMark = DefaultMark;

		_choices = [.. (Choices ?? []).Select(x => x with
		{
			DefaultMark = x.Id == defaultChoice ? defaultMark : null
		})];

		// A search left from the last time would hide the other items.
		SearchInput.Text = null;

		ShowChoices();
	}

	/// <summary>
	/// <see cref="TextBox.TextProperty" /> changed handler of <see cref="SearchInput" />.
	/// </summary>
	private void TextProperty_Changed(string? value) => ShowChoices();
	#endregion

	#region Helpers
	/// <summary>
	/// Returns how well an item matches the search, where a lower rank is better; <c>null</c> when it does not match.
	/// </summary>
	private static int? Rank(SelectorChoice choice, string search)
	{
		if (choice
			.SearchTerms
			.Any(x => x.Equals(search, StringComparison.OrdinalIgnoreCase)))
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
			.SearchTerms
			.Any(x => x.StartsWith(search, StringComparison.OrdinalIgnoreCase))
			|| choice.Description?.StartsWith(search, StringComparison.OrdinalIgnoreCase) == true)
		{
			return 2;
		}

		if (choice
			.Name
			.Contains(search, StringComparison.OrdinalIgnoreCase))
		{
			return 3;
		}

		return choice.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) == true ? 4 : null;
	}

	/// <summary>
	/// Takes an item and closes the list.
	/// </summary>
	private void Choose(SelectorChoice? choice)
	{
		// Enter over an empty search result keeps the list open.
		if (choice is null)
		{
			return;
		}

		SetCurrentValue(SelectedChoiceProperty, choice.Id);

		_flyout.Hide();
	}

	/// <summary>
	/// Moves the selection of the list by a number of items, stopping at its ends.
	/// </summary>
	private void MoveSelection(int step)
	{
		int count = ChoicesList.ItemCount;

		if (count == 0)
		{
			return;
		}

		ChoicesList.SelectedIndex = Math.Clamp(
			ChoicesList.SelectedIndex + step,
			0,
			count - 1);
	}

	/// <summary>
	/// Shows the items that match the search and selects the one that Enter takes.
	/// </summary>
	private void ShowChoices()
	{
		// An extension may be typed with its dot.
		string search = SearchInput.Text?.Trim().TrimStart('.') ?? string.Empty;

		if (search.Length == 0)
		{
			ChoicesList.ItemsSource = _choices;

			// The list opens at the chosen item.
			ChoicesList.SelectedItem = _choices.FirstOrDefault(x => x.Id == SelectedChoice) ?? _choices.FirstOrDefault();

			return;
		}

		// The best matches come first, so that Enter takes the item the search most likely means.
		SelectorChoice[] matches = [.. _choices
			.Select(x => (Choice: x, Rank: Rank(x, search)))
			.Where(static x => x.Rank is not null)
			.OrderBy(static x => x.Rank)
			.Select(static x => x.Choice)];

		ChoicesList.ItemsSource = matches;

		ChoicesList.SelectedItem = matches.FirstOrDefault();
	}
	#endregion
}
