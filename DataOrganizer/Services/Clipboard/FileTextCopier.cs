using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Documents;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

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
