using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Windows;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="ToastWindow" />.
/// </summary>
internal sealed partial class ToastViewModel : ObservableObject
{
	#region Properties
	/// <summary>
	/// Message.
	/// </summary>
	[ObservableProperty]
	public partial string? Message { get; set; }

	/// <summary>
	/// Title.
	/// </summary>
	[ObservableProperty]
	public partial string? Title { get; set; }
	#endregion
}
