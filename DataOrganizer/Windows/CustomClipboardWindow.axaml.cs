using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class CustomClipboardWindow : Window
{
	#region Properties
	/// <inheritdoc cref="CustomClipboardViewModel" />
	public CustomClipboardViewModel ViewModel { get; } = null!;
	#endregion

	#region Constructors
	public CustomClipboardWindow() => InitializeComponent();

	public CustomClipboardWindow(CustomClipboardViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
