using System;
using System.Runtime.CompilerServices;

namespace DataProtection.Interfaces;

/// <summary>
/// Reports a failed cryptographic operation to the log and to the user.
/// </summary>
public interface IEncryptionFailureReporter
{
	#region Methods
	/// <summary>
	/// Reports the failure; a rejected password is told apart from damaged data.
	/// </summary>
	void Report(Exception exception, [CallerMemberName] string callerName = "");
	#endregion
}
