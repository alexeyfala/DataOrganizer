using DataOrganizer.Infrastructure.Notifications.Enums;

namespace DataOrganizer.Infrastructure.Notifications.Dto;

/// <summary>
/// Text of a snackbar together with the level that colours it.
/// </summary>
/// <param name="Text">Text of the snackbar.</param>
/// <param name="Level">Level of the message.</param>
public sealed record SnackbarContent(string Text, SnackbarMessageLevel Level);
