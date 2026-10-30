using Clipboard.Interfaces;
using FileContents.Enums;
using FileContents.Helpers;
using Hierarchy.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace Clipboard.Services;

/// <summary>
/// Copies the text of a file to the clipboard and tells why a copy failed, each heir through its own channel.
/// </summary>
public abstract class ContentCopierBase
{
	#region Data
	/// <inheritdoc cref="IFileTextCopier" />
	private readonly IFileTextCopier _fileTextCopier;
	#endregion

	#region Constructors
	protected ContentCopierBase(IFileTextCopier fileTextCopier) => _fileTextCopier = fileTextCopier;
	#endregion

	#region Methods
	/// <summary>
	/// Copies the text of a file under a header; <c>true</c> when it reached the clipboard.
	/// </summary>
	protected async Task<bool> CopyTextAsync(
		FileDto file,
		string header,
		CancellationToken token)
	{
		FileTextStatus status = await _fileTextCopier
			.CopyAsync(file, header, token)
			.ConfigureAwait(true);

		return FileTextStatusRules.HoldsText(
			status,
			() => ReportDamaged(file),
			() => ReportNotText(file),
			() => ReportEmpty(file));
	}

	/// <summary>
	/// Tells that the contents of a file could not be read.
	/// </summary>
	protected abstract void ReportDamaged(FileDto file);

	/// <summary>
	/// Tells that a file has no contents to copy.
	/// </summary>
	protected abstract void ReportEmpty(FileDto file);

	/// <summary>
	/// Tells that the contents of a file are not text.
	/// </summary>
	protected abstract void ReportNotText(FileDto file);
	#endregion
}
