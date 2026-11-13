using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Shared.Interfaces;

/// <summary>
/// Replaces the contents of files so that a write which does not reach the disk leaves the previous contents untouched.
/// </summary>
public interface IAtomicFileWriter
{
	#region Methods
	/// <inheritdoc cref="WriteAllBytesAsync" />
	void WriteAllBytes(string filePath, byte[] bytes);

	/// <summary>
	/// Writes bytes into a temporary file and puts it in the place of <paramref name="filePath" />.
	/// </summary>
	Task WriteAllBytesAsync(
		string filePath,
		byte[] bytes,
		CancellationToken token = default);
	#endregion
}
