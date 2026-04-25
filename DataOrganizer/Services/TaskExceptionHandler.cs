using DataOrganizer.Interfaces;
using Serilog;
using Shared.Extensions;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

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
			if (x.Exception is null)
			{
				return;
			}

			_logger.LogException(x.Exception);
		}, TaskContinuationOptions.OnlyOnFaulted);
	}
	#endregion
}
