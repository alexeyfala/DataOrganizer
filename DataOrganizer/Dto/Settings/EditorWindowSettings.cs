using DataOrganizer.ViewModels.Windows;

namespace DataOrganizer.Dto.Settings;

/// <summary>
/// Persisted settings of <c>EditorWindow</c>.
/// </summary>
public sealed class EditorWindowSettings : WindowPlacementSettings
{
	#region Properties
	/// <inheritdoc cref="EditorViewModel.IsReadOnly" />
	public required bool IsReadOnly { get; init; }

	/// <inheritdoc cref="EditorViewModel.NavigationColumnWidth" />
	public required double NavigationColumnWidth { get; init; }
	#endregion
}
