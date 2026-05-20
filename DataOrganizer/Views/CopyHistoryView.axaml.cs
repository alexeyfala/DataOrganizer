using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal sealed partial class CopyHistoryView : UserControl
{
	#region Constructors
	public CopyHistoryView()
	{
		InitializeComponent();

		DataContext = Ioc
			.Default
			.GetRequiredService<CopyHistoryViewModel>();
	}
	#endregion
}