using DataOrganizer.Dto.Dialogs;
using Repository.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Dialogs;

/// <summary>
/// Provides dialog boxes for interaction.
/// </summary>
public interface IDialogService :
	IAppPickerPrompt,
	IConfirmationPrompt,
	IImportModePrompt,
	IPasswordPrompt
{
	#region Methods
	/// <summary>
	/// Displays the hotkey editor.
	/// </summary>
	Task<EditingHotkeysResult> EditHotkeysAsync(IEnumerable<KeyStroke> initialHotkeys);

	/// <summary>
	/// Requests the user to close files.
	/// </summary>
	Task<bool> RequestCloseFilesAsync(CancellationToken token = default);

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

	/// <summary>
	/// Displays the entity creation dialog.
	/// </summary>
	Task<EntityCreationResult?> ShowEntityCreationAsync(CancellationToken token = default);

	/// <summary>
	/// Shows a properties dialog.
	/// </summary>
	void ShowProperties(IEnumerable<PropertyDescription> properties);

	/// <summary>
	/// Shows application settings.
	/// </summary>
	Task<ShowSettingsResult> ShowSettingsAsync();
	#endregion
}
