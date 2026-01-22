using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using DataOrganizer.Extensions;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Threading.Tasks;
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
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Button.ClickEvent" /> handler.
	/// </summary>
	private async void Button_Click(object? sender, RoutedEventArgs e)
	{
		// The delay is necessary, otherwise the button command will not be executed.
		await Task
			.Delay(50)
			.ConfigureAwait(true);

		this
			.FindParent<Control>(x => x.ContextFlyout is not null)?
			.ContextFlyout?
			.Hide();

		this
			.FindParent<Button>(x => x.Flyout is not null)?
			.Flyout?
			.Hide();
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		if (Flyout is not null)
		{
			return;
		}

		AddHandler(
			ClickEvent,
			Button_Click,
			RoutingStrategies.Bubble);
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

	/// <inheritdoc />
	protected override void OnUnloaded(RoutedEventArgs e)
	{
		base.OnUnloaded(e);

		if (Flyout is not null)
		{
			return;
		}

		RemoveHandler(ClickEvent, Button_Click);
	}
	#endregion
}
