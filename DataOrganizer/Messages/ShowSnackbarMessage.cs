using DataOrganizer.Enums;

namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised to request displaying a snackbar with the given text and level.
/// </summary>
public sealed record ShowSnackbarMessage(string Text, SnackbarMessageLevel Level);
