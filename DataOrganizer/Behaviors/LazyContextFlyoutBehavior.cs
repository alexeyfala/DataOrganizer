using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using System;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Lazily creates and assigns a <see cref="Flyout" /> as <see cref="Control.ContextFlyout" />
/// when the user first requests a context menu on the associated control.
/// Until then the flyout's content tree is not built, which avoids paying the
/// template instantiation cost on every realized item container.
/// </summary>
internal sealed class LazyContextFlyoutBehavior : Behavior<Control>
{
	#region Properties
	/// <summary>
	/// Template that produces the flyout's content (typically a <see cref="StackPanel" />
	/// with command buttons).
	/// </summary>
	public IDataTemplate? ContentTemplate
	{
		get => GetValue(ContentTemplateProperty);
		set => SetValue(ContentTemplateProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <inheritdoc cref="ContentTemplate" />
	public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty = AvaloniaProperty
		.Register<LazyContextFlyoutBehavior, IDataTemplate?>(nameof(ContentTemplate));
	#endregion

	#region Data
	/// <summary>
	/// Flyout.
	/// </summary>
	private Flyout? _flyout;
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Control.ContextRequestedEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_ContextRequested(
		object? sender,
		ContextRequestedEventArgs e)
	{
		if (e.Handled
			|| AssociatedObject is null
			|| ContentTemplate is null
			|| _flyout is not null)
		{
			return;
		}

		if (!EnsureFlyout())
		{
			return;
		}

		// The framework's ContextFlyout subscription was added just now (by the
		// assignment above), so it has not run for this invocation. Show the
		// flyout manually for the first request.
		_flyout!.ShowAt(AssociatedObject, showAtPointer: true);

		e.Handled = true;
	}

	/// <summary>
	/// <see cref="StyledElement.DataContextChanged" /> handler of <see cref="AssociatedObject" />.
	/// Clears the cached flyout when the container is recycled by virtualization
	/// to a different item — otherwise the flyout's bindings keep targeting the
	/// previous data item.
	/// </summary>
	private void AssociatedObject_DataContextChanged(
		object? sender,
		EventArgs e)
	{
		if (_flyout is null)
		{
			return;
		}

		if (AssociatedObject is { } control && ReferenceEquals(control.ContextFlyout, _flyout))
		{
			control.ContextFlyout = null;
		}

		_flyout = null;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Builds the flyout (if not yet built) and shows it at <see cref="AssociatedObject" />.
	/// Useful for programmatic triggers, e.g. opening the menu by a double-tap
	/// command rather than by a real <see cref="Control.ContextRequestedEvent" />.
	/// </summary>
	public void Show(bool showAtPointer = false)
	{
		if (AssociatedObject is null || !EnsureFlyout())
		{
			return;
		}

		_flyout!.ShowAt(AssociatedObject, showAtPointer);
	}

	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		if (AssociatedObject is null)
		{
			return;
		}

		AssociatedObject.AddHandler(
			InputElement.ContextRequestedEvent,
			AssociatedObject_ContextRequested);

		AssociatedObject.DataContextChanged += AssociatedObject_DataContextChanged;
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		if (AssociatedObject is null)
		{
			_flyout = null;

			return;
		}

		AssociatedObject.RemoveHandler(
			InputElement.ContextRequestedEvent,
			AssociatedObject_ContextRequested);

		AssociatedObject.DataContextChanged -= AssociatedObject_DataContextChanged;

		if (ReferenceEquals(AssociatedObject.ContextFlyout, _flyout))
		{
			AssociatedObject.ContextFlyout = null;
		}

		_flyout = null;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds the flyout from <see cref="ContentTemplate" /> and assigns it as
	/// <see cref="Control.ContextFlyout" /> on <see cref="AssociatedObject" />.
	/// Returns <c>True</c> if the flyout is ready (either freshly built or already cached).
	/// </summary>
	private bool EnsureFlyout()
	{
		if (_flyout is not null)
		{
			return true;
		}

		if (AssociatedObject is null || ContentTemplate is null)
		{
			return false;
		}

		if (ContentTemplate.Build(AssociatedObject.DataContext) is not { } content)
		{
			return false;
		}

		content.DataContext = AssociatedObject.DataContext;

		_flyout = new Flyout
		{
			Content = content,
			OverlayDismissEventPassThrough = true
		};

		AssociatedObject.ContextFlyout = _flyout;

		return true;
	}
	#endregion
}
