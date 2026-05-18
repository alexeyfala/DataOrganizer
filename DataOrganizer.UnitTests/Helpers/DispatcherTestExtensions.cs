using Avalonia.Threading;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.Helpers;

/// <summary>
/// Test-only extensions that tame the <see cref="IDispatcher" /> NSubstitute mock for tests
/// that exercise code which posts UI-touching work through the dispatcher (e.g. asynchronous
/// updates inside <c>ViewModelBase.CloseExecutingFile</c>).
/// </summary>
internal static class DispatcherTestExtensions
{
	#region Methods
	/// <summary>
	/// Configures an <see cref="IDispatcher" /> substitute to invoke any <see cref="Action" />
	/// passed to <see cref="IDispatcher.Post(Action, DispatcherPriority)" /> immediately on the calling thread,
	/// instead of recording the call without execution. This makes effects of dispatcher-posted
	/// work observable synchronously in unit-test assertions.
	/// </summary>
	public static IDispatcher RunPostInline(this IDispatcher dispatcher)
	{
		dispatcher
			.When(x => x.Post(Arg.Any<Action>()))
			.Do(call => call.Arg<Action>().Invoke());

		return dispatcher;
	}
	#endregion
}
