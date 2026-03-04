using Avalonia;
using DataOrganizer.Abstract;
using DataOrganizer.Enums;
using DataOrganizer.Views;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="ImportListSelectorView" />.
/// </summary>
internal sealed class ImportListSelectorViewModel : AsyncResultViewModelBase<ImportListVariant>
{
	#region Constructors
	public ImportListSelectorViewModel(Application app) : base(app)
	{
	}
	#endregion
}
