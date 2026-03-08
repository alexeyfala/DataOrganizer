using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.Enums;
using DataOrganizer.Views;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="ImportListSelectorView" />.
/// </summary>
internal sealed partial class ImportListSelectorViewModel : AsyncResultViewModelBase<ImportListVariant>
{
	#region Auto-Generated Properties
	/// <summary>
	/// Add to the list.
	/// </summary>
	[ObservableProperty]
	private bool _addToList;

	/// <summary>
	/// Header.
	/// </summary>
	[ObservableProperty]
	private string? _header;

	/// <summary>
	/// Replace the list.
	/// </summary>
	[ObservableProperty]
	private bool _replace = true;
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
			return SetResultAsync(ImportListVariant.AddToList);
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
	public ImportListSelectorViewModel(Application app) : base(app)
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
