using DataOrganizer.Infrastructure.Settings.Interfaces;

namespace DataOrganizer.Infrastructure.Settings.Services;

/// <inheritdoc cref="ISettingsSessionState" />
public sealed class SettingsSessionState : ISettingsSessionState
{
	#region Properties
	/// <inheritdoc />
	public int LastCategoryIndex { get; set; }
	#endregion
}
