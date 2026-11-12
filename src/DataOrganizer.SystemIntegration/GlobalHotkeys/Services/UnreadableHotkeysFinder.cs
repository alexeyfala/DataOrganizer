using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.SystemIntegration.GlobalHotkeys.Extensions;
using DataOrganizer.SystemIntegration.GlobalHotkeys.Interfaces;
using Serilog;
using System.Collections.Generic;

namespace DataOrganizer.SystemIntegration.GlobalHotkeys.Services;

/// <inheritdoc cref="IUnreadableHotkeysFinder" />
public sealed class UnreadableHotkeysFinder : IUnreadableHotkeysFinder
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public UnreadableHotkeysFinder(ILogger logger) => _logger = logger;
	#endregion

	#region Methods
	/// <inheritdoc />
	public FileDto[] Find(IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		FileDto[] unreadable = [.. hierarchy.GetFilesWithUnreadableHotkeys()];

		// Stryker disable once Statement : only writes to the log
		unreadable.ForEach(x =>
		{
			_logger.LogError(
				$@"Hotkeys of file ""{x.Name}"" ({x.Id}) could not be read.",
				breakInDebugger: false);
		});

		return unreadable;
	}
	#endregion
}
