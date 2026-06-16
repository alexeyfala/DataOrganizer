using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class CustomClipboardWindow : Window
{
	#region Properties
	/// <inheritdoc cref="ClipboardLogViewModel" />
	public ClipboardLogViewModel ViewModel { get; } = null!;
	#endregion

	#region Constructors
	public CustomClipboardWindow() => InitializeComponent();

	public CustomClipboardWindow(ClipboardLogViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
