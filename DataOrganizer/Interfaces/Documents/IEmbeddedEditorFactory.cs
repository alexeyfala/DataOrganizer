using DataOrganizer.Dto.Entities;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Interfaces.Documents;

/// <summary>
/// Creates the view models of the editors of files opened in the built-in editor.
/// </summary>
public interface IEmbeddedEditorFactory
{
	#region Methods
	/// <summary>
	/// Creates the editor of a file by its kind, ready to load it; <c>null</c> for a kind that has no editor.
	/// </summary>
	EmbeddedEditorViewModelBase? Create(FileDto file);
	#endregion
}
