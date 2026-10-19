using System;

namespace DataOrganizer.Interfaces.Runtime;

/// <summary>
/// Repeats work on the UI thread at an interval of the application clock.
/// </summary>
public interface IUiTimer
{
	#region Methods
	/// <summary>
	/// Runs <paramref name="tick" /> on the UI thread every <paramref name="interval" /> until it answers <c>false</c>.
	/// </summary>
	void Run(Func<bool> tick, TimeSpan interval);
	#endregion
}
