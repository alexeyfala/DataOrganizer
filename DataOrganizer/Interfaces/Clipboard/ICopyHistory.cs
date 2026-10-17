using DataOrganizer.Dto.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataOrganizer.Interfaces.Clipboard;

/// <summary>
/// The files whose contents were copied, the latest first, with the one chosen in their list; kept in a file of its own.
/// </summary>
public interface ICopyHistory
{
	#region Properties
	/// <summary>
	/// Identifiers of the copied files, the latest first.
	/// </summary>
	ReadOnlyObservableCollection<Guid> ItemIds { get; }

	/// <summary>
	/// The identifier of the file chosen in the list of the history.
	/// </summary>
	Guid SelectedItemId { get; set; }
	#endregion

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
	/// Reads the history from its file, keeping the files that <paramref name="hierarchy" /> holds.
	/// </summary>
	void Load(IEnumerable<ExplorerItemDtoBase> hierarchy);

	/// <summary>
	/// Forgets a copied file.
	/// </summary>
	void Remove(FileDto file);

	/// <summary>
	/// Writes the history over its file.
	/// </summary>
	void Save();
	#endregion
}
