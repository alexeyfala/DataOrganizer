using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Helpers.Views;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Dialogs;

/// <summary>
/// Asks the user to confirm an action.
/// </summary>
public interface IConfirmationPrompt
{
	#region Methods
	/// <summary>
	/// Asks a question with options <see cref="YesNoCancelButtons.YesCancel" />,
	/// returns <c>True</c> if the answer was <see cref="YesNoCancelAnswer.Yes" />.
	/// </summary>
	Task<bool> RequestYesCancelAsync(string text, CancellationToken token = default);

	/// <summary>
	/// Asks a question with options <see cref="YesNoCancelButtons.YesNo" /> in the dialog host named by
	/// <paramref name="dialogHostIdentifier" />, returns <c>True</c> if the answer was <see cref="YesNoCancelAnswer.Yes" />.
	/// </summary>
	Task<bool> RequestYesNoAsync(
		string text,
		string dialogHostIdentifier = DialogHostIdentifiers.Main,
		CancellationToken token = default);
	#endregion
}
