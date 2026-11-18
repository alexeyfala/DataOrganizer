using System.Collections.Generic;
using System.Threading;

namespace DataOrganizer.Hierarchy.UnitTests.FilteredLists.Fakes;

/// <summary>
/// Synchronization context that keeps the posted callbacks until the test runs them.
/// </summary>
internal sealed class QueueingSynchronizationContext : SynchronizationContext
{
	#region Data
	/// <summary>
	/// Callbacks posted and not run yet, in their order.
	/// </summary>
	private readonly Queue<(SendOrPostCallback Callback, object? State)> _posted = new();
	#endregion

	#region Methods
	/// <inheritdoc />
	public override void Post(SendOrPostCallback d, object? state) => _posted.Enqueue((d, state));

	/// <summary>
	/// Runs the callbacks posted so far, in their order.
	/// </summary>
	public void RunPosted()
	{
		while (_posted.TryDequeue(out (SendOrPostCallback Callback, object? State) posted))
		{
			posted.Callback(posted.State);
		}
	}
	#endregion
}
