using System;
using System.Text;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Encoding of the bytes of a file, together with the byte order mark they may start with.
/// </summary>
public sealed record FileEncoding
{
	#region Properties
	/// <summary>
	/// Encoding of the text that follows the byte order mark.
	/// </summary>
	public required Encoding Encoding { get; init; }

	/// <summary>
	/// <c>True</c> when the bytes start with the byte order mark of <see cref="Encoding" />.
	/// </summary>
	public required bool HasByteOrderMark { get; init; }

	/// <summary>
	/// Name of the encoding as the status bar shows it, with the byte order mark noted, as in Notepad++.
	/// </summary>
	public string Name => Encoding.CodePage switch
	{
		Utf8CodePage => HasByteOrderMark ? "UTF-8-BOM" : "UTF-8",
		Utf16CodePage => WithMark("UTF-16 LE"),
		Utf16BigEndianCodePage => WithMark("UTF-16 BE"),
		Utf32CodePage => WithMark("UTF-32 LE"),
		Utf32BigEndianCodePage => WithMark("UTF-32 BE"),
		_ => Encoding.WebName.StartsWith(WindowsPrefix, StringComparison.Ordinal)
			? $"Windows-{Encoding.WebName[WindowsPrefix.Length..]}"
			: Encoding.WebName.ToUpperInvariant()
	};
	#endregion

	#region Data
	/// <summary>
	/// Code page of UTF-16 in big-endian order.
	/// </summary>
	private const int Utf16BigEndianCodePage = 1201;

	/// <summary>
	/// Code page of UTF-16 in little-endian order.
	/// </summary>
	private const int Utf16CodePage = 1200;

	/// <summary>
	/// Code page of UTF-32 in big-endian order.
	/// </summary>
	private const int Utf32BigEndianCodePage = 12001;

	/// <summary>
	/// Code page of UTF-32 in little-endian order.
	/// </summary>
	private const int Utf32CodePage = 12000;

	/// <summary>
	/// Code page of UTF-8.
	/// </summary>
	private const int Utf8CodePage = 65001;

	/// <summary>
	/// Start of the web names of the code pages of Windows, which their usual names write with a capital letter.
	/// </summary>
	private const string WindowsPrefix = "windows-";
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the name of a Unicode encoding with the byte order mark noted.
	/// </summary>
	private string WithMark(string name) => HasByteOrderMark ? $"{name} BOM" : name;
	#endregion
}
