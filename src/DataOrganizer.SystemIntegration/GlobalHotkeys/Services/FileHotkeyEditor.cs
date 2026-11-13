using Comparation;
using DataOrganizer.Database.Entities.Models;
using DataOrganizer.Database.Repository.Dto;
using DataOrganizer.Database.Repository.Interfaces.Database;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.Extensions;
using DataOrganizer.Infrastructure.Diagnostics.Interfaces;
using DataOrganizer.Infrastructure.Notifications.Interfaces;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.Shared.Properties;
using DataOrganizer.SystemIntegration.GlobalHotkeys.Enums;
using DataOrganizer.SystemIntegration.GlobalHotkeys.Extensions;
using DataOrganizer.SystemIntegration.GlobalHotkeys.Interfaces;
using MapsterMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.GlobalHotkeys.Services;

/// <inheritdoc cref="IFileHotkeyEditor" />
public sealed class FileHotkeyEditor : IFileHotkeyEditor
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="IMapper" />
	private readonly IMapper _mapper;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public FileHotkeyEditor(
		IDbAccess dbAccess,
		IDbFailureReporter dbFailureReporter,
		IMapper mapper,
		INotificationService notification)
	{
		_dbAccess = dbAccess;

		_dbFailureReporter = dbFailureReporter;

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

		HotkeyEntity[] createdHotkeys;

		try
		{
			createdHotkeys = await _dbAccess
				.ReplaceHotkeysAsync(dto.Id, newHotkeys, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			// The database keeps the old hotkeys, so the session keeps them too.
			_dbFailureReporter.Report(ex, Strings.FailedToSaveHotkeys);

			return OverwriteHotkeysOutcome.ExceptionThrown;
		}

		dto
			.Hotkeys
			.Clear();

		dto
			.Hotkeys
			.AddRange(_mapper.Map<HotkeyEntity[], HotkeyDto[]>(createdHotkeys));

		return newHotkeys.IsEmpty()
			? OverwriteHotkeysOutcome.EmptySequence
			: OverwriteHotkeysOutcome.Rewritten;
	}
	#endregion
}
