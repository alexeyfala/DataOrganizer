using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Extensions.Hotkeys;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Hotkeys;
using Hierarchy.Dto;
using Serilog;
using Shared.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Hotkeys;

/// <inheritdoc cref="IHotkeysEditingScenario" />
public sealed class HotkeysEditingScenario : IHotkeysEditingScenario
{
	#region Data
	/// <inheritdoc cref="IFileHotkeyEditor" />
	private readonly IFileHotkeyEditor _fileHotkeyEditor;

	/// <inheritdoc cref="IHotkeysPrompt" />
	private readonly IHotkeysPrompt _hotkeysPrompt;

	/// <inheritdoc cref="IHotkeyTrackingControl" />
	private readonly IHotkeyTrackingControl _hotkeyTrackingControl;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public HotkeysEditingScenario(
		IFileHotkeyEditor fileHotkeyEditor,
		IHotkeysPrompt hotkeysPrompt,
		IHotkeyTrackingControl hotkeyTrackingControl,
		ILogger logger)
	{
		_fileHotkeyEditor = fileHotkeyEditor;

		_hotkeysPrompt = hotkeysPrompt;

		_hotkeyTrackingControl = hotkeyTrackingControl;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task EditAsync(FileDto file, IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		_logger.LogInformation("Show hotkeys editor");

		await _hotkeyTrackingControl
			.PauseHotkeyTrackingAsync()
			.ConfigureAwait(true);

		EditingHotkeysResult result = await _hotkeysPrompt
			.EditHotkeysAsync(file.Hotkeys.ToKeyStrokes())
			.ConfigureAwait(false);

		if (result.IsSaved)
		{
			await _fileHotkeyEditor
				.OverwriteAsync(file, result.NewHotkeys, hierarchy)
				.ConfigureAwait(false);
		}

		_hotkeyTrackingControl.ResumeHotkeyTracking();
	}
	#endregion
}
