using CommunityToolkit.Mvvm.Input;
using DialogContracts.Interfaces;
using Repository.Dto;
using System.Collections.ObjectModel;

namespace DataOrganizer.ViewModels.Dialogs;

/// <summary>
/// View model for <c>PropertiesView</c>.
/// </summary>
internal sealed partial class PropertiesViewModel
{
	#region Properties
	/// <summary>
	/// The list of properties.
	/// </summary>
	public ObservableCollection<PropertyDescription> Properties { get; } = [];
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes the dialog.
	/// </summary>
	[RelayCommand]
	private void Close() => _dialogHostCloser.Close();
	#endregion

	#region Data
	/// <inheritdoc cref="IDialogHostCloser" />
	private readonly IDialogHostCloser _dialogHostCloser;
	#endregion

	#region Constructors
	public PropertiesViewModel(IDialogHostCloser dialogHostCloser) => _dialogHostCloser = dialogHostCloser;
	#endregion
}
