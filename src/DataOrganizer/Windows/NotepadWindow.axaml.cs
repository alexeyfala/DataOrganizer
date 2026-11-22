using DataOrganizer.ViewModels.Windows;
using System;

namespace DataOrganizer.Windows;

/// <summary>
/// Window of the notepad with its tabs of text.
/// </summary>
public sealed partial class NotepadWindow : AppWindowBase
{
	#region Properties
	/// <inheritdoc cref="NotepadViewModel" />
	public NotepadViewModel ViewModel { get; } = null!;

	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }
	#endregion

	#region Constructors
	/// <summary>
	/// Loads the markup and starts tracking the placement of the window.
	/// </summary>
	public NotepadWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NotepadWindow" /> class.
	/// </summary>
	public NotepadWindow(NotepadViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnClosed(EventArgs e)
	{
		base.OnClosed(e);

		// The tabs end with the window, and the views kept for them go with it.
		Tabs.ForgetViews();
	}
	#endregion
}
