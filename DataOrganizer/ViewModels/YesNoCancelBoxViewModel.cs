using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Enums;
using DataOrganizer.Views;
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
	/// Returns <c>True</c> if "No" button is visible.
	/// </summary>
	[ObservableProperty]
	private bool _noButtonVisible;

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
	private void CancelButtonPressed() => _source.SetResult(YesNoCancelResult.Cancel);

	/// <summary>
	/// Handles "No" button pressed.
	/// </summary>
	[RelayCommand]
	private void NoButtonPressed() => _source.SetResult(YesNoCancelResult.No);

	/// <summary>
	/// Handles "Yes" button pressed.
	/// </summary>
	[RelayCommand]
	private void YesButtonPressed() => _source.SetResult(YesNoCancelResult.Yes);
	#endregion

	#region Data
	/// <inheritdoc cref="TaskCompletionSource" />
	private readonly TaskCompletionSource<YesNoCancelResult> _source = new();
	#endregion

	#region Methods
	/// <summary>
	/// Returns a result of the user choice.
	/// </summary>
	public Task<YesNoCancelResult> GetResultAsync(in YesNoCancelVariant variant)
	{
		NoButtonVisible = variant == YesNoCancelVariant.YesNo;

		CancelButtonVisible = variant == YesNoCancelVariant.YesNoCancel;

		return _source.Task;
	}
	#endregion
}
