using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal sealed partial class CopyHistoryView : UserControl
{
	#region Properties
	/// <inheritdoc cref="CopyHistoryViewModel" />
	public CopyHistoryViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public CopyHistoryView()
	{
		InitializeComponent();

		DataContext = ViewModel = Ioc
			.Default
			.GetRequiredService<CopyHistoryViewModel>();
	}
	#endregion
}