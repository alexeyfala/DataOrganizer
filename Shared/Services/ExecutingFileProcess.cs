using Shared.Interfaces;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Shared.Services;

/// <inheritdoc cref="IExecutingFileProcess" />
[ExcludeFromCodeCoverage(Justification = "Wraps a real process")]
internal sealed class ExecutingFileProcess : IExecutingFileProcess
{
	#region Properties
	/// <inheritdoc />
	public bool HasExited => _process.HasExited;

	/// <inheritdoc />
	public int Id => _process.Id;
	#endregion

	#region Data
	/// <inheritdoc cref="Process" />
	private readonly Process _process;
	#endregion

	#region Constructors
	public ExecutingFileProcess(Process process) => _process = process;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose() => _process.Dispose();

	/// <inheritdoc />
	public void Kill() => _process.Kill();
	#endregion
}
