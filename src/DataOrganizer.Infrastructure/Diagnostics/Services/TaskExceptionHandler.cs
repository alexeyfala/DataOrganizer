using DataOrganizer.Infrastructure.Diagnostics.Interfaces;
using DataOrganizer.Shared.Extensions;
using Serilog;
using System.Threading.Tasks;

namespace DataOrganizer.Infrastructure.Diagnostics.Services;

/// <inheritdoc cref="ITaskExceptionHandler" />
public class TaskExceptionHandler : ITaskExceptionHandler
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="TaskExceptionHandler" /> class.
	/// </summary>
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
