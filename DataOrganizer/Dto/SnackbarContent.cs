using DataOrganizer.Enums;

namespace DataOrganizer.Dto;

/// <summary>
/// Text of a snackbar together with the level that colours it.
/// </summary>
public sealed record SnackbarContent(string Text, SnackbarMessageLevel Level);
