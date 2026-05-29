using DataOrganizer.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

internal sealed class GlobalExceptionHandler : IGlobalExceptionHandler
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
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public GlobalExceptionHandler(ILogger logger) => _logger = logger;
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

		if (AppUtils.IsLinux && IsBenignDBusAppMenuFailure(e.Exception))
		{
			// Avalonia's DBus menu exporter fires-and-forgets a call to the global AppMenu
			// registrar; on Linux distributions that don't run "com.canonical.AppMenu.Registrar"
			// (e.g., default GNOME) the call surfaces as an unobserved task exception. The
			// failure is cosmetic — global menu integration is unavailable, the tray menu
			// continues to work via the GTK status icon. Log at debug level and swallow.
			_logger.LogDebug($"Suppressed unobserved DBus AppMenu.Registrar failure: {e.Exception.GetBaseException().Message}");

			return;
		}

		HandleException(e.Exception);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

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
			if (!_handledExceptions.Add($"{exception.GetType().Name}: {exception.Message}"))
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

	#region Helpers
	/// <summary>
	/// Returns <c>True</c> if the aggregated exception is exclusively composed of DBus failures
	/// caused by the missing "com.canonical.AppMenu.Registrar" service on Linux.
	/// </summary>
	private static bool IsBenignDBusAppMenuFailure(AggregateException aggregate)
	{
		const string dBusExceptionTypeName = "Tmds.DBus.Protocol.DBusException";

		const string appMenuRegistrarServiceName = "com.canonical.AppMenu.Registrar";

		ReadOnlyCollection<Exception> leafExceptions = aggregate
			.Flatten()
			.InnerExceptions;

		if (leafExceptions.Count == 0)
		{
			return false;
		}

		foreach (Exception leaf in leafExceptions)
		{
			if (leaf.GetType().FullName != dBusExceptionTypeName
				|| !leaf.Message.Contains(appMenuRegistrarServiceName, StringComparison.Ordinal))
			{
				return false;
			}
		}

		return true;
	}
	#endregion
}
