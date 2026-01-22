using DataOrganizer.Abstract;
using DataOrganizer.Views;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="YesNoQuestionBox" />.
/// </summary>
public sealed class YesNoQuestionBoxViewModel : TextWithDefaultButtonViewModelBase
{
	#region Methods
	/// <inheritdoc />
	protected override bool CanExecuteDefaultPressed() => true;
	#endregion
}
