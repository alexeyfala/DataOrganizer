namespace DataOrganizer.Enums;

public enum FilesEncryptionResult : byte
{
	Encrypted,
	FailedToLoadContents,
	FailedToEncryptContents,
	UnableToCreateDatabaseBackup,
	ExceptionThrown
}
