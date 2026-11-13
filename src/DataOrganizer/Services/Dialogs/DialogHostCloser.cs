using DataOrganizer.Infrastructure.DialogContracts.Constants;
using DataOrganizer.Infrastructure.DialogContracts.Interfaces;
using DialogHostAvalonia;

namespace DataOrganizer.Services.Dialogs;

/// <inheritdoc cref="IDialogHostCloser" />
public sealed class DialogHostCloser : IDialogHostCloser
{
	#region Methods
	/// <inheritdoc />
	public void Close() => DialogHost.Close(DialogHostIdentifiers.Main);
	#endregion
}
