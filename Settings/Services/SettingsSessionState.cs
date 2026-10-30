using Settings.Interfaces;

namespace Settings.Services;

/// <inheritdoc cref="ISettingsSessionState" />
public sealed class SettingsSessionState : ISettingsSessionState
{
	#region Properties
	/// <inheritdoc />
	public int LastCategoryIndex { get; set; }
	#endregion
}
