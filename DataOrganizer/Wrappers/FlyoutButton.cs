using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using DataOrganizer.Extensions;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using FontWeight = Avalonia.Media.FontWeight;

namespace DataOrganizer.Wrappers;

internal sealed class FlyoutButton : Button
{
	#region Properties
	/// <summary>
	/// Header.
	/// </summary>
	public string? Header { get; init; }

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
	/// Identifies the <see cref="Icon" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<MaterialIconKind> IconProperty = AvaloniaProperty
		.Register<FlyoutButton, MaterialIconKind>(name: nameof(Icon));
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
			.FindLogicalParent<Control>(x => x.ContextFlyout is not null)?
			.ContextFlyout?
			.Hide();

		this
			.FindLogicalParent<Button>(x => x.Flyout is not null)?
			.Flyout?
			.Hide();
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

		//if (Flyout is not null)
		//{
		//	stackPanel.Children.Add(new MaterialIcon
		//	{
		//		Kind = MaterialIconKind.Play,
		//		HorizontalAlignment = HorizontalAlignment.Right
		//	});
		//}

		Content = stackPanel;
	}

	///// <inheritdoc />
	//protected override async void OnPointerEntered(PointerEventArgs e)
	//{
	//	base.OnPointerEntered(e);

	//	if (Flyout is null)
	//	{
	//		return;
	//	}

	//	await System.Threading.Tasks.Task
	//		.Delay(Shared.Common.AppUtils.TipDelay)
	//		.ConfigureAwait(true);

	//	if (!IsPointerOver)
	//	{
	//		return;
	//	}

	//	Flyout.ShowAt(this);
	//}
	#endregion
}
