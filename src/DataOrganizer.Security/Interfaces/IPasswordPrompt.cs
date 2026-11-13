using DataOrganizer.Security.Enums;
using DataOrganizer.Security.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Security.Interfaces;

/// <summary>
/// Asks the user for a password.
/// </summary>
public interface IPasswordPrompt
{
	#region Methods
	/// <summary>
	/// Requests a password from user; the caller owns the returned secret and disposes it.
	/// </summary>
	Task<PinnedSecret> RequestPasswordAsync(
		string header,
		string? label = null,
		string? description = null,
		PasswordPromptMode mode = PasswordPromptMode.Verify,
		CancellationToken token = default);
	#endregion
}
