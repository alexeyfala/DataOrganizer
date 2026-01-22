using Avalonia;
using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class EditorWindow : Window
{
	#region Properties
	/// <summary>
	/// Previous value of <see cref="Visual.Bounds" />.
	/// </summary>
	public Rect PreviousBounds { get; set; }

	/// <inheritdoc cref="EditorViewModel" />
	public EditorViewModel ViewModel { get; }
	#endregion Properties

	#region Constructors
	public EditorWindow(EditorViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}