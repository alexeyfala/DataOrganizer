using DataOrganizer.Dto;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Exchange;
using DataOrganizer.Interfaces.Execution;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Exchange;

public sealed class ImportScenario : IImportScenario
{
	#region Data
	/// <inheritdoc cref="IContentKeys" />
	private readonly IContentKeys _contentKeys;

	/// <inheritdoc cref="ICopyHistoryRecorder" />
	private readonly ICopyHistoryRecorder _copyHistoryRecorder;

	/// <inheritdoc cref="IDataExchangeService" />
	private readonly IDataExchangeService _dataExchange;

	/// <inheritdoc cref="IOpenFilesCloser" />
	private readonly IOpenFilesCloser _openFilesCloser;
	#endregion

	#region Constructors
	public ImportScenario(
		IContentKeys contentKeys,
		ICopyHistoryRecorder copyHistoryRecorder,
		IDataExchangeService dataExchange,
		IOpenFilesCloser openFilesCloser)
	{
		_contentKeys = contentKeys;

		_copyHistoryRecorder = copyHistoryRecorder;

		_dataExchange = dataExchange;

		_openFilesCloser = openFilesCloser;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<ImportDataResult?> ImportAsync(Collection<ExplorerItemDtoBase> hierarchy)
	{
		if (!await _openFilesCloser
			.TryCloseAsync([.. hierarchy.GetFilesBy(x => x.IsOpened())])
			.ConfigureAwait(true))
		{
			return null;
		}

		if (await _dataExchange
			.ImportDataAsync(hierarchy)
			.ConfigureAwait(true) is not { } result || result.Variant == ImportMode.None)
		{
			return null;
		}

		if (result.Variant == ImportMode.Replace)
		{
			hierarchy.Clear();

			_copyHistoryRecorder.Clear();

			_contentKeys.DiscardAllKeys();
		}

		return result;
	}
	#endregion
}
