using DataOrganizer.Dto.Execution;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Dialogs;

/// <summary>
/// Asks the user to pick an application from a list.
/// </summary>
public interface IAppPickerPrompt
{
	#region Methods
	/// <summary>
	/// Shows the "open with" picker with <paramref name="candidates" /> and returns the
	/// chosen application, or <c>null</c> when the user cancels.
	/// </summary>
	Task<AssociatedAppInfo?> PickAppAsync(
		IEnumerable<AssociatedAppInfo> candidates,
		CancellationToken token = default);
	#endregion
}
