using CommunityToolkit.Mvvm.ComponentModel;

namespace DataOrganizer.ViewModels.Windows;

/// <summary>
/// View model for <c>ToastWindow</c>.
/// </summary>
internal sealed partial class ToastViewModel : ObservableObject
{
	#region Properties
	/// <summary>
	/// The body text of the toast.
	/// </summary>
	[ObservableProperty]
	public partial string? Message { get; set; }

	/// <summary>
	/// The heading of the toast.
	/// </summary>
	[ObservableProperty]
	public partial string? Title { get; set; }
	#endregion
}
