using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

public abstract class BooleanAsyncResultViewModelBase : AsyncResultViewModelBase<bool>
{
	#region Methods
	/// <inheritdoc cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />
	public Task<bool> GetResultAsync(CancellationToken token = default) => GetResultAsync(false, token);
	#endregion
}
