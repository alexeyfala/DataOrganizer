namespace DataOrganizer.Enums;

public enum FolderEncryptionResult : byte
{
	FailedToLoadContents,
	UnableToCreateDatabaseBackup,
	FailedToEncryptContents,
	FailedToDecryptContents,
	FailedToSaveContents,
	FailedToSavePasswordHash,
	ExceptionThrown,
	Done
}
