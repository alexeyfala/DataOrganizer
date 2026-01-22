using DataOrganizer.Abstract;
using DataOrganizer.Views;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="MultilineTextEditView" />.
/// </summary>
public sealed class MultilineTextEditViewModel : TextWithDefaultButtonViewModelBase
{
	#region Methods
	/// <inheritdoc cref="" />
	protected override bool CanExecuteDefaultPressed() => true;
	#endregion
}
