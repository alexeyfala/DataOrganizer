using DataOrganizer.SystemIntegration.FileExecution.Dto;
using DataOrganizer.SystemIntegration.FileExecution.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.FileExecution.Services;

/// <summary>
/// Leaves the program that opens a file to the operating system.
/// </summary>
public sealed class SystemAppResolver : IAppResolver
{
	#region Methods
	/// <inheritdoc />
	public Task<AppResolution> ResolveAsync(string filePath, CancellationToken token = default)
	{
		return Task.FromResult(new AppResolution(IsCancelled: false, AppPath: null));
	}
	#endregion
}
