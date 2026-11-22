using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

/// <summary>
/// Dialog that creates a folder, a file or a dataset.
/// </summary>
public sealed partial class EntityCreationView : UserControl
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="EntityCreationView" /> class.
	/// </summary>
	public EntityCreationView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="EntityCreationView" /> class.
	/// </summary>
	public EntityCreationView(EntityCreationViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
