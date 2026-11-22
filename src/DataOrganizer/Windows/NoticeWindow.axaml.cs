using Avalonia.Controls;
using DataOrganizer.ViewModels.Windows;

namespace DataOrganizer.Windows;

internal partial class NoticeWindow : Window
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="NoticeWindow" /> class.
	/// </summary>
	public NoticeWindow() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="NoticeWindow" /> class.
	/// </summary>
	public NoticeWindow(NoticeViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
