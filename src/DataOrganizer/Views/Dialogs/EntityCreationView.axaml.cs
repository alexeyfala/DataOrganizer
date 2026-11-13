using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

public sealed partial class EntityCreationView : UserControl
{
	#region Constructors
	public EntityCreationView() => InitializeComponent();

	public EntityCreationView(EntityCreationViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
