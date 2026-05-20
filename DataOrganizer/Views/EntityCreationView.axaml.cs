using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class EntityCreationView : UserControl
{
	#region Constructors
	public EntityCreationView(EntityCreationViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}
	#endregion
}