using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Windows;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="ToastWindow" />.
/// </summary>
internal sealed partial class ToastViewModel : ObservableObject
{
	#region Auto-Generated Properties
	/// <summary>
	/// Message.
	/// </summary>
	[ObservableProperty]
	private string? _message;

	/// <summary>
	/// Title.
	/// </summary>
	[ObservableProperty]
	private string? _title;
	#endregion
}
