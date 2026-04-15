using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class ExecutedFilesView : UserControl
{
	#region Properties
	/// <inheritdoc cref="ExecutedFilesViewModel" />
	public ExecutedFilesViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public ExecutedFilesView()
	{
		InitializeComponent();

		DataContext = ViewModel = Ioc
			.Default
			.GetRequiredService<ExecutedFilesViewModel>();
	}
	#endregion
}