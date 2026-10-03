using DataOrganizer.Dto.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Hierarchy;

/// <summary>
/// Adds objects made up for trying the application to the database.
/// </summary>
public interface ISampleSeeder
{
	#region Methods
	/// <summary>
	/// Adds a run of samples after the objects of the root, encrypts the folders of the run meant to be encrypted, damages
	/// the files meant to be broken and returns the folder of the run; the hotkeys of the samples keep clear of those saved.
	/// </summary>
	Task<FolderDto> SeedAsync(CancellationToken token = default);

	/// <summary>
	/// Adds a run of many samples with large contents after the objects of the root and returns the folder of the run.
	/// </summary>
	Task<FolderDto> SeedLargeAsync(CancellationToken token = default);
	#endregion
}
