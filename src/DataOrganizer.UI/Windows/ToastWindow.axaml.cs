using DataOrganizer.UI.ViewModels.Windows;

namespace DataOrganizer.UI.Windows;

/// <summary>
/// Window that shows a short message in the lower corner of the screen.
/// </summary>
internal partial class ToastWindow : AppWindowBase
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="ToastWindow" /> class.
	/// </summary>
	public ToastWindow() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="ToastWindow" /> class.
	/// </summary>
	public ToastWindow(ToastViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
