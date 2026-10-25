using DataOrganizer.Interfaces.Runtime;

namespace DataOrganizer.Dto.Settings;

/// <summary>
/// Persisted settings of <c>EditorWindow</c>.
/// </summary>
public sealed class EditorWindowSettings : WindowPlacementSettings
{
	#region Properties
	/// <inheritdoc cref="ISessionReadOnlyMode.IsReadOnly" />
	public required bool IsReadOnly { get; init; }

	/// <summary>
	/// Width of the navigation column.
	/// </summary>
	public required double NavigationColumnWidth { get; init; }
	#endregion
}
