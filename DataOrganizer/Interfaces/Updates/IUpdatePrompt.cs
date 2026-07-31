using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Updates;

/// <summary>
/// Presents the update-available confirmation to the user.
/// </summary>
public interface IUpdatePrompt
{
	#region Methods
	/// <summary>
	/// Asks whether to open the page of a newer application version.
	/// </summary>
	Task<bool> ConfirmUpdateAsync(string text, CancellationToken token = default);
	#endregion
}
