using Avalonia;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

public abstract class BooleanAsyncResultViewModelBase : AsyncResultViewModelBase<bool>
{
	#region Constructors
	protected BooleanAsyncResultViewModelBase(Application app) : base(app)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />
	public Task<bool> GetResultAsync(in CancellationToken token = default)
	{
		return GetResultAsync(
			defaultResult: false,
			token: token);
	}
	#endregion
}
