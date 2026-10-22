using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System.ComponentModel;

namespace DataOrganizer.Controls;

/// <summary>
/// A flyout whose content is built from <see cref="MenuTemplate" /> anew each time it opens and dropped when it closes.
/// </summary>
internal sealed class LazyMenuFlyout : Flyout
{
	#region Properties
	/// <summary>
	/// Template the content is built from each time the flyout opens; it gets the data of the target.
	/// </summary>
	public IDataTemplate? MenuTemplate
	{
		get => GetValue(MenuTemplateProperty);
		set => SetValue(MenuTemplateProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="MenuTemplate" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<IDataTemplate?> MenuTemplateProperty = AvaloniaProperty
		.Register<LazyMenuFlyout, IDataTemplate?>(name: nameof(MenuTemplate));
	#endregion

	#region Constructors
	static LazyMenuFlyout()
	{
		// A click outside closes the menu and still reaches the place it falls on.
		OverlayDismissEventPassThroughProperty.OverrideDefaultValue<LazyMenuFlyout>(true);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override Control CreatePresenter()
	{
		Control presenter = base.CreatePresenter();

		// Screen readers and UI Automation see one menu around its items, not loose buttons of the window.
		AutomationProperties.SetAccessibilityView(presenter, AccessibilityView.Control);

		AutomationProperties.SetControlTypeOverride(presenter, AutomationControlType.Menu);

		return presenter;
	}

	/// <inheritdoc />
	protected override void OnClosed()
	{
		if (MenuTemplate is not null)
		{
			Content = null;
		}

		base.OnClosed();
	}

	/// <inheritdoc />
	protected override void OnOpening(CancelEventArgs args)
	{
		base.OnOpening(args);

		object? data = Target?.DataContext;

		if (args.Cancel
			|| MenuTemplate?.Build(data) is not { } menu)
		{
			return;
		}

		// The data is set before the menu enters the tree, so it stays with the menu while the target takes other data.
		menu.DataContext = data;

		Content = menu;
	}
	#endregion
}
