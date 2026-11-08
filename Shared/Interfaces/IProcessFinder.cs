namespace Shared.Interfaces;

/// <summary>
/// Finds running processes.
/// </summary>
public interface IProcessFinder
{
	#region Methods
	/// <summary>
	/// Takes hold of the running process <paramref name="processId" />; <c>null</c> when no such process runs.
	/// </summary>
	IExecutingFileProcess? FindById(int processId);
	#endregion
}
