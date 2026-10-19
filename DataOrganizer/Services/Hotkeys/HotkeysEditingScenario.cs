using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions.Hotkeys;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Runtime;
using Serilog;
using Shared.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Hotkeys;

public sealed class HotkeysEditingScenario : IHotkeysEditingScenario
{
	#region Data
	/// <inheritdoc cref="IBackgroundServiceControl" />
	private readonly IBackgroundServiceControl _backgroundServiceControl;

	/// <inheritdoc cref="IFileHotkeyEditor" />
	private readonly IFileHotkeyEditor _fileHotkeyEditor;

	/// <inheritdoc cref="IHotkeysPrompt" />
	private readonly IHotkeysPrompt _hotkeysPrompt;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public HotkeysEditingScenario(
		IBackgroundServiceControl backgroundServiceControl,
		IFileHotkeyEditor fileHotkeyEditor,
		IHotkeysPrompt hotkeysPrompt,
		ILogger logger)
	{
		_backgroundServiceControl = backgroundServiceControl;

		_fileHotkeyEditor = fileHotkeyEditor;

		_hotkeysPrompt = hotkeysPrompt;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task EditAsync(FileDto file, IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		_logger.LogInformation("Show hotkeys editor");

		await _backgroundServiceControl
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

		_backgroundServiceControl.ResumeHotkeyTracking();
	}
	#endregion
}
