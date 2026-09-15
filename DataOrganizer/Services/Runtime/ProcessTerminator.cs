using DataOrganizer.Interfaces.Runtime;
using System;

namespace DataOrganizer.Services.Runtime;

/// <inheritdoc cref="IProcessTerminator" />
public sealed class ProcessTerminator : IProcessTerminator
{
	#region Methods
	/// <inheritdoc />
	public void Terminate(int exitCode = 0) => Environment.Exit(exitCode);
	#endregion
}
