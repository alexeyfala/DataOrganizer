namespace DataOrganizer.Enums.Dialogs;

/// <summary>
/// The button the user answered a dialog with.
/// </summary>
public enum YesNoCancelAnswer
{
	/// <summary>
	/// Refusal; a dialog without a "Cancel" button answers this when it is closed.
	/// </summary>
	No,

	/// <summary>
	/// The action is dropped; a dialog with a "Cancel" button answers this when it is closed.
	/// </summary>
	Cancel,

	/// <summary>
	/// Consent.
	/// </summary>
	Yes
}
