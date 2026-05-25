using DataOrganizer.DTO;
using DataOrganizer.Enums;
using Repository.DTO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;


/// <summary>
/// Provides dialog boxes for interaction.
/// </summary>
public interface IDialogService
{
	#region Methods
	/// <summary>
	/// Displays the hotkey editor.
	/// </summary>
	Task<EditingHotkeysResult> EditHotkeysAsync(IEnumerable<CodeMaskPair> initialHotkeys);

	/// <summary>
	/// Shows the "open with" picker with <paramref name="candidates" /> and returns the
	/// chosen application, or <c>null</c> when the user cancels.
	/// </summary>
	Task<AssociatedAppInfo?> PickAppAsync(
		IEnumerable<AssociatedAppInfo> candidates,
		CancellationToken token = default);

	/// <summary>
	/// Requests the user to close files.
	/// </summary>
	Task<bool> RequestCloseFilesAsync(CancellationToken token = default);

	/// <summary>
	/// Requests the user to enter a string key and optionally a value.
	/// </summary>
	Task<StringKeyValuePair?> RequestKeyValueInputAsync(
		KeyValueInputParameters parameters,
		CancellationToken token = default);

	/// <summary>
	/// Requests the user to enter a multiline text.
	/// </summary>
	Task<ValueIsValidPair> RequestMultilineTextAsync(string? text, CancellationToken token = default);

	/// <summary>
	/// Requests a password from user.
	/// </summary>
	Task<char[]> RequestPasswordAsync(
		string header,
		string? label = null,
		CancellationToken token = default);

	/// <summary>
	/// Asks a question with options <see cref="YesNoCancelVariant.YesCancel" />,
	/// returns <c>True</c> if the answer was <see cref="YesNoCancelResult.Yes" />.
	/// </summary>
	Task<bool> RequestYesCancelDialogAsync(string text, CancellationToken token = default);

	/// <summary>
	/// Asks a question with options <see cref="YesNoCancelVariant.YesNo" />,
	/// returns <c>True</c> if the answer was <see cref="YesNoCancelResult.Yes" />.
	/// </summary>
	Task<bool> RequestYesNoDialogAsync(string text, CancellationToken token = default);

	/// <summary>
	/// Selects import variant.
	/// </summary>
	Task<ImportListVariant> SelectImportVariantAsync(CancellationToken token = default);

	/// <summary>
	/// Displays the entity creation dialog.
	/// </summary>
	Task<EntityCreationResult?> ShowEntityCreationAsync(CancellationToken token = default);

	/// <summary>
	/// Shows a properties dialog.
	/// </summary>
	void ShowProperties(IEnumerable<PropertyNameValuePair> properties);

	/// <summary>
	/// Shows application settings.
	/// </summary>
	Task<ShowSettingsResult> ShowSettingsAsync();
	#endregion
}
