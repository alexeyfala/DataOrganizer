using DialogContracts.Dto;
using Repository.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DialogContracts.Interfaces;

/// <summary>
/// Shows the dialogs of a new object, of the properties and of the application settings.
/// </summary>
public interface IDialogService
{
	#region Methods
	/// <summary>
	/// Displays the entity creation dialog.
	/// </summary>
	Task<EntityCreationResult?> ShowEntityCreationAsync(CancellationToken token = default);

	/// <summary>
	/// Shows a properties dialog.
	/// </summary>
	void ShowProperties(IEnumerable<PropertyDescription> properties);

	/// <summary>
	/// Shows application settings.
	/// </summary>
	Task ShowSettingsAsync();
	#endregion
}
