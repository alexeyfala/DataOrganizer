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
	public Rect PreviousBounds { get; private set; }

	/// <inheritdoc cref="EditorViewModel" />
	public EditorViewModel ViewModel { get; } = null!;
	#endregion Properties

	#region Constructors
	/// <summary>
	/// Parameterless ctor for the Avalonia XAML compiler / previewer.
	/// Not used at runtime — DI always invokes the overload below.
	/// </summary>
	public EditorWindow() => InitializeComponent();

	public EditorWindow(EditorViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnResized(WindowResizedEventArgs e)
	{
		base.OnResized(e);

		if (WindowState == WindowState.Maximized)
		{
			return;
		}

		// Remember bounds only in the normal (non-maximized) state,
		// so we can restore proper size after un-maximizing.
		PreviousBounds = Bounds;
	}
	#endregion
}
