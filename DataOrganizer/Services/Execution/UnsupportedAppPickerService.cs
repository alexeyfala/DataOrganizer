using DataOrganizer.Dto.Execution;
using DataOrganizer.Interfaces.Execution;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Execution;

/// <summary>
/// Stand-in for operating systems that have no application picker; reports no candidates.
/// </summary>
public sealed class UnsupportedAppPickerService : IAppPickerService
{
	#region Methods
	/// <inheritdoc />
	public AssociatedAppInfo? CreateFromPath(string appPath) => null;

	/// <inheritdoc />
	public Task<AssociatedAppInfo?> PickAppAsync(string filePath, CancellationToken token = default)
	{
		return Task.FromResult<AssociatedAppInfo?>(null);
	}
	#endregion
}
