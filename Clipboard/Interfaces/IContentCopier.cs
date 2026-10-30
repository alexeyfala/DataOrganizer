using Hierarchy.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace Clipboard.Interfaces;

/// <summary>
/// Copies the contents of a file to the clipboard, tells why a copy failed and puts a done copy into the copy history.
/// </summary>
public interface IContentCopier
{
	#region Methods
	/// <summary>
	/// Copies the contents of a file that is still in the database; <c>true</c> when they reached the clipboard. A shown
	/// copy history moves the file to its top at once only when <paramref name="showAtOnce" />.
	/// </summary>
	Task<bool> CopyAsync(
		FileDto file,
		bool showAtOnce,
		CancellationToken token = default);
	#endregion
}
