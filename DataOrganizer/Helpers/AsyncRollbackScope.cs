using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Helpers;

/// <summary>
/// Collects rollback actions and runs them in LIFO order on <see cref="DisposeAsync" />
/// unless <see cref="Commit" /> was called first.
/// </summary>
internal sealed class AsyncRollbackScope : IAsyncDisposable
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger? _logger;

	/// <summary>
	/// Stack of rollback actions, popped LIFO on uncommitted disposal.
	/// </summary>
	private readonly Stack<Func<Task>> _rollbacks = new();

	/// <summary>
	/// Returns <c>True</c> when <see cref="Commit" /> has been called.
	/// </summary>
	private bool _committed;

	/// <summary>
	/// Returns <c>True</c> if the scope has been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public AsyncRollbackScope(ILogger? logger = null) => _logger = logger;
	#endregion

	#region Methods
	/// <summary>
	/// Marks the scope as successfully completed. Subsequent <see cref="DisposeAsync" />
	/// becomes a no-op — registered rollback actions will not run.
	/// </summary>
	public void Commit() => _committed = true;

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		if (_committed)
		{
			return;
		}

		_logger?.LogWarning($"Rolling back {_rollbacks.Count} action(s).");

		while (_rollbacks.TryPop(out Func<Task>? action))
		{
			try
			{
				await action().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger?.LogException(ex);
			}
		}
	}

	/// <summary>
	/// Registers an asynchronous <paramref name="action" /> to be executed (LIFO order)
	/// if the scope is disposed without <see cref="Commit" />.
	/// </summary>
	public void OnRollback(Func<Task> action) => _rollbacks.Push(action);

	/// <summary>
	/// Registers a synchronous <paramref name="action" /> to be executed (LIFO order) if
	/// the scope is disposed without <see cref="Commit" />. The action is wrapped into a
	/// completed <see cref="Task" /> internally.
	/// </summary>
	public void OnRollback(Action action) => _rollbacks.Push(() =>
	{
		action();

		return Task.CompletedTask;
	});
	#endregion
}
