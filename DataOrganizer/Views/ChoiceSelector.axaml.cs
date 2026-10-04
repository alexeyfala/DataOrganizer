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
using System.Windows.Input;

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
	/// Command run with the id of the item taken, for a choice that changes something rather than being kept; the chosen
	/// item is then left to the owner.
	/// </summary>
	public ICommand? ChooseCommand
	{
		get => GetValue(ChooseCommandProperty);
		set => SetValue(ChooseCommandProperty, value);
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
	/// Command run as the list opens, before it takes the items that cannot be chosen.
	/// </summary>
	public ICommand? FlyoutOpeningCommand
	{
		get => GetValue(FlyoutOpeningCommandProperty);
		set => SetValue(FlyoutOpeningCommandProperty, value);
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
	/// <c>True</c> when the list has a search box and keeps its height whatever the search finds; a short list without one
	/// takes the height of its items.
	/// </summary>
	public bool IsSearchable
	{
		get => GetValue(IsSearchableProperty);
		set => SetValue(IsSearchableProperty, value);
	}

	/// <summary>
	/// Id of the chosen item.
	/// </summary>
	public string? SelectedChoice
	{
		get => GetValue(SelectedChoiceProperty);
		set => SetValue(SelectedChoiceProperty, value);
	}

	/// <summary>
	/// Ids of the items that cannot be chosen, which the list grays out and puts last; the chosen item stays available.
	/// </summary>
	public IReadOnlyCollection<string>? UnavailableChoices
	{
		get => GetValue(UnavailableChoicesProperty);
		set => SetValue(UnavailableChoicesProperty, value);
	}

	/// <summary>
	/// Tip that the list shows over an item that cannot be chosen.
	/// </summary>
	public string? UnavailableTip
	{
		get => GetValue(UnavailableTipProperty);
		set => SetValue(UnavailableTipProperty, value);
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
	/// Identifies the <see cref="ChooseCommand" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<ICommand?> ChooseCommandProperty = AvaloniaProperty
		.Register<ChoiceSelector, ICommand?>(name: nameof(ChooseCommand));

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
	/// Identifies the <see cref="FlyoutOpeningCommand" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<ICommand?> FlyoutOpeningCommandProperty = AvaloniaProperty
		.Register<ChoiceSelector, ICommand?>(name: nameof(FlyoutOpeningCommand));

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
	/// Identifies the <see cref="IsSearchable" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsSearchableProperty = AvaloniaProperty
		.Register<ChoiceSelector, bool>(
			name: nameof(IsSearchable),
			defaultValue: true);

	/// <summary>
	/// Identifies the <see cref="SelectedChoice" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> SelectedChoiceProperty = AvaloniaProperty
		.Register<ChoiceSelector, string?>(name: nameof(SelectedChoice));

	/// <summary>
	/// Identifies the <see cref="UnavailableChoices" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<IReadOnlyCollection<string>?> UnavailableChoicesProperty = AvaloniaProperty
		.Register<ChoiceSelector, IReadOnlyCollection<string>?>(name: nameof(UnavailableChoices));

	/// <summary>
	/// Identifies the <see cref="UnavailableTip" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> UnavailableTipProperty = AvaloniaProperty
		.Register<ChoiceSelector, string?>(name: nameof(UnavailableTip));
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

		// The items that cannot be chosen may have changed since the list was open last.
		if (FlyoutOpeningCommand is { } command && command.CanExecute(null))
		{
			command.Execute(null);
		}

		string? defaultChoice = DefaultChoice;

		string? defaultMark = DefaultMark;

		string? selectedChoice = SelectedChoice;

		IReadOnlyCollection<string> unavailableChoices = UnavailableChoices ?? [];

		string? unavailableTip = UnavailableTip;

		// The items that cannot be chosen go last, each part keeping its order.
		_choices = [.. (Choices ?? [])
			.Select(x =>
			{
				// The chosen item is in effect, so taking it again changes nothing.
				bool isAvailable = x.Id is not { } id || id == selectedChoice || !unavailableChoices.Contains(id);

				return x with
				{
					DefaultMark = x.Id == defaultChoice ? defaultMark : null,
					IsAvailable = isAvailable,
					UnavailableTip = isAvailable ? null : unavailableTip
				};
			})
			.OrderBy(static x => !x.IsAvailable)];

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
		// Enter over an empty search result keeps the list open, and so does an item that cannot be chosen.
		if (choice is not { IsAvailable: true })
		{
			return;
		}

		// The owner of a command shows the outcome itself, as the choice may come to nothing.
		if (ChooseCommand is not { } command)
		{
			SetCurrentValue(SelectedChoiceProperty, choice.Id);
		}
		else if (command.CanExecute(choice.Id))
		{
			command.Execute(choice.Id);
		}

		_flyout.Hide();
	}

	/// <summary>
	/// Moves the selection of the list by a number of items, stopping at its first item and at the last one that can be
	/// chosen.
	/// </summary>
	private void MoveSelection(int step)
	{
		// The items that cannot be chosen come last, so the selection stops before them.
		int count = ChoicesList
			.Items
			.Cast<SelectorChoice>()
			.Count(static x => x.IsAvailable);

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
			ChoicesList.SelectedItem = _choices.FirstOrDefault(x => x.Id == SelectedChoice)
				?? _choices.FirstOrDefault(static x => x.IsAvailable);

			return;
		}

		// The best matches come first, so that Enter takes the item the search most likely means, while the items that
		// cannot be chosen follow all the others.
		SelectorChoice[] matches = [.. _choices
			.Select(x => (Choice: x, Rank: Rank(x, search)))
			.Where(static x => x.Rank is not null)
			.OrderBy(static x => !x.Choice.IsAvailable)
			.ThenBy(static x => x.Rank)
			.Select(static x => x.Choice)];

		ChoicesList.ItemsSource = matches;

		ChoicesList.SelectedItem = matches.FirstOrDefault(static x => x.IsAvailable);
	}
	#endregion
}
