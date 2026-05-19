using CommunityToolkit.Mvvm.Messaging.Messages;
using DataOrganizer.Enums;

namespace DataOrganizer.Messages;

/// <summary>
/// Payload that describes a snackbar to be shown by <see cref="Abstract.ViewModelBase" />.
/// </summary>
internal sealed record ShowSnackbarPayload(string Text, SnackbarMessageLevel Level);

/// <summary>
/// Notification raised to request displaying a snackbar with the given text and level.
/// </summary>
internal sealed class ShowSnackbarMessage : ValueChangedMessage<ShowSnackbarPayload>
{
	#region Constructors
	public ShowSnackbarMessage(ShowSnackbarPayload payload) : base(payload)
	{
	}
	#endregion
}
