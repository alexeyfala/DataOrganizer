using DataOrganizer.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides dialog boxes for interaction.
/// </summary>
public interface IDialogService
{
	#region Methods
	/// <summary>
	/// Requests the user to close files.
	/// </summary>
	Task<bool> RequestUserCloseFilesAsync(CancellationToken token = default);

	/// <summary>
	/// Requests a password from user.
	/// </summary>
	Task<string?> RequestUserPasswordAsync(
		string header,
		string? label = null,
		CancellationToken token = default);

	/// <summary>
	/// Selects import variant.
	/// </summary>
	Task<ImportListVariant> SelectImportVariantAsync(CancellationToken token = default);
	#endregion
}
