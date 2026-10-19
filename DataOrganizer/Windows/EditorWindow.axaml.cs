using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DataOrganizer.ViewModels.Windows;

namespace DataOrganizer.Windows;

public sealed partial class EditorWindow : Window, IMainWindow
{
	#region Properties
	/// <inheritdoc />
	public bool IsClosingToSwitch { get; set; }

	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }

	/// <inheritdoc cref="EditorViewModel" />
	public EditorViewModel ViewModel { get; } = null!;
	#endregion

	#region Constructors
	public EditorWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this);
	}

	public EditorWindow(EditorViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);

		ReportShown();
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == IsVisibleProperty)
		{
			ReportShown();
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Tells the view model whether the window is loaded and visible.
	/// </summary>
	private void ReportShown()
	{
		if (DataContext is not EditorViewModel viewModel)
		{
			return;
		}

		viewModel.IsWindowShown = IsLoaded && IsVisible;
	}
	#endregion
}
