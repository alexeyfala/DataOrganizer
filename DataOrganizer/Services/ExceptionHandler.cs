using DataOrganizer.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
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
	/// List of previously handled exceptions.
	/// </summary>
	private readonly List<string> _handledExceptions = [];

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
	private void CurrentDomain_UnhandledException(EventPattern<UnhandledExceptionEventArgs> e)
	{
		HandleException((Exception)e.EventArgs.ExceptionObject);
	}

	/// <summary>
	/// Handles <see cref="TaskScheduler.UnobservedTaskException" />.
	/// </summary>
	private void TaskScheduler_UnobservedTaskException(EventPattern<UnobservedTaskExceptionEventArgs> e)
	{
		e.EventArgs.SetObserved();

		HandleException(e.EventArgs.Exception);
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
	}

	/// <inheritdoc />
	public void StartMonitoring()
	{
		Observable.FromEventPattern<UnhandledExceptionEventHandler, UnhandledExceptionEventArgs>(
			x => AppDomain.CurrentDomain.UnhandledException += x,
			x => AppDomain.CurrentDomain.UnhandledException -= x)
			.Subscribe(CurrentDomain_UnhandledException)
			.DisposeWith(_disposables);

		Observable.FromEventPattern<UnobservedTaskExceptionEventArgs>(
			x => TaskScheduler.UnobservedTaskException += x,
			x => TaskScheduler.UnobservedTaskException -= x)
			.Subscribe(TaskScheduler_UnobservedTaskException)
			.DisposeWith(_disposables);
	}
	#endregion

	#region Service
	/// <summary>
	/// Handles the exception.
	/// </summary>
	private void HandleException(Exception exception)
	{
		lock (_mutex)
		{
			if (_handledExceptions.Any(x => string.Equals(x, exception.Message)))
			{
				return;
			}

			_handledExceptions.Add(exception.Message);

			const string title = "Unhandled Exception";

			_logger.LogException(title, exception);
		}
	}
	#endregion
}
