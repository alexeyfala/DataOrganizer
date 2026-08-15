using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces;

/// <summary>
/// Housekeeping of the database file itself.
/// </summary>
public interface IDbMaintenance
{
	#region Methods
	/// <summary>
	/// Rewrites the database once to drop the free pages left over from before secure deletion was in place.
	/// </summary>
	Task EraseFreePagesOnceAsync(CancellationToken token = default);
	#endregion
}
