using DataOrganizer.Interfaces.Settings;

namespace DataOrganizer.Services.Settings;

internal sealed class SettingsSessionState : ISettingsSessionState
{
	#region Properties
	/// <inheritdoc />
	public int LastCategoryIndex { get; set; }
	#endregion
}
