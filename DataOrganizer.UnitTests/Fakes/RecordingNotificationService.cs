using DataOrganizer.Dto;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;

namespace DataOrganizer.UnitTests.Helpers;

/// <summary>
/// Test-only <see cref="INotificationService" /> that keeps the last message asked for,
/// making it observable in unit-test assertions.
/// </summary>
internal sealed class RecordingNotificationService : INotificationService
{
	#region Properties
	/// <summary>
	/// The message asked for last; <c>null</c> while none has been.
	/// </summary>
	public SnackbarContent? Shown { get; private set; }
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ShowErrorSnackbar(string text) => Show(text, SnackbarMessageLevel.Error);

	/// <inheritdoc />
	public void ShowInformationSnackbar(string text) => Show(text, SnackbarMessageLevel.Information);

	/// <inheritdoc />
	public void ShowToast(string message) => Show(message, SnackbarMessageLevel.Information);

	/// <inheritdoc />
	public void ShowWarningSnackbar(string text) => Show(text, SnackbarMessageLevel.Warning);
	#endregion

	#region Helpers
	/// <summary>
	/// Keeps the message instead of showing it.
	/// </summary>
	private void Show(string text, SnackbarMessageLevel level) => Shown = new(text, level);
	#endregion
}
