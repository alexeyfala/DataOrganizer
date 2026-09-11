using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels.Dialogs;

/// <summary>
/// View model for <c>ImportListSelectorView</c>.
/// </summary>
internal sealed partial class ImportListSelectorViewModel : AsyncResultViewModelBase<ImportMode>
{
	#region Properties
	/// <summary>
	/// Add to the list.
	/// </summary>
	[ObservableProperty]
	public partial bool AddToList { get; set; }

	/// <summary>
	/// Text shown above the import options.
	/// </summary>
	[ObservableProperty]
	public partial string? Header { get; set; }

	/// <summary>
	/// <c>True</c> when the imported entries replace the current list.
	/// </summary>
	[ObservableProperty]
	public partial bool Replace { get; set; } = true;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes the dialog with the selected import mode.
	/// </summary>
	[RelayCommand]
	private Task Apply()
	{
		if (Replace)
		{
			return SetResultAsync(ImportMode.Replace);
		}

		if (AddToList)
		{
			return SetResultAsync(ImportMode.Append);
		}

		return SetResultAsync(ImportMode.None);
	}

	/// <summary>
	/// Closes the dialog without importing.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => SetResultAsync(ImportMode.None);
	#endregion

	#region Constructors
	public ImportListSelectorViewModel(
		Application app,
		ITaskExceptionHandler exceptionHandler) : base(app, exceptionHandler)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />
	public Task<ImportMode> GetResultAsync(CancellationToken token = default)
	{
		return GetResultAsync(
			defaultResult: ImportMode.None,
			token: token);
	}
	#endregion
}
