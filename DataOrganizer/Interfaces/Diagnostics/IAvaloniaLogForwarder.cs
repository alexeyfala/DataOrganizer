using System;

namespace DataOrganizer.Interfaces.Diagnostics;

/// <summary>
/// Passes the warnings and errors of Avalonia to the log of the application, with every value that may come from the
/// data of the user left out.
/// </summary>
public interface IAvaloniaLogForwarder : IDisposable
{
	#region Methods
	/// <summary>
	/// Takes the place of the sink of the log of Avalonia, which keeps getting every entry.
	/// </summary>
	void StartForwarding();
	#endregion
}
