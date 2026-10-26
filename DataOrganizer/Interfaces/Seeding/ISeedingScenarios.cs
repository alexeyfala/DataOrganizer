using Hierarchy.Dto;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Seeding;

/// <summary>
/// Adds made-up samples for trying the application: objects to the database and the tree, entries to the clipboard
/// history; a failure is written into the log.
/// </summary>
public interface ISeedingScenarios
{
	#region Methods
	/// <summary>
	/// Adds entries made up for trying the clipboard history to it.
	/// </summary>
	Task SeedClipboardHistoryAsync();

	/// <summary>
	/// Adds a run of many sample objects with large contents to the database and the hierarchy, with the progress bar on.
	/// </summary>
	Task SeedLargeSamplesAsync(Collection<ExplorerItemDtoBase> hierarchy);

	/// <summary>
	/// Adds a run of sample objects to the database and the hierarchy, with the progress bar on.
	/// </summary>
	Task SeedSamplesAsync(Collection<ExplorerItemDtoBase> hierarchy);
	#endregion
}
