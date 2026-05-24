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
	/// Enumerates applications that can open <paramref name="filePath" /> and shows the
	/// picker dialog. Returns the selected <see cref="AssociatedAppInfo" /> or
	/// <c>null</c> when the user cancels or no candidates exist on the current OS.
	/// </summary>
	Task<AssociatedAppInfo?> PickAppAsync(string filePath, CancellationToken token = default);
	#endregion
}
