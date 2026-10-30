using Hierarchy.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace GlobalHotkeys.Interfaces;

/// <summary>
/// Copies the contents of a file whose hotkey was typed, tells how the copy went and puts a done copy into the copy
/// history.
/// </summary>
public interface IHotkeyContentCopier
{
	#region Methods
	/// <summary>
	/// Copies the contents of a file, bringing back the main window first for the password of a locked keeper.
	/// </summary>
	Task CopyAsync(FileDto file, CancellationToken token = default);
	#endregion
}
