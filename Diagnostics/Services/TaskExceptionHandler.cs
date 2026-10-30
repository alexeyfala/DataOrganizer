using Diagnostics.Interfaces;
using Serilog;
using Shared.Extensions;
using System.Threading.Tasks;

namespace Diagnostics.Services;

/// <inheritdoc cref="ITaskExceptionHandler" />
public class TaskExceptionHandler : ITaskExceptionHandler
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public TaskExceptionHandler(ILogger logger) => _logger = logger;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Watch(Task task)
	{
		task.ContinueWith(x =>
		{
			// Stryker disable once Block,Statement : a faulted task always carries its exception
			if (x.Exception is null)
			{
				return;
			}

			_logger.LogException(x.Exception);
		}, TaskContinuationOptions.OnlyOnFaulted);
	}
	#endregion
}
