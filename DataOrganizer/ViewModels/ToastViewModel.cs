using CommunityToolkit.Mvvm.ComponentModel;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>ToastWindow</c>.
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
