using DataOrganizer.Interfaces.Dialogs;
using DialogHostAvalonia;

namespace DataOrganizer.Services.Dialogs;

/// <inheritdoc cref="IDialogHostCloser" />
public sealed class DialogHostCloser : IDialogHostCloser
{
	#region Methods
	/// <inheritdoc />
	public void Close() => DialogHost.Close(null);
	#endregion
}
