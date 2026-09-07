using DataOrganizer.DTO;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;

namespace DataOrganizer.UnitTests.Helpers;

/// <summary>
/// Test-only <see cref="ISnackbarService" /> that keeps the last message asked for,
/// making it observable in unit-test assertions.
/// </summary>
internal sealed class RecordingSnackbarService : ISnackbarService
{
	#region Properties
	/// <summary>
	/// The message asked for last; <c>null</c> while none has been.
	/// </summary>
	public SnackbarContent? Shown { get; private set; }
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ShowError(string text) => Show(text, SnackbarMessageLevel.Error);

	/// <inheritdoc />
	public void ShowInformation(string text) => Show(text, SnackbarMessageLevel.Information);

	/// <inheritdoc />
	public void ShowWarning(string text) => Show(text, SnackbarMessageLevel.Warning);
	#endregion

	#region Helpers
	/// <summary>
	/// Keeps the message instead of showing it.
	/// </summary>
	private void Show(string text, SnackbarMessageLevel level) => Shown = new(text, level);
	#endregion
}
