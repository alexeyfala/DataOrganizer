using DataOrganizer.DTO;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Bridge between OS-level "open with" candidate enumeration and the UI dialog
/// that lets the user pick one of the candidates.
/// </summary>
public interface IAppPickerService
{
	#region Methods
	/// <summary>
	/// Builds an <see cref="AssociatedAppInfo" /> from a user-chosen executable path
	/// (typically the result of a "Browse for another app" file picker). Returns
	/// <c>null</c> when <paramref name="appPath" /> is empty.
	/// </summary>
	AssociatedAppInfo? CreateFromPath(string appPath);

	/// <summary>
	/// Enumerates applications that can open <paramref name="filePath" /> and shows the
	/// picker dialog. Returns the selected <see cref="AssociatedAppInfo" /> or
	/// <c>null</c> when the user cancels or no candidates exist on the current OS.
	/// </summary>
	Task<AssociatedAppInfo?> PickAppAsync(string filePath, CancellationToken token = default);
	#endregion
}
