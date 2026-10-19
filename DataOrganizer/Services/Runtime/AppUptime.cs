using DataOrganizer.Interfaces.Runtime;
using System;

namespace DataOrganizer.Services.Runtime;

public sealed class AppUptime : IAppUptime
{
	#region Properties
	/// <inheritdoc />
	public TimeSpan Elapsed => _timeProvider.GetElapsedTime(_startTimestamp);
	#endregion

	#region Data
	/// <summary>
	/// Timestamp of the start.
	/// </summary>
	private readonly long _startTimestamp;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public AppUptime(TimeProvider timeProvider)
	{
		_timeProvider = timeProvider;

		_startTimestamp = timeProvider.GetTimestamp();
	}
	#endregion
}
