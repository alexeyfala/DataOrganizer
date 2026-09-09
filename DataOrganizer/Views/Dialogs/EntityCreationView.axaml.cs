using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class EntityCreationView : UserControl
{
	#region Constructors
	public EntityCreationView() => InitializeComponent();

	public EntityCreationView(EntityCreationViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
