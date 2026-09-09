using Avalonia;
using Avalonia.Controls;
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
	/// <c>True</c> when the "Cancel" button is visible.
	/// </summary>
	[ObservableProperty]
	public partial bool CancelButtonVisible { get; set; }

	/// <summary>
	/// <c>True</c> when the "Cancel" button is <see cref="Button.IsCancel" />.
	/// </summary>
	[ObservableProperty]
	public partial bool CancelIsCancel { get; set; }

	/// <summary>
	/// <c>True</c> when the "No" button is visible.
	/// </summary>
	[ObservableProperty]
	public partial bool NoButtonVisible { get; set; }

	/// <summary>
	/// <c>True</c> when the "No" button is <see cref="Button.IsCancel" />.
	/// </summary>
	[ObservableProperty]
	public partial bool NoIsCancel { get; set; }

	/// <summary>
	/// Text.
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
				NoButtonVisible = true;

				NoIsCancel = true;
				break;

			case YesNoCancelButtons.YesCancel:
				CancelButtonVisible = true;

				CancelIsCancel = true;
				break;

			case YesNoCancelButtons.YesNoCancel:
				NoButtonVisible = true;

				CancelButtonVisible = true;

				CancelIsCancel = true;
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
