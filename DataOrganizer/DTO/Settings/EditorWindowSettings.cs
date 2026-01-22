using Avalonia.Controls;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;

namespace DataOrganizer.DTO.Settings;

public sealed class EditorWindowSettings : PositionSizeSettings
{
	#region Properties
	/// <inheritdoc cref="EditorViewModel.IsReadOnly" />
	public required bool IsReadOnly { get; init; }

	/// <inheritdoc cref="INavigationColumnViewModel.NavigationColumnWidth" />
	public required double NavigationColumnWidth { get; init; }

	/// <inheritdoc cref="Avalonia.Controls.WindowState" />
	public required WindowState WindowState { get; init; }
	#endregion
}
