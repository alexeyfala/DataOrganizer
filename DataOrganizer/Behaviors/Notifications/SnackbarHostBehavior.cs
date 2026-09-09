using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Interfaces;
using Material.Styles.Controls;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Hands the associated <see cref="SnackbarHost" /> over to <see cref="Presenter" /> while it is on the screen.
/// </summary>
internal sealed class SnackbarHostBehavior : Behavior<SnackbarHost>
{
	#region Properties
	/// <inheritdoc cref="ISnackbarPresenter" />
	public ISnackbarPresenter? Presenter
	{
		get => GetValue(PresenterProperty);
		set => SetValue(PresenterProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Presenter" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<ISnackbarPresenter?> PresenterProperty = AvaloniaProperty
		.Register<SnackbarHostBehavior, ISnackbarPresenter?>(name: nameof(Presenter));
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Control.Loaded" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_Loaded(object? sender, RoutedEventArgs e)
	{
		if (AssociatedObject is not { } host)
		{
			return;
		}

		Presenter?.AttachHost(host);
	}

	/// <summary>
	/// <see cref="Control.Unloaded" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_Unloaded(object? sender, RoutedEventArgs e)
	{
		if (AssociatedObject is not { } host)
		{
			return;
		}

		Presenter?.DetachHost(host);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		if (AssociatedObject is not { } host)
		{
			return;
		}

		host.AddHandler(Control.LoadedEvent, AssociatedObject_Loaded);

		host.AddHandler(Control.UnloadedEvent, AssociatedObject_Unloaded);

		// The host of a window that is already on the screen has no Loaded event left to raise.
		if (!host.IsLoaded)
		{
			return;
		}

		Presenter?.AttachHost(host);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		if (AssociatedObject is not { } host)
		{
			return;
		}

		host.RemoveHandler(Control.LoadedEvent, AssociatedObject_Loaded);

		host.RemoveHandler(Control.UnloadedEvent, AssociatedObject_Unloaded);

		Presenter?.DetachHost(host);
	}
	#endregion
}
