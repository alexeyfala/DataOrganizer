using DataOrganizer.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

/// <inheritdoc cref="IExceptionHandler" />
internal sealed class ExceptionHandler : IExceptionHandler
{
	#region Data
	/// <inheritdoc cref="CompositeDisposable" />
	private readonly CompositeDisposable _disposables = [];

	/// <summary>
	/// Set of previously handled exceptions.
	/// </summary>
	private readonly HashSet<string> _handledExceptions = [];

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="Lock" />
	private readonly Lock _mutex = new();

	/// <summary>
	/// Returns <c>True</c> if the service was disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public ExceptionHandler(ILogger logger) => _logger = logger;
	#endregion

	#region Event Handlers
	/// <summary>
	/// Handles <see cref="AppDomain.UnhandledException" />.
	/// </summary>
	private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
	{
		HandleException((Exception)e.ExceptionObject);
	}

	/// <summary>
	/// Handles <see cref="TaskScheduler.UnobservedTaskException" />.
	/// </summary>
	private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		e.SetObserved();

		HandleException(e.Exception);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

		_disposables.Dispose();

		_handledExceptions.Clear();
	}

	/// <inheritdoc />
	public void StartMonitoring()
	{
		AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

		TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

		Disposable.Create(() =>
		{
			AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
		}).DisposeWith(_disposables);
	}

	/// <summary>
	/// Handles the exception.
	/// </summary>
	internal void HandleException(Exception exception)
	{
		lock (_mutex)
		{
			if (!_handledExceptions.Add(exception.Message))
			{
				return;
			}

			_logger.LogException("Unhandled Exception", exception);

			if (_handledExceptions.Count < 5)
			{
				return;
			}

			_handledExceptions.Clear();
		}
	}
	#endregion
}
