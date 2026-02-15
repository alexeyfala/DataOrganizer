using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.Views;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="MultilineTextEditView" />.
/// </summary>
public sealed partial class MultilineTextEditViewModel : AsyncResultViewModelBase<bool>
{
	#region Auto-Generated Properties
	/// <summary>
	/// Text.
	/// </summary>
	[ObservableProperty]
	private string? _text;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Cancel.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => SetResultAsync(false);

	/// <summary>
	/// Save.
	/// </summary>
	[RelayCommand]
	private Task Save() => SetResultAsync(true);
	#endregion

	#region Methods
	/// <inheritdoc cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />
	public Task<bool> GetResultAsync(CancellationToken token = default)
	{
		return GetResultAsync(defaultResult: false, token);
	}
	#endregion
}
