using DataOrganizer.ViewModels.Windows;

namespace DataOrganizer.Windows;

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
