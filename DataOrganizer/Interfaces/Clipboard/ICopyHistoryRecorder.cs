using DataOrganizer.Dto.Entities;

namespace DataOrganizer.Interfaces.Clipboard;

/// <summary>
/// Changes of the copy history: the copied files it gains and the files it forgets.
/// </summary>
public interface ICopyHistoryRecorder
{
	#region Methods
	/// <summary>
	/// Puts a copied file at the top; a shown list moves it there at once only when <paramref name="showAtOnce" />.
	/// </summary>
	void Add(FileDto file, bool showAtOnce);

	/// <summary>
	/// Forgets every copied file and the chosen one.
	/// </summary>
	void Clear();

	/// <summary>
	/// Forgets a copied file.
	/// </summary>
	void Remove(FileDto file);
	#endregion
}
