using System;

namespace DataOrganizer.Interfaces.Diagnostics;

/// <summary>
/// Reports a failed database operation to the log and to the user.
/// </summary>
public interface IDbFailureReporter
{
	#region Methods
	/// <summary>
	/// Reports the failure under the supplied text; a cancelled operation is passed over.
	/// </summary>
	void Report(Exception exception, string text);
	#endregion
}
