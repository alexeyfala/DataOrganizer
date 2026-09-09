using DataOrganizer.Interfaces;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Services;

/// <summary>
/// Shows messages one after another on the surface of a presenter, holding back the ones waiting for their turn.
/// </summary>
internal sealed class MessageChannel<TContent> where TContent : notnull
{
	#region Data
	/// <inheritdoc cref="IMessagePresenter{TContent}" />
	private readonly IMessagePresenter<TContent> _presenter;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <summary>
	/// Messages waiting for their turn.
	/// </summary>
	private readonly Queue<TContent> _waiting = new();

	/// <summary>
	/// Moment the shown message may go away at; <c>null</c> while nothing is shown.
	/// </summary>
	private DateTimeOffset? _removeAt;

	/// <summary>
	/// Moment the free surface may be given to the next message at.
	/// </summary>
	private DateTimeOffset _showNextAt;

	/// <summary>
	/// The message on the screen; <c>null</c> while none is shown.
	/// </summary>
	private TContent? _shown;
	#endregion

	#region Constructors
	public MessageChannel(IMessagePresenter<TContent> presenter, TimeProvider timeProvider)
	{
		_presenter = presenter;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Shows a message or puts it in the queue, and returns <c>false</c> when it will not be shown at all.
	/// </summary>
	internal bool Show(TContent content)
	{
		if (!_presenter.CanShow)
		{
			return false;
		}

		// A repeat of what is on the screen gets more time instead of a place in the queue.
		if (_shown is { } shown && EqualityComparer<TContent>.Default.Equals(shown, content))
		{
			_removeAt = _timeProvider.GetUtcNow() + MessageChannelOptions.MessageDuration;

			return true;
		}

		if (IsSurfaceFree())
		{
			Post(content);

			return true;
		}

		if (_waiting.Count >= MessageChannelOptions.MaxWaitingMessages)
		{
			return false;
		}

		_waiting.Enqueue(content);

		return true;
	}

	/// <summary>
	/// Takes the shown message away once its time is up, gives the free surface to the next one,
	/// and returns whether the channel still has anything to do.
	/// </summary>
	internal bool Tick()
	{
		if (!_presenter.CanShow)
		{
			_waiting.Clear();

			_removeAt = null;

			_shown = default;

			return false;
		}

		if (_removeAt is { } removeAt)
		{
			if (IsMessageOver(removeAt))
			{
				Remove();
			}

			return true;
		}

		if (_waiting.Count == 0)
		{
			return false;
		}

		if (IsSurfaceFree())
		{
			Post(_waiting.Dequeue());
		}

		return true;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when a message has been on the screen long enough and the pointer does not hold it.
	/// </summary>
	private bool IsMessageOver(DateTimeOffset removeAt)
	{
		return _timeProvider.GetUtcNow() >= removeAt && !_presenter.IsPointerOverMessage;
	}

	/// <summary>
	/// Tells whether no message occupies the surface and the pause after the last one is over.
	/// </summary>
	private bool IsSurfaceFree()
	{
		return _removeAt is null && _timeProvider.GetUtcNow() >= _showNextAt;
	}

	/// <summary>
	/// Hands a message over to the presenter and gives it its time on the screen.
	/// </summary>
	private void Post(TContent content)
	{
		_presenter.Post(content);

		_shown = content;

		_removeAt = _timeProvider.GetUtcNow() + MessageChannelOptions.MessageDuration;
	}

	/// <summary>
	/// Takes the shown message off the screen and holds the surface free for the pause.
	/// </summary>
	private void Remove()
	{
		_presenter.Remove();

		_shown = default;

		_removeAt = null;

		_showNextAt = _timeProvider.GetUtcNow() + MessageChannelOptions.MessageGap;
	}
	#endregion
}
