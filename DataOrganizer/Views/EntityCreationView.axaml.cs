using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class EntityCreationView : UserControl
{
	#region Properties
	/// <inheritdoc cref="EntityCreationViewModel" />
	public EntityCreationViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public EntityCreationView(EntityCreationViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}