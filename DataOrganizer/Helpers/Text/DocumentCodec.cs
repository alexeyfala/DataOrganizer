using DataOrganizer.Dto.Documents;
using DataOrganizer.Extensions;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Encoding that the text of a document is stored in: reads the text from its bytes and gives the bytes of the text.
/// </summary>
internal sealed class DocumentCodec
{
	#region Properties
	/// <summary>
	/// Web name of the encoding found from the bytes when they were read; <c>null</c> when they are not found to be text.
	/// </summary>
	public string? DefaultEncoding { get; private set; }

	/// <summary>
	/// Web name of the encoding the text is read in and written in; <c>null</c> until the bytes are read.
	/// </summary>
	public string? Encoding => _encoding?.Encoding.WebName;

	/// <summary>
	/// Name of <see cref="Encoding" /> as the status bar shows it; <c>null</c> until the bytes are read.
	/// </summary>
	public string? EncodingName => _encoding?.Name;

	/// <summary>
	/// <c>True</c> while the text holds a character that <see cref="Encoding" /> does not have, so it has no bytes.
	/// </summary>
	public bool IsTextOutsideEncoding { get; private set; }

	/// <summary>
	/// Web name of the encoding to keep in the editor state; <c>null</c> while the text takes the one found from the bytes.
	/// </summary>
	public string? StoredEncoding => Encoding == DefaultEncoding ? null : Encoding;
	#endregion

	#region Data
	/// <summary>
	/// Encoding of the bytes together with their byte order mark; <c>null</c> until they are read.
	/// </summary>
	private FileEncoding? _encoding;
	#endregion

	#region Methods
	/// <summary>
	/// Returns the bytes of a text in <see cref="Encoding" />, with the byte order mark; <c>null</c> until the bytes are
	/// read, or when a character of the text is not in the encoding, and then <paramref name="missingCharacter" /> holds it.
	/// </summary>
	public byte[]? Encode(string text, out string? missingCharacter)
	{
		if (_encoding is not { } encoding)
		{
			missingCharacter = null;

			return null;
		}

		byte[]? contents = FileTextCodec.TryEncode(text, encoding, out missingCharacter);

		IsTextOutsideEncoding = contents is null;

		return contents;
	}

	/// <summary>
	/// Returns the web names of the encodings that cannot read the bytes of a text; <c>null</c> when the text has no bytes
	/// in <see cref="Encoding" />.
	/// </summary>
	public IReadOnlySet<string>? FindUnreadableEncodings(string text)
	{
		// Once written, the text in its encoding gives the stored bytes, which another encoding reads again. A text that
		// the encoding cannot hold has no such bytes, and a choice keeps the encoding then.
		if (_encoding is not { } encoding || FileTextCodec.TryEncode(text, encoding, out _) is not { } contents)
		{
			return null;
		}

		try
		{
			return FileTextCodec.FindUnreadableEncodings(contents);
		}
		finally
		{
			contents.ZeroMemory();
		}
	}

	/// <summary>
	/// Returns the text of stored bytes in the encoding chosen by its web name, or in the one found from the bytes when
	/// there is no choice or it cannot read them; <c>null</c> when they are not text.
	/// </summary>
	public string? Read(ReadOnlySpan<byte> contents, string? chosenEncoding)
	{
		if (FileTextCodec.TryRead(contents, chosenEncoding) is not { } read)
		{
			return null;
		}

		// The mark belongs to the bytes rather than to the text: it stays out of the text and returns with its bytes.
		_encoding = read.Encoding;

		DefaultEncoding = FileTextCodec
			.Detect(contents)?
			.Encoding
			.WebName;

		return read.Text;
	}

	/// <summary>
	/// Reads a text again from its bytes in another encoding, chosen by its web name, which the text takes when it reads
	/// them; <c>null</c> when the text has no bytes in <see cref="Encoding" />.
	/// </summary>
	public EncodingChange? Reread(string text, string chosenEncoding)
	{
		// Once written, the text in its encoding gives the stored bytes, so they need not be read again.
		if (_encoding is not { } current || FileTextCodec.TryEncode(text, current, out _) is not { } contents)
		{
			return null;
		}

		try
		{
			FileEncoding? chosen = FileTextCodec.Choose(contents, chosenEncoding);

			if (chosen is null || FileTextCodec.TryDecode(contents, chosen) is not { } reread)
			{
				return new()
				{
					Name = chosen?.Name ?? chosenEncoding,
					Text = null
				};
			}

			_encoding = chosen;

			return new()
			{
				Name = chosen.Name,
				Text = reread
			};
		}
		finally
		{
			contents.ZeroMemory();
		}
	}
	#endregion
}
