using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Views.Dialogs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels.Dialogs;

/// <summary>
/// View model for <c>YesNoCancelBoxView</c>.
/// </summary>
public sealed partial class YesNoCancelBoxViewModel : AsyncResultViewModelBase<YesNoCancelAnswer>
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the Escape key activates the "Cancel" button.
	/// </summary>
	[ObservableProperty]
	public partial bool CancelButtonHandlesEscape { get; set; }

	/// <summary>
	/// <c>True</c> when the "Cancel" button is visible.
	/// </summary>
	[ObservableProperty]
	public partial bool IsCancelButtonVisible { get; set; }

	/// <summary>
	/// <c>True</c> when the "No" button is visible.
	/// </summary>
	[ObservableProperty]
	public partial bool IsNoButtonVisible { get; set; }

	/// <summary>
	/// <c>True</c> when the Escape key activates the "No" button.
	/// </summary>
	[ObservableProperty]
	public partial bool NoButtonHandlesEscape { get; set; }

	/// <summary>
	/// The question put to the user.
	/// </summary>
	[ObservableProperty]
	public partial string? Text { get; set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Handles "Cancel" button pressed.
	/// </summary>
	[RelayCommand]
	private Task CancelButtonPressed() => SetResultAsync(YesNoCancelAnswer.Cancel);

	/// <summary>
	/// Handles "No" button pressed.
	/// </summary>
	[RelayCommand]
	private Task NoButtonPressed() => SetResultAsync(YesNoCancelAnswer.No);

	/// <summary>
	/// Handles "Yes" button pressed.
	/// </summary>
	[RelayCommand]
	private Task YesButtonPressed() => SetResultAsync(YesNoCancelAnswer.Yes);
	#endregion

	#region Methods
	/// <inheritdoc cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />
	public Task<YesNoCancelAnswer> GetResultAsync(
		YesNoCancelButtons variant,
		CancellationToken token = default)
	{
		switch (variant)
		{
			case YesNoCancelButtons.YesNo:
				IsNoButtonVisible = true;

				NoButtonHandlesEscape = true;
				break;

			case YesNoCancelButtons.YesCancel:
				IsCancelButtonVisible = true;

				CancelButtonHandlesEscape = true;
				break;

			case YesNoCancelButtons.YesNoCancel:
				IsNoButtonVisible = true;

				IsCancelButtonVisible = true;

				CancelButtonHandlesEscape = true;
				break;

			default:
				throw new NotImplementedException();
		}

		return GetResultAsync(
			defaultResult: YesNoCancelAnswer.Cancel,
			token: token);
	}
	#endregion

	#region Constructors
	public YesNoCancelBoxViewModel(
		Application app,
		ITaskExceptionHandler exceptionHandler) : base(app, exceptionHandler)
	{
	}
	#endregion
}
