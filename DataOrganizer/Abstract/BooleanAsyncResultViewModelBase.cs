using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

public abstract class BooleanAsyncResultViewModelBase : AsyncResultViewModelBase<bool>
{
	#region Methods
	/// <inheritdoc cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />
	public Task<bool> GetResultAsync(in CancellationToken token = default)
	{
		return GetResultAsync(defaultResult: false, token: token);
	}
	#endregion
}
