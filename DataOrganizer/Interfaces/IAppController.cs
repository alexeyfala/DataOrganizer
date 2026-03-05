using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.Windows;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Performs application lifecycle control.
/// </summary>
public interface IAppController
{
	#region Methods
	/// <summary>
	/// Launches the application.
	/// </summary>
	Task LaunchAppAsync(ConsoleWindow? console, CancellationToken token = default);

	/// <summary>
	/// Loads all <see cref="FolderModel" /> and all <see cref="FileModel" /> from database<br />
	/// then maps it to hierarchy of <see cref="ExplorerModelBaseDto" /> and returns.
	/// </summary>
	Task<ExplorerModelBaseDto[]> LoadAllHierarchyFromDbAsync(CancellationToken token = default);
	#endregion
}
