using DataOrganizer.DTO.Entities;
using DataOrganizer.Extensions;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataOrganizer.DTO.Settings;

/// <summary>
/// The settings for "Copy History".
/// </summary>
public sealed class CopyHistoryViewSettings
{
	#region Properties
	/// <summary>
	/// Copy content history.
	/// </summary>
	/// <remarks>
	/// Don't remove the "set;" accessor, it's required for deserialization.
	/// </remarks>
	public ObservableCollection<Guid> Items { get; set; } = [];

	/// <summary>
	/// The selected item identifier in <see cref="Items" />.
	/// </summary>
	public Guid SelectedItemId { get; set; }
	#endregion

	#region Methods
	/// <summary>
	/// Adds objects to <see cref="Items" /> from <paramref name="source" /> if they are in <paramref name="hierarchy" />.
	/// </summary>
	public void AddItems(IEnumerable<Guid> source, IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		source.ForEach(id =>
		{
			if (!hierarchy.ContainsFileBy(x => x.Id == id))
			{
				return;
			}

			Items.Add(id);
		});
	}
	#endregion
}
