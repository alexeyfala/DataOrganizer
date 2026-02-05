namespace DataOrganizer.Enums;

public enum FilesEncryptionResult : byte
{
	FailedToLoadContents,
	UnableToCreateDatabaseBackup,
	FailedToEncryptContents,
	FailedToSaveContents,
	FailedToSavePasswordHash,
	ExceptionThrown,
	Done
}
