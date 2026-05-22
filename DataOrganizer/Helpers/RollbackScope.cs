using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DataOrganizer.Helpers;

/// <summary>
/// Collects rollback actions and runs them in LIFO order on <see cref="Dispose" />
/// unless <see cref="Commit" /> was called first.
/// </summary>
internal sealed class RollbackScope(ILogger? logger = null) : IDisposable
{
	#region Data
	/// <summary>
	/// Stack of rollback actions, popped LIFO on uncommitted disposal.
	/// </summary>
	private readonly Stack<Action> _rollbacks = new();

	/// <summary>
	/// Returns <c>True</c> when <see cref="Commit" /> has been called.
	/// </summary>
	private bool _committed;

	/// <summary>
	/// Returns <c>True</c> if the scope has been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Methods
	/// <summary>
	/// Marks the scope as successfully completed. Subsequent <see cref="Dispose" /> becomes
	/// a no-op — registered rollback actions will not run.
	/// </summary>
	public void Commit() => _committed = true;

	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		if (_committed)
		{
			return;
		}

		while (_rollbacks.TryPop(out Action? action))
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				logger?.LogException(ex);
			}
		}
	}

	/// <summary>
	/// Registers <paramref name="action" /> to be executed (LIFO order) if the scope is
	/// disposed without <see cref="Commit" />.
	/// </summary>
	public void OnRollback(Action action) => _rollbacks.Push(action);
	#endregion
}
