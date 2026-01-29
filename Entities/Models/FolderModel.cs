using Entities.Abstract;
using System.Collections.ObjectModel;

namespace Entities.Models;

/// <summary>
/// Folder model in a virtual file system.
/// </summary>
public sealed class FolderModel : ExplorerModelBase
{
	#region Properties
	/// <summary>
	/// Child objects.
	/// </summary>
	public Collection<ExplorerModelBase> Children { get; } = [];

	/// <summary>
	/// Returns <c>True</c> if the folder is expanded.
	/// </summary>
	public bool IsExpanded { get; init; }

	/// <summary>
	/// Hash of password for encryption.
	/// </summary>
	public string? PasswordHash { get; init; }
	#endregion
}
