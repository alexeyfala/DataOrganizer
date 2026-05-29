using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>ImportListSelectorView</c>.
/// </summary>
internal sealed partial class ImportListSelectorViewModel : AsyncResultViewModelBase<ImportListVariant>
{
	#region Properties
	/// <summary>
	/// Add to the list.
	/// </summary>
	[ObservableProperty]
	public partial bool AddToList { get; set; }

	/// <summary>
	/// Header.
	/// </summary>
	[ObservableProperty]
	public partial string? Header { get; set; }

	/// <summary>
	/// Replace the list.
	/// </summary>
	[ObservableProperty]
	public partial bool Replace { get; set; } = true;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Apply.
	/// </summary>
	[RelayCommand]
	private Task Apply()
	{
		if (Replace)
		{
			return SetResultAsync(ImportListVariant.Replace);
		}

		if (AddToList)
		{
			return SetResultAsync(ImportListVariant.Append);
		}

		return SetResultAsync(ImportListVariant.None);
	}

	/// <summary>
	/// Cancel.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => SetResultAsync(ImportListVariant.None);
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
	public Task<ImportListVariant> GetResultAsync(CancellationToken token = default)
	{
		return GetResultAsync(
			defaultResult: ImportListVariant.None,
			token: token);
	}
	#endregion
}
