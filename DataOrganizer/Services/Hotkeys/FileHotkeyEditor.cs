using Comparation;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Notifications;
using Entities.Models;
using MapsterMapper;
using Repository.Dto;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Hotkeys;

public sealed class FileHotkeyEditor : IFileHotkeyEditor
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMapper" />
	private readonly IMapper _mapper;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public FileHotkeyEditor(
		IDbAccess dbAccess,
		ILogger logger,
		IMapper mapper,
		INotificationService notification)
	{
		_dbAccess = dbAccess;

		_logger = logger;

		_mapper = mapper;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<OverwriteHotkeysOutcome> OverwriteAsync(
		FileDto dto,
		KeyStroke[] newHotkeys,
		IEnumerable<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default)
	{
		IEqualityComparer<HotkeyDto> comparer = Equality.Of<HotkeyDto>()
			.By(x => x.Code)
			.AndBy(x => x.Mask);

		HotkeyDto[] newHotkeyDtos = [.. newHotkeys.ToHotkeyDtos()];

		if (dto
			.Hotkeys
			.SequenceEqual(newHotkeyDtos, comparer))
		{
			return OverwriteHotkeysOutcome.SameHotkeys;
		}

		if (newHotkeyDtos.IsNotEmpty() && hierarchy.FindFileBy(x => x.Hotkeys.SequenceEqual(newHotkeyDtos, comparer)) is { } existed)
		{
			string sequence = newHotkeys.GetHotkeysPresentation();

			_notification.ShowWarningSnackbar($@"{string.Format(Strings.HotkeysAlreadyAssignedFor, sequence)} ""{existed.Name}""");

			return OverwriteHotkeysOutcome.AlreadyInUse;
		}

		try
		{
			if (dto.Hotkeys.Count > 0)
			{
				dto
					.Hotkeys
					.Clear();

				await _dbAccess
					.DeleteHotkeysAsync(dto.Id, token)
					.ConfigureAwait(false);
			}

			if (newHotkeys.IsEmpty())
			{
				return OverwriteHotkeysOutcome.EmptySequence;
			}

			try
			{
				HotkeyEntity[] createdHotkeys = await _dbAccess
					.AddHotkeysAsync(dto.Id, newHotkeys, token)
					.ConfigureAwait(false);

				HotkeyDto[] mapped = _mapper.Map<HotkeyEntity[], HotkeyDto[]>(createdHotkeys);

				dto
					.Hotkeys
					.AddRange(mapped);

				return OverwriteHotkeysOutcome.Rewritten;
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);

				return OverwriteHotkeysOutcome.ExceptionThrown;
			}
		}
		finally
		{
			dto.SetHotkeysToolTip();
		}
	}
	#endregion
}
