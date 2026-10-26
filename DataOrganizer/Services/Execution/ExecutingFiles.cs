using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Messages.Execution;
using Hierarchy.Dto;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Threading.Interfaces;

namespace DataOrganizer.Services.Execution;

/// <inheritdoc cref="IExecutingFiles" />
public sealed class ExecutingFiles : IExecutingFiles, IRecipient<CloseExecutingFileMessage>
{
	#region Properties
	/// <inheritdoc />
	public ReadOnlyObservableCollection<FileDto> Files { get; }
	#endregion

	#region Data
	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="IExecutionEngine" />
	private readonly IExecutionEngine _executionEngine;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <summary>
	/// Source of <see cref="Files" />.
	/// </summary>
	private readonly ObservableCollection<FileDto> _files = [];

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public ExecutingFiles(
		IDispatcherAccessor dispatcher,
		IExecutionEngine executionEngine,
		ILogger logger,
		IMessenger messenger,
		ITaskExceptionHandler exceptionHandler)
	{
		_dispatcher = dispatcher;

		_executionEngine = executionEngine;

		_exceptionHandler = exceptionHandler;

		_logger = logger;

		Files = new(_files);

		messenger.RegisterAll(this);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Close(FileDto file)
	{
		_logger.LogInformation($"Closing an executed file in the operating system:{file.GetPropertyValues(
			true,
			nameof(FileDto.Id),
			nameof(FileDto.Name),
			nameof(FileDto.Kind))}");

		_dispatcher.Post(() =>
		{
			_files.Remove(file);

			file.IsExecuting = false;
		});

		_exceptionHandler.Watch(_executionEngine.CloseAsync(file.Id));
	}

	/// <inheritdoc />
	public Task CloseAllAsync() => _files
		.ToArray()
		.ForEachAsync(x => _executionEngine.CloseAsync(x.Id));

	/// <inheritdoc />
	public async Task<bool> ExecuteAsync(ExecuteFileParameters parameters, CancellationToken token = default)
	{
		if (!await _executionEngine
			.ExecuteAsync(parameters, token)
			.ConfigureAwait(true))
		{
			return false;
		}

		parameters.File.IsExecuting = true;

		_files.Add(parameters.File);

		return true;
	}

	/// <inheritdoc />
	public bool IsExecuting(Guid id) => _executionEngine.IsExecuting(id);

	/// <inheritdoc />
	public void Receive(CloseExecutingFileMessage message)
	{
		// The executing files change on the UI thread, so they are searched there.
		_dispatcher.Post(() =>
		{
			if (_files.FirstOrDefault(x => x.Id == message.FileId) is not { } file)
			{
				return;
			}

			Close(file);
		});
	}
	#endregion
}
