using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
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
	public MaterialIconKind Icon { get; init; }

	/// <inheritdoc />
	protected override Type StyleKeyOverride { get; } = typeof(Button);
	#endregion

	#region Constructors
	public FlyoutButton()
	{
		FontWeight = FontWeight.Normal;

		HorizontalContentAlignment = HorizontalAlignment.Left;

		Cursor = Cursor.Default;
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

		stackPanel.Children.Add(new MaterialIcon
		{
			Kind = Icon
		});

		stackPanel.Children.Add(new TextBlock
		{
			Text = Header
		});

		Content = stackPanel;
	}
	#endregion
}
