using DataOrganizer.Interfaces.Documents;
using System;
using System.Threading;
using Threading.Interfaces;

namespace DataOrganizer.Services.Documents;

/// <inheritdoc cref="ITypingPause" />
public sealed class TypingPause : ITypingPause
{
	#region Data
	/// <summary>
	/// Pause in typing after which the action runs.
	/// </summary>
	internal static readonly TimeSpan Delay = TimeSpan.FromSeconds(0.5);

	/// <summary>
	/// Work that waits for the pause.
	/// </summary>
	private readonly Action _action;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <summary>
	/// Counts the pause from the latest change.
	/// </summary>
	private readonly ITimer _timer;

	/// <summary>
	/// Whether the action waits for the pause.
	/// </summary>
	private bool _isPending;
	#endregion

	#region Constructors
	public TypingPause(Action action, IDispatcherAccessor dispatcher, TimeProvider timeProvider)
	{
		_action = action;

		_dispatcher = dispatcher;

		_timer = timeProvider.CreateTimer(
			_ => _dispatcher.Post(Flush),
			null,
			Timeout.InfiniteTimeSpan,
			Timeout.InfiniteTimeSpan);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		_isPending = false;

		_timer.Dispose();
	}

	/// <inheritdoc />
	public void Flush()
	{
		if (!_isPending)
		{
			return;
		}

		_isPending = false;

		_action();
	}

	/// <inheritdoc />
	public void Restart()
	{
		_isPending = true;

		_timer.Change(Delay, Timeout.InfiniteTimeSpan);
	}
	#endregion
}
