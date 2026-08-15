using System;
using System.ComponentModel;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Counts down the configured delay and reports its expiry, at which the decrypted contents are to be hidden.
/// </summary>
public interface IAutoLockService : INotifyPropertyChanged
{
	#region Properties
	/// <summary>
	/// <c>True</c> while a countdown is running.
	/// </summary>
	bool IsArmed { get; }

	/// <summary>
	/// Time left before the expiry, rounded up to a whole second; <c>null</c> without a running countdown.
	/// </summary>
	TimeSpan? Remaining { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Starts the countdown from the delay stored in the settings, replacing the running one;
	/// the delay of no auto-lock stops the countdown instead.
	/// </summary>
	void Arm();

	/// <summary>
	/// Stops the running countdown, if any.
	/// </summary>
	void Stop();
	#endregion
}
