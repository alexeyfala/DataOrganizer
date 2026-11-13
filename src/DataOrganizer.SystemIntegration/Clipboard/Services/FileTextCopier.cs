using DataOrganizer.Documents.FileContents.Dto;
using DataOrganizer.Documents.FileContents.Enums;
using DataOrganizer.Documents.FileContents.Interfaces;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.Enums;
using DataOrganizer.SystemIntegration.Clipboard.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.Clipboard.Services;

/// <inheritdoc cref="IFileTextCopier" />
public sealed class FileTextCopier : IFileTextCopier
{
	#region Data
	/// <inheritdoc cref="IClipboardTextWriter" />
	private readonly IClipboardTextWriter _clipboardWriter;

	/// <inheritdoc cref="IFileTextReader" />
	private readonly IFileTextReader _fileTextReader;
	#endregion

	#region Constructors
	public FileTextCopier(IClipboardTextWriter clipboardWriter, IFileTextReader fileTextReader)
	{
		_clipboardWriter = clipboardWriter;

		_fileTextReader = fileTextReader;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<FileTextStatus> CopyAsync(
		FileDto file,
		string header,
		CancellationToken token = default)
	{
		FileTextReading reading = await _fileTextReader
			.ReadAsync(file, header, token)
			.ConfigureAwait(false);

		if (reading.Status != FileTextStatus.Text)
		{
			return reading.Status;
		}

		await _clipboardWriter
			.WriteTextAsync(reading.Text, isSensitive: file.EncryptionStatus != EncryptionStatus.None)
			.ConfigureAwait(false);

		return reading.Status;
	}
	#endregion
}
