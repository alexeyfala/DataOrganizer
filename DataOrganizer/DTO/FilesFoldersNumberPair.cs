using Cysharp.Text;
using Shared.Properties;
using System.Diagnostics;

namespace DataOrganizer.DTO;

/// <summary>
/// The number of <see cref="Files" /> and <see cref="Folders" />.
/// </summary>
[DebuggerDisplay($"{nameof(Files)} = {{{nameof(Files)}}}, {nameof(Folders)} = {{{nameof(Folders)}}}")]
internal readonly struct FilesFoldersNumberPair
{
	#region Properties
	/// <summary>
	/// Number of files.
	/// </summary>
	public required uint Files { get; init; }

	/// <summary>
	/// Number of folders.
	/// </summary>
	public required uint Folders { get; init; }
	#endregion Properties

	#region Methods
	/// <summary>
	/// Creates a string with information about the number of objects.
	/// </summary>
	public string AsString()
	{
		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		builder.Append(Strings.Folders);

		builder.Append(':');

		builder.Append(' ');

		builder.Append(Folders);

		builder.Append("  ");

		builder.Append(Strings.Files);

		builder.Append(':');

		builder.Append(' ');

		builder.Append(Files);

		return builder.ToString();
	}
	#endregion
}
