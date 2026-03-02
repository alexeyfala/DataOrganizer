using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.Enums;
using DataOrganizer.Views;
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
	public YesNoCancelBoxViewModel(Application app) : base(app)
	{
	}
	#endregion
}
