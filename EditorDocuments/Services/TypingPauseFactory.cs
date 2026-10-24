using EditorDocuments.Interfaces;
using System;
using Threading.Interfaces;

namespace EditorDocuments.Services;

public sealed class TypingPauseFactory : ITypingPauseFactory
{
	#region Data
	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public TypingPauseFactory(IDispatcherAccessor dispatcher, TimeProvider timeProvider)
	{
		_dispatcher = dispatcher;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public ITypingPause Create(Action action) => new TypingPause(action, _dispatcher, _timeProvider);
	#endregion
}
