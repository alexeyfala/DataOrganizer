namespace DataOrganizer.Enums;

public enum FilesEncryptionResult : byte
{
	Encrypted,
	FailedToLoadContents,
	UnableToCreateDatabaseBackup,
	FailedToEncryptContents,
	FailedToSaveContents,
	FailedToSavePasswordHash,
	ExceptionThrown
}
