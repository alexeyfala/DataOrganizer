using DataOrganizer.Dto.Dialogs;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Dialogs;

/// <summary>
/// Asks the user to enter a text.
/// </summary>
public interface ITextInputPrompt
{
	#region Methods
	/// <summary>
	/// Requests the user to enter a string key and optionally a value.
	/// </summary>
	Task<KeyValueInput?> RequestKeyValueInputAsync(
		KeyValueInputParameters parameters,
		CancellationToken token = default);

	/// <summary>
	/// Requests the user to enter a multiline text; <paramref name="name" /> is put into the header of the dialog.
	/// A sensitive text is copied out of the dialog with the clipboard sensitivity markers.
	/// </summary>
	Task<TextInputResult> RequestMultilineTextAsync(
		string? text,
		string? name = null,
		bool isSensitive = false,
		CancellationToken token = default);
	#endregion
}
