using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Views.Dialogs;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels.Dialogs;

/// <summary>
/// View model for <c>MultilineTextEditView</c>.
/// </summary>
public sealed partial class MultilineTextEditViewModel : BooleanAsyncResultViewModel
{
	#region Properties
	/// <summary>
	/// Text shown above the input field.
	/// </summary>
	[ObservableProperty]
	public partial string? Header { get; set; }

	/// <summary>
	/// <c>True</c> when the edited text is sensitive: a copy of it carries the clipboard sensitivity markers.
	/// </summary>
	[ObservableProperty]
	public partial bool IsSensitive { get; set; }

	/// <summary>
	/// The text being edited.
	/// </summary>
	[ObservableProperty]
	public partial string? Text { get; set; }
	#endregion

	#region Constructors
	public MultilineTextEditViewModel(
		Application app,
		ITaskExceptionHandler exceptionHandler) : base(app, exceptionHandler)
	{
	}
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes the dialog and discards the edit.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => SetResultAsync(false);

	/// <summary>
	/// Closes the dialog and keeps the edit.
	/// </summary>
	[RelayCommand]
	private Task Save() => SetResultAsync(true);
	#endregion
}
