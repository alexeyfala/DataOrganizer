using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions.Hotkeys;
using DataOrganizer.Interfaces.Hotkeys;
using Serilog;
using Shared.Extensions;
using System.Collections.Generic;

namespace DataOrganizer.Services.Hotkeys;

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
