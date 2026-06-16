using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class ClipboardLogWindow : Window
{
	#region Properties
	/// <inheritdoc cref="ClipboardLogViewModel" />
	public ClipboardLogViewModel ViewModel { get; } = null!;
	#endregion

	#region Constructors
	public ClipboardLogWindow() => InitializeComponent();

	public ClipboardLogWindow(ClipboardLogViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
