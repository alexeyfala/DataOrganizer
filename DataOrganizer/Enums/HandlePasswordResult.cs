namespace DataOrganizer.Enums;

/// <summary>
/// Password match result.
/// </summary>
public enum HandlePasswordResult : byte
{
	PasswordNotEntered,
	PasswordDoesNotMatch,
	FailedToShowFileContents,
	Applied
}
