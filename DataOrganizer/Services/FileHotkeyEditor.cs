using CommunityToolkit.Mvvm.Messaging;
using Comparation;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using Entities.Models;
using MapsterMapper;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class FileHotkeyEditor : IFileHotkeyEditor
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMapper" />
	private readonly IMapper _mapper;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	public FileHotkeyEditor(
		IDbAccess dbAccess,
		ILogger logger,
		IMapper mapper,
		IMessenger messenger)
	{
		_dbAccess = dbAccess;

		_logger = logger;

		_mapper = mapper;

		_messenger = messenger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<OverwriteHotkeysResult> OverwriteAsync(
		FileModelDto dto,
		CodeMaskPair[] newHotkeys,
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default)
	{
		IEqualityComparer<HotkeyModelDto> comparer = Equality.Of<HotkeyModelDto>()
			.By(x => x.Code)
			.AndBy(x => x.Mask);

		HotkeyModelDto[] temp = [.. newHotkeys.ToHotkeyModelsDto()];

		if (dto
			.Hotkeys
			.SequenceEqual(temp, comparer))
		{
			return OverwriteHotkeysResult.SameHotkeys;
		}

		if (temp.IsNotEmpty() && hierarchy.FindFileBy(x => x.Hotkeys.SequenceEqual(temp, comparer)) is { } existed)
		{
			string sequence = newHotkeys.GetHotkeysPresentation();

			_messenger.Send(new ShowSnackbarMessage(
				$@"{string.Format(Strings.HotkeysAlreadyAssignedFor, sequence)} ""{existed.Name}""",
				SnackbarMessageLevel.Warning));

			return OverwriteHotkeysResult.AlreadyInUse;
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
				return OverwriteHotkeysResult.EmptySequence;
			}

			try
			{
				HotkeyModel[] createdHotkeys = await _dbAccess
					.AddHotkeysAsync(dto.Id, newHotkeys, token)
					.ConfigureAwait(false);

				HotkeyModelDto[] mapped = _mapper.Map<HotkeyModel[], HotkeyModelDto[]>(createdHotkeys);

				dto
					.Hotkeys
					.AddRange(mapped);

				return OverwriteHotkeysResult.Rewritten;
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);

				return OverwriteHotkeysResult.ExceptionThrown;
			}
		}
		finally
		{
			dto.SetHotkeysToolTip();
		}
	}
	#endregion
}
