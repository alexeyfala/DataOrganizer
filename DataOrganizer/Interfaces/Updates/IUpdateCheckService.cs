using DataOrganizer.DTO.Updates;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Updates;

/// <summary>
/// Checks whether a newer application version is available.
/// </summary>
public interface IUpdateCheckService
{
	#region Methods
	/// <summary>
	/// Determines whether a newer application version is available.
	/// </summary>
	Task<UpdateCheckResult> CheckAsync(CancellationToken token = default);
	#endregion
}
