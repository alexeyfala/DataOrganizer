using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataOrganizer.Dto.Settings;

/// <summary>
/// The settings for "Copy History".
/// </summary>
public sealed class CopyHistoryViewSettings
{
	#region Properties
	/// <summary>
	/// Identifiers of the files kept in the copy history.
	/// </summary>
	/// <remarks>
	/// Don't remove the "set;" accessor, it's required for deserialization.
	/// </remarks>
	public ObservableCollection<Guid> ItemIds { get; set; } = [];

	/// <summary>
	/// The selected item identifier in <see cref="ItemIds" />.
	/// </summary>
	public Guid SelectedItemId { get; set; }
	#endregion

	#region Methods
	/// <summary>
	/// Adds objects to <see cref="ItemIds" /> from <paramref name="source" /> if they are in <paramref name="hierarchy" />.
	/// </summary>
	public void AddItemIds(IEnumerable<Guid> source, IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		source.ForEach(id =>
		{
			if (!hierarchy.ContainsFileBy(x => x.Id == id))
			{
				return;
			}

			ItemIds.Add(id);
		});
	}
	#endregion
}
