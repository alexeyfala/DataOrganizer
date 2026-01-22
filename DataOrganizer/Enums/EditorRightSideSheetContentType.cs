using DataOrganizer.ViewModels;

namespace DataOrganizer.Enums;

/// <summary>
/// The type of <see cref="EditorViewModel.RightSideSheetContent" />.
/// </summary>
public enum EditorRightSideSheetContentType : byte
{
	None,
	CopyHistory,
	ExecutedFiles
}
