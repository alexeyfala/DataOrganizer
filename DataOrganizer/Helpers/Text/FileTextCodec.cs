using DataOrganizer.Dto.Documents;
using DataOrganizer.Extensions;
using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Unicode;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Reading of the text of a file from its bytes and writing it back in the encoding of the bytes.
/// </summary>
internal static class FileTextCodec
{
	#region Properties
	/// <summary>
	/// Encodings that a text can be read in, as a list offers them: Unicode first, then the others by description.
	/// </summary>
	public static IReadOnlyList<SelectorChoice> EncodingChoices { get; } = CreateEncodingChoices();

	/// <summary>
	/// Encoding of the bytes that have no byte order mark and are not UTF-8: the code page of programs without Unicode.
	/// </summary>
	public static Encoding Fallback { get; } = FindFallback();
	#endregion

	#region Data
	/// <summary>
	/// Code page of UTF-8, which cannot read the bytes that need a fallback.
	/// </summary>
	private const int Utf8CodePage = 65001;

	/// <summary>
	/// Code page of Windows for Western European languages, the fallback where no other is known.
	/// </summary>
	private const int WesternCodePage = 1252;

	/// <summary>
	/// Control characters that a text does not hold: all of them but the tab, the line breaks, the form feed, the end of
	/// file mark of DOS and the escape of terminal colors.
	/// </summary>
	private static readonly SearchValues<byte> BinaryControls = SearchValues.Create(
	[
		0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
		0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19,
		0x1C, 0x1D, 0x1E, 0x1F
	]);

