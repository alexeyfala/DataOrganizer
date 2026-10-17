using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Dialogs;

/// <summary>
/// Dialog that a host shows, whose closing animation can be awaited.
/// </summary>
public interface IShownDialog
{
	#region Methods
	/// <summary>
	/// Completes once the closing animation has hidden the dialog, or after a longest wait.
	/// </summary>
	Task WaitUntilHiddenAsync();
	#endregion
}
