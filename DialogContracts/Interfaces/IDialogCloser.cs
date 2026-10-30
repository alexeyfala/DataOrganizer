using System.Threading.Tasks;

namespace DialogContracts.Interfaces;

/// <summary>
/// Closes the dialog that a named host shows and follows its closing animation.
/// </summary>
public interface IDialogCloser
{
	#region Methods
	/// <summary>
	/// Closes the dialog the host shows and completes once its closing animation has hidden it; nothing happens when the
	/// host shows none.
	/// </summary>
	Task CloseAsync(string dialogHostIdentifier);

	/// <summary>
	/// Returns the dialog the host shows now; <c>null</c> when it shows none.
	/// </summary>
	IShownDialog? FindShownDialog(string dialogHostIdentifier);
	#endregion
}
