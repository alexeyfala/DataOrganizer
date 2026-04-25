using Avalonia;
using DataOrganizer.Abstract;
using DataOrganizer.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// Th view model for returning <see cref="bool" /> value.
/// </summary>
public class BooleanAsyncResultViewModel : AsyncResultViewModelBase<bool>
{
	#region Constructors
	public BooleanAsyncResultViewModel(
		Application app,
		ITaskExceptionHandler handler) : base(app, handler)
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
