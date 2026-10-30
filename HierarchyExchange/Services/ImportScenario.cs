using AppInstance.Interfaces;
using Clipboard.Interfaces;
using DataProtection.Interfaces;
using FileExecution.Interfaces;
using Hierarchy.Dto;
using Hierarchy.Extensions;
using HierarchyExchange.Dto;
using HierarchyExchange.Enums;
using HierarchyExchange.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace HierarchyExchange.Services;

/// <inheritdoc cref="IImportScenario" />
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

	/// <inheritdoc cref="ISessionTreeState" />
	private readonly ISessionTreeState _sessionTreeState;
	#endregion

	#region Constructors
	public ImportScenario(
		IContentKeys contentKeys,
		ICopyHistoryRecorder copyHistoryRecorder,
		IDataExchangeService dataExchange,
		IOpenFilesCloser openFilesCloser,
		ISessionTreeState sessionTreeState)
	{
		_contentKeys = contentKeys;

		_copyHistoryRecorder = copyHistoryRecorder;

		_dataExchange = dataExchange;

		_openFilesCloser = openFilesCloser;

		_sessionTreeState = sessionTreeState;
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

			_sessionTreeState.Clear();
		}

		// Before the objects reach the tree, so that their rows show them as they were stored.
		_sessionTreeState.Remember(result.Imported);

		return result;
	}
	#endregion
}
