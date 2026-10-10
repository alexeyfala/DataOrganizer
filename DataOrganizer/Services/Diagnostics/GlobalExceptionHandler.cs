using DataOrganizer.Interfaces.Diagnostics;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Diagnostics;

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

		if (OperatingSystem.IsLinux() && IsBenignDBusAccentColorFailure(e.Exception))
		{
			// Known Avalonia bug: DBusPlatformSettings reads the FreeDesktop appearance portal's
			// accent color as a struct, but some portals return another
			// variant type, so the fire-and-forget read faults. Theming is unaffected. Swallow.
			_logger.LogDebug($"Suppressed unobserved DBus accent-color failure: {e.Exception.GetBaseException().Message}");

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
	/// <c>True</c> when the aggregated exception has leaves and every one of them satisfies <paramref name="predicate" />.
	/// </summary>
	internal static bool AreAllLeaves(AggregateException aggregate, Func<Exception, bool> predicate)
	{
		ReadOnlyCollection<Exception> leafExceptions = aggregate
			.Flatten()
			.InnerExceptions;

		if (leafExceptions.Count == 0)
		{
			return false;
		}

		foreach (Exception leaf in leafExceptions)
		{
			if (!predicate(leaf))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// <c>True</c> for a failure of any type inside Avalonia's reader of the FreeDesktop settings portal.
	/// </summary>
	internal static bool IsPlatformSettingsFailure(string? stackTrace)
	{
		const string platformSettingsTypeName = "Avalonia.FreeDesktop.DBusPlatformSettings";

		return stackTrace?.Contains(platformSettingsTypeName, StringComparison.Ordinal) == true;
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
	/// <c>True</c> when the aggregated exception is exclusively composed of Avalonia's accent-color
	/// read failures from the FreeDesktop appearance portal on Linux.
	/// </summary>
	private static bool IsBenignDBusAccentColorFailure(AggregateException aggregate)
	{
		return AreAllLeaves(aggregate, x => IsPlatformSettingsFailure(x.StackTrace));
	}
	#endregion
}
