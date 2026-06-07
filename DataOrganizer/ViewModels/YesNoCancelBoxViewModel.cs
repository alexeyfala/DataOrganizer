using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>YesNoCancelBox</c>.
/// </summary>
public sealed partial class YesNoCancelBoxViewModel : AsyncResultViewModelBase<YesNoCancelResult>
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
	private Task CancelButtonPressed() => SetResultAsync(YesNoCancelResult.Cancel);

	/// <summary>
	/// Handles "No" button pressed.
	/// </summary>
	[RelayCommand]
	private Task NoButtonPressed() => SetResultAsync(YesNoCancelResult.No);

	/// <summary>
	/// Handles "Yes" button pressed.
	/// </summary>
	[RelayCommand]
	private Task YesButtonPressed() => SetResultAsync(YesNoCancelResult.Yes);
	#endregion

	#region Methods
	/// <inheritdoc cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />
	public Task<YesNoCancelResult> GetResultAsync(
		YesNoCancelVariant variant,
		CancellationToken token = default)
	{
		switch (variant)
		{
			case YesNoCancelVariant.YesNo:
				NoButtonVisible = true;

				NoIsCancel = true;
				break;

			case YesNoCancelVariant.YesCancel:
				CancelButtonVisible = true;

				CancelIsCancel = true;
				break;

			case YesNoCancelVariant.YesNoCancel:
				NoButtonVisible = true;

				CancelButtonVisible = true;

				CancelIsCancel = true;
				break;

			default:
				throw new NotImplementedException();
		}

		return GetResultAsync(
			defaultResult: YesNoCancelResult.Cancel,
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
