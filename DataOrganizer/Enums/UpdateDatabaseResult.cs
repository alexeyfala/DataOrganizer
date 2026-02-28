namespace DataOrganizer.Enums;

public enum UpdateDatabaseResult : byte
{
	FailedToSaveContentsInDb,
	FailedToSaveFolderPropertiesInDb,
	ExceptionThrown,
	Done
}
