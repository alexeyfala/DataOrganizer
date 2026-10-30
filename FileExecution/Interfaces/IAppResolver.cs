using FileExecution.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace FileExecution.Interfaces;

/// <summary>
/// Chooses the program that opens a file on the current operating system.
/// </summary>
public interface IAppResolver
{
	#region Methods
	/// <summary>
	/// Returns the program that opens <paramref name="filePath" />, asking the user when the system has no usable one.
	/// </summary>
	Task<AppResolution> ResolveAsync(string filePath, CancellationToken token = default);
	#endregion
}
