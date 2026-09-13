using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Notifications;
using Serilog;
using Shared.Extensions;
using System;

namespace DataOrganizer.Services.Diagnostics;

public sealed class DbFailureReporter : IDbFailureReporter
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
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

		_logger.LogException(text, exception, breakInDebugger: false);

		_notification.ShowErrorSnackbar(text);
	}
	#endregion
}