	/// <summary>
	/// Encodings of <see cref="EncodingChoices" />, by web name.
	/// </summary>
	private static readonly FrozenDictionary<string, Encoding> Encodings = GetEncodingInfos()
		.Select(static x => x.GetEncoding())
		.ToFrozenDictionary(
			static x => x.WebName,
			StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Encodings that a byte order mark tells; the mark of UTF-32 LE starts with that of UTF-16 LE, so it comes first.
	/// </summary>
	private static readonly Encoding[] MarkedEncodings =
	[
		Encoding.UTF8,
		Encoding.UTF32,
		new UTF32Encoding(bigEndian: true, byteOrderMark: true),
		Encoding.Unicode,
		Encoding.BigEndianUnicode
	];
	#endregion

	#region Methods
	/// <summary>
	/// Returns an encoding of <see cref="EncodingChoices" /> by its web name, with the byte order mark the bytes of a file
	/// start with; <c>null</c> for a name the list does not hold.
	/// </summary>
	public static FileEncoding? Choose(ReadOnlySpan<byte> contents, string name)
	{
		if (!Encodings.TryGetValue(name, out Encoding? encoding))
		{
			return null;
		}

		// The mark comes from the bytes, so the list offers each Unicode encoding once.
		return new FileEncoding
		{
			Encoding = encoding,
			HasByteOrderMark = !encoding.Preamble.IsEmpty && contents.StartsWith(encoding.Preamble)
		};
	}

	/// <summary>
	/// Returns the encoding of the bytes of a file, with <see cref="Fallback" /> for the ones that are neither marked nor
	/// UTF-8; <c>null</c> when they are not text.
	/// </summary>
	public static FileEncoding? Detect(ReadOnlySpan<byte> contents) => Detect(contents, Fallback);

	/// <summary>
	/// Returns the encoding of the bytes of a file, with a fallback for the ones that are neither marked nor UTF-8;
	/// <c>null</c> when they are not text.
	/// </summary>
	public static FileEncoding? Detect(ReadOnlySpan<byte> contents, Encoding fallback)
	{
		foreach (Encoding encoding in MarkedEncodings)
		{
			if (contents.StartsWith(encoding.Preamble))
			{
				return new FileEncoding
				{
					Encoding = encoding,
					HasByteOrderMark = true
				};
			}
		}

		// A zero byte is valid in most encodings, yet in bytes without a mark it means binary data or UTF-16 text.
		if (contents.Contains((byte)0))
		{
			return null;
		}

		if (Utf8.IsValid(contents))
		{
			return new FileEncoding
			{
				Encoding = Encoding.UTF8,
				HasByteOrderMark = false
			};
		}

		// A code page reads any bytes, so only control characters tell binary data from its text.
		if (contents.ContainsAny(BinaryControls))
		{
			return null;
		}

		return new FileEncoding
		{
			Encoding = fallback,
			HasByteOrderMark = false
		};
	}

	/// <summary>
	/// Returns the text of the bytes of a file without the byte order mark; <c>null</c> when the encoding cannot read them
	/// or would not write them back the same.
	/// </summary>
	public static string? TryDecode(ReadOnlySpan<byte> contents, FileEncoding encoding)
	{
		Encoding strict = CreateStrict(encoding.Encoding);

		ReadOnlySpan<byte> preamble = encoding.HasByteOrderMark ? strict.Preamble : [];

		if (!contents.StartsWith(preamble))
		{
			return null;
		}

		ReadOnlySpan<byte> body = contents[preamble.Length..];

		string text;

		byte[] written;

		try
		{
			text = strict.GetString(body);

			written = strict.GetBytes(text);
		}
		catch (Exception ex) when (ex is DecoderFallbackException or EncoderFallbackException)
		{
			return null;
		}

		// Some code pages read several byte sequences as one character, so a save would change the bytes.
		try
		{
			return written.AsSpan().SequenceEqual(body) ? text : null;
		}
		finally
		{
			written.ZeroMemory();
		}
	}

	/// <summary>
	/// Returns the bytes of a text in the encoding of a file, with its byte order mark; <c>null</c> when a character of the
	/// text is not in the encoding, and then <paramref name="missingCharacter" /> holds it.
	/// </summary>
	public static byte[]? TryEncode(string text, FileEncoding encoding, out string? missingCharacter)
	{
		Encoding strict = CreateStrict(encoding.Encoding);

		ReadOnlySpan<byte> preamble = encoding.HasByteOrderMark ? strict.Preamble : [];

		try
		{
			byte[] contents = new byte[preamble.Length + strict.GetByteCount(text)];

			preamble.CopyTo(contents);

			strict.GetBytes(text, contents.AsSpan(preamble.Length));

			missingCharacter = null;

			return contents;
		}
		catch (EncoderFallbackException ex)
		{
			// A character outside the basic plane comes as the two halves of its surrogate pair.
			missingCharacter = ex.IsUnknownSurrogate()
				? new string([ex.CharUnknownHigh, ex.CharUnknownLow])
				: ex.CharUnknown.ToString();

			return null;
		}
	}

	/// <summary>
	/// Returns the text of the bytes of a file in the encoding chosen by its web name, or in the one found from the bytes
	/// when there is no choice or it cannot read them; <c>null</c> when they are not text.
	/// </summary>
	public static FileText? TryRead(ReadOnlySpan<byte> contents, string? chosenEncoding)
	{
		FileEncoding? chosen = chosenEncoding is null ? null : Choose(contents, chosenEncoding);

		return ReadIn(contents, chosen) ?? ReadIn(contents, Detect(contents));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the choice of an encoding, named as the status bar names it and found by its code page and web name.
	/// </summary>
	private static SelectorChoice CreateEncodingChoice(EncodingInfo info)
	{
		Encoding encoding = info.GetEncoding();

		FileEncoding unmarked = new()
		{
			Encoding = encoding,
			HasByteOrderMark = false
		};

		return new()
		{
			Description = info.DisplayName,
			Id = encoding.WebName,
			Name = unmarked.Name,
			SearchTerms = [info.CodePage.ToString(CultureInfo.InvariantCulture), encoding.WebName]
		};
	}

	/// <summary>
	/// Returns the choices of the encodings that a text can be read in: Unicode first, then the others by description.
	/// </summary>
	private static SelectorChoice[] CreateEncodingChoices()
	{
		EncodingInfo[] infos = GetEncodingInfos();

		// Only the Unicode encodings have a byte order mark; UTF-8 leads them, and the others follow by code page.
		IEnumerable<EncodingInfo> unicode = infos
			.Where(static x => !x.GetEncoding().Preamble.IsEmpty)
			.OrderBy(static x => x.CodePage == Utf8CodePage ? 0 : x.CodePage);

		IEnumerable<EncodingInfo> others = infos
			.Where(static x => x.GetEncoding().Preamble.IsEmpty)
			.OrderBy(static x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
			.ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase);

		return [.. unicode
			.Concat(others)
			.Select(CreateEncodingChoice)];
	}

	/// <summary>
	/// Returns a copy of an encoding that throws on bytes it cannot read and on characters it cannot write, rather than
	/// putting replacements or look-alikes in their place.
	/// </summary>
	private static Encoding CreateStrict(Encoding encoding)
	{
		Encoding strict = (Encoding)encoding.Clone();

		strict.DecoderFallback = DecoderFallback.ExceptionFallback;

		strict.EncoderFallback = EncoderFallback.ExceptionFallback;

		return strict;
	}

	/// <summary>
	/// Returns the code page of programs without Unicode: the one of the system on Windows and the one of the region on
	/// macOS and Linux.
	/// </summary>
	private static Encoding FindFallback()
	{
		EncodingProvider provider = CodePagesEncodingProvider.Instance;

		// The system may run such programs in UTF-8, which cannot read the bytes that need a fallback.
		if (OperatingSystem.IsWindows() && provider.GetEncoding(0) is { CodePage: not Utf8CodePage } system)
		{
			return system;
		}

		// The application changes only the language of its interface, so the culture keeps the region of the system.
		int regionCodePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;

		return (regionCodePage > 0 ? provider.GetEncoding(regionCodePage) : null)
			?? provider.GetEncoding(WesternCodePage)!;
	}

	/// <summary>
	/// Returns the encodings of .NET and the code pages of Windows that come with it.
	/// </summary>
	private static EncodingInfo[] GetEncodingInfos()
	{
		return [.. Encoding
			.GetEncodings()
			.Concat(CodePagesEncodingProvider.Instance.GetEncodings())
			.DistinctBy(static x => x.CodePage)];
	}

	/// <summary>
	/// Returns the text of the bytes of a file in an encoding; <c>null</c> without an encoding or when it cannot read them.
	/// </summary>
	private static FileText? ReadIn(ReadOnlySpan<byte> contents, FileEncoding? encoding)
	{
		if (encoding is null || TryDecode(contents, encoding) is not { } text)
		{
			return null;
		}

		return new()
		{
			Encoding = encoding,
			Text = text
		};
	}
	#endregion
}
