namespace DataOrganizer.Enums;

public enum FolderEncryptionResult : byte
{
	FailedToLoadContents,
	UnableToCreateDatabaseBackup,
	FailedToEncryptContents,
	FailedToSaveContents,
	FailedToSavePasswordHash,
	ExceptionThrown,
	Done
}
