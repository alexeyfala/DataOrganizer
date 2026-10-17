using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Settings;

namespace DataOrganizer.Interfaces.Settings;

/// <summary>
/// Settings being edited in the settings session, before they are saved or discarded.
/// </summary>
public interface ISettingsDraft
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the edited settings differ from the saved ones.
	/// </summary>
	bool IsDirty { get; }

	/// <summary>
	/// <c>True</c> when the edits are discarded.
	/// </summary>
	bool IsDiscarded { get; }

	/// <summary>
	/// <c>True</c> when the edits are to be saved.
	/// </summary>
	bool IsSaved { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Starts a session on a copy of the saved settings and returns the copy to edit.
	/// </summary>
	AppSettings Begin();

	/// <summary>
	/// Marks the edits discarded.
	/// </summary>
	void Discard();

	/// <summary>
	/// Shows a theme before it is saved.
	/// </summary>
	void PreviewTheme(ThemeMode theme, ThemePrimaryColor primaryColor, ThemeSecondaryColor secondaryColor);

	/// <summary>
	/// Marks the edits to be saved.
	/// </summary>
	void Save();
	#endregion
}
