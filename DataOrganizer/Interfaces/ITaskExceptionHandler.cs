using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Handles "Fire-and-forget" possible task exceptions.
/// </summary>
public interface ITaskExceptionHandler
{
	#region Methods
	/// <summary>
	/// Watches and handles possible exception.
	/// </summary>
	void Watch(Task task);
	#endregion
}
