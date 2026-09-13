using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Notifications;
using Repository.Exceptions;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Threading;

namespace DataOrganizer.Services.Diagnostics;

public sealed class DbFailureReporter : IDbFailureReporter
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <summary>
	/// <c>True</c> once the user has been told that the database does not accept changes.
	/// </summary>
	private bool _isRefusalShown;
	#endregion

	#region Constructors
	public DbFailureReporter(ILogger logger, INotificationService notification)
	{
		_logger = logger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Report(Exception exception, string text)
	{
		if (exception is OperationCanceledException)
		{
			// The caller gave up on its own, so nothing failed and nothing is worth saying.
			return;
		}

		if (exception is DatabaseNotWritableException)
		{
			// A refusal is a state the database is in, not a failure of this operation.
			_logger.LogWarning(exception.Message);

			if (!Interlocked.Exchange(ref _isRefusalShown, true))
			{
				// The refusal holds for the rest of the session, so saying it once says it enough.
				_notification.ShowErrorSnackbar(Strings.DatabaseIsUnavailable);
			}

			return;
		}

		_logger.LogException(text, exception, breakInDebugger: false);

		_notification.ShowErrorSnackbar(text);
	}
	#endregion
}
