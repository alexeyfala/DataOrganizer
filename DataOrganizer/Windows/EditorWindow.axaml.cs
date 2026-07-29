//using Avalonia;
using Avalonia.Controls;
using DataOrganizer.Helpers;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class EditorWindow : Window
{
	#region Properties
	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }

	// Replaced by "Placement", which tracks both the size and the position.
	///// <summary>
	///// Previous value of <see cref="Visual.Bounds" />.
	///// </summary>
	//public Rect PreviousBounds { get; private set; }

	/// <inheritdoc cref="EditorViewModel" />
	public EditorViewModel ViewModel { get; } = null!;
	#endregion Properties

	#region Constructors
	public EditorWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this);
	}

	public EditorWindow(EditorViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion

	#region Methods
	///// <inheritdoc />
	//protected override void OnResized(WindowResizedEventArgs e)
	//{
	//	base.OnResized(e);
	//
	//	if (WindowState == WindowState.Maximized)
	//	{
	//		return;
	//	}
	//
	//	// Remember bounds only in the normal (non-maximized) state,
	//	// so we can restore proper size after un-maximizing.
	//	PreviousBounds = Bounds;
	//}
	#endregion
}
