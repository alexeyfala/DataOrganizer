using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels.Dialogs;

/// <summary>
/// The view model for returning <see cref="bool" /> value.
/// </summary>
public abstract class BooleanAsyncResultViewModel : AsyncResultViewModelBase<bool>
{
	#region Constructors
	protected BooleanAsyncResultViewModel(
		IDialogCloser dialogCloser,
		ITaskExceptionHandler exceptionHandler) : base(dialogCloser, exceptionHandler)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />
	public Task<bool> GetResultAsync(Task dialogClosed, in CancellationToken token = default)
	{
		return GetResultAsync(
			dialogClosed,
			defaultResult: false,
			token: token);
	}
	#endregion
}
