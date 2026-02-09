using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Enums;
using DataOrganizer.Views;
using DialogHostAvalonia;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="YesNoCancelBox" />.
/// </summary>
public sealed partial class YesNoCancelBoxViewModel : ObservableObject
{
	#region Auto-Generated Properties
	/// <summary>
	/// Returns <c>True</c> if "Canlel" button is visible.
	/// </summary>
	[ObservableProperty]
	private bool _cancelButtonVisible;

	/// <summary>
	/// Returns <c>True</c> if "Cancel" button is <see cref="Button.IsCancel" />.
	/// </summary>
	[ObservableProperty]
	private bool _cancelIsCancel;

	/// <summary>
	/// Returns <c>True</c> if "No" button is visible.
	/// </summary>
	[ObservableProperty]
	private bool _noButtonVisible;

	/// <summary>
	/// Returns <c>True</c> if "No" button is <see cref="Button.IsCancel" />.
	/// </summary>
	[ObservableProperty]
	private bool _noIsCancel;

	/// <summary>
	/// Text.
	/// </summary>
	[ObservableProperty]
	private string? _text;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Handles "Cancel" button pressed.
	/// </summary>
	[RelayCommand]
	private void CancelButtonPressed() => _source.TrySetResult(YesNoCancelResult.Cancel);

	/// <summary>
	/// Handles "No" button pressed.
	/// </summary>
	[RelayCommand]
	private void NoButtonPressed() => _source.TrySetResult(YesNoCancelResult.No);

	/// <summary>
	/// Handles "Yes" button pressed.
	/// </summary>
	[RelayCommand]
	private void YesButtonPressed() => _source.TrySetResult(YesNoCancelResult.Yes);
	#endregion

	#region Data
	/// <inheritdoc cref="TaskCompletionSource" />
	private readonly TaskCompletionSource<YesNoCancelResult> _source = new();
	#endregion

	#region Methods
	/// <summary>
	/// Returns a result of the user choice.
	/// </summary>
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

			case YesNoCancelVariant.YesNoCancel:
				NoButtonVisible = true;

				CancelButtonVisible = true;

				CancelIsCancel = true;
				break;

			default:
				throw new NotImplementedException();
		}

		if (!AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			_ = WaitDialogCloseAsync(token);
		}

		return _source.Task;
	}
	#endregion

	#region Service
	/// <summary>
	/// Waits for the dialog <see cref="DialogHost" /> to close.
	/// </summary>
	/// <remarks>
	/// Needed in case the user closes the dialog without using provided buttons.
	/// </remarks>
	private async Task WaitDialogCloseAsync(CancellationToken token = default)
	{
		while (DialogHost.IsDialogOpen(null))
		{
			await Task
				.Delay(500, token)
				.ConfigureAwait(false);
		}

		_source.TrySetResult(YesNoCancelResult.Cancel);
	}
	#endregion
}
