using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Globalization;
using System.Linq;
using FontWeight = Avalonia.Media.FontWeight;

namespace DataOrganizer.Controls;

/// <summary>
/// A menu-style button that builds its content from <see cref="Icon" /> and <see cref="Header" />,
/// and closes the flyout it sits in when clicked, unless it opens a flyout of its own.
/// </summary>
internal sealed class FlyoutButton : Button
{
	#region Properties
	/// <summary>
	/// Keys of the command, shown right of <see cref="Header" />, or added to the tip of a button without one.
	/// </summary>
	public string? Gesture
	{
		get => GetValue(GestureProperty);
		set => SetValue(GestureProperty, value);
	}

	/// <summary>
	/// Header.
	/// </summary>
	public string? Header
	{
		get => GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// Icon.
	/// </summary>
	public MaterialIconKind Icon
	{
		get => GetValue(IconProperty);
		set => SetValue(IconProperty, value);
	}

	/// <summary>
	/// A foreground for icon.
	/// </summary>
	public IBrush? IconForeground { get; init; }

	/// <inheritdoc />
	protected override Type StyleKeyOverride { get; } = typeof(Button);
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Gesture" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> GestureProperty = AvaloniaProperty
		.Register<FlyoutButton, string?>(name: nameof(Gesture));

	/// <summary>
	/// Identifies the <see cref="Header" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> HeaderProperty = AvaloniaProperty
		.Register<FlyoutButton, string?>(name: nameof(Header));

	/// <summary>
	/// Identifies the <see cref="Icon" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<MaterialIconKind> IconProperty = AvaloniaProperty
		.Register<FlyoutButton, MaterialIconKind>(name: nameof(Icon));
	#endregion

	#region Data
	/// <summary>
	/// Style class of the text with the keys.
	/// </summary>
	private const string GestureClass = "GestureInFlyoutTextBlockStyle";

	/// <summary>
	/// Gap between the header and the keys.
	/// </summary>
	private const double GestureSpacing = 24.0;

	/// <summary>
	/// Format of a tip with the keys after it.
	/// </summary>
	private const string TipFormat = "{0} ({1})";
	#endregion

	#region Constructors
	public FlyoutButton()
	{
		FontWeight = FontWeight.Normal;

		HorizontalContentAlignment = HorizontalAlignment.Left;

		Cursor = Cursor.Default;

		FontSize = 15.0;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnClick()
	{
		base.OnClick();

		if (Flyout is not null)
		{
			return;
		}

		this
			.GetLogicalAncestors()
			.OfType<Control>()
			.FirstOrDefault(x => x.ContextFlyout is not null)?
			.ContextFlyout?
			.Hide();

		this
			.GetLogicalAncestors()
			.OfType<Button>()
			.FirstOrDefault(x => x.Flyout is not null)?
			.Flyout?
			.Hide();
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();

		// A button without a header has only its tip to show the keys in.
		if (Header is not null
			|| Gesture is not { } gesture
			|| ToolTip.GetTip(this) is not string tip)
		{
			return;
		}

		ToolTip.SetTip(this, string.Format(CultureInfo.CurrentCulture, TipFormat, tip, gesture));
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);

		if (Content is not null)
		{
			return;
		}

		StackPanel stackPanel = new()
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10.0
		};

		MaterialIcon icon = new()
		{
			Kind = Icon,
			IconSize = FontSize
		};

		if (IconForeground is not null)
		{
			icon.Foreground = IconForeground;
		}

		stackPanel.Children.Add(icon);

		stackPanel.Children.Add(new TextBlock
		{
			Text = Header
		});

		if (Gesture is not { } gesture)
		{
			Content = stackPanel;

			return;
		}

		// The keys stand at the right edge, which the content reaches only when it stretches.
		HorizontalContentAlignment = HorizontalAlignment.Stretch;

		TextBlock keys = new()
		{
			Margin = new Thickness(GestureSpacing, 0.0, 0.0, 0.0),
			Text = gesture
		};

		keys.Classes.Add(GestureClass);

		DockPanel.SetDock(keys, Dock.Right);

		Content = new DockPanel
		{
			Children =
			{
				keys,
				stackPanel
			}
		};
	}
	#endregion
}
