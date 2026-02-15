using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
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
public sealed partial class YesNoCancelBoxViewModel : AsyncResultViewModelBase<YesNoCancelResult>
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
	private void CancelButtonPressed()
	{
		if (!AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			DialogHost.Close(null);
		}

		_source.TrySetResult(YesNoCancelResult.Cancel);
	}

	/// <summary>
	/// Handles "No" button pressed.
	/// </summary>
	[RelayCommand]
	private void NoButtonPressed()
	{
		if (!AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			DialogHost.Close(null);
		}

		_source.TrySetResult(YesNoCancelResult.No);
	}

	/// <summary>
	/// Handles "Yes" button pressed.
	/// </summary>
	[RelayCommand]
	private void YesButtonPressed()
	{
		if (!AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			DialogHost.Close(null);
		}

		_source.TrySetResult(YesNoCancelResult.Yes);
	}
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

		return GetResultAsync(YesNoCancelResult.Cancel, token);
	}
	#endregion
}
