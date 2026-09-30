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
	/// Adds a run of samples after the objects of the root and encrypts the folders of the run meant to be encrypted;
	/// a sample whose hotkey clashes with one already saved goes without it.
	/// </summary>
	Task SeedAsync(CancellationToken token = default);
	#endregion
}
