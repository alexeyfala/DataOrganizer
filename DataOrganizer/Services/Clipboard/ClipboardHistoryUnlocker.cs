using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Notifications;
using Shared.Properties;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

/// <inheritdoc cref="IClipboardHistoryUnlocker" />
public sealed class ClipboardHistoryUnlocker : IClipboardHistoryUnlocker
{
	#region Data
	/// <inheritdoc cref="IClipboardLogPersistenceCoordinator" />
	private readonly IClipboardLogPersistenceCoordinator _clipboardLogPersistence;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="IPasswordPrompt" />
	private readonly IPasswordPrompt _passwordPrompt;
	#endregion

	#region Constructors
	public ClipboardHistoryUnlocker(
		IClipboardLogPersistenceCoordinator clipboardLogPersistence,
		INotificationService notification,
		IPasswordPrompt passwordPrompt)
	{
		_clipboardLogPersistence = clipboardLogPersistence;

		_notification = notification;

		_passwordPrompt = passwordPrompt;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task UnlockIfRequiredAsync()
	{
		string? label = null;

		while (_clipboardLogPersistence.RequiresUnlock)
		{
			// The first password of the history is created here, every later one is only checked.
			bool hasPassword = _clipboardLogPersistence.HasPassword;

			label ??= hasPassword
				? Strings.Password
				: Strings.NewPassword;

			using PinnedSecret password = await _passwordPrompt.RequestPasswordAsync(
				header: Strings.ClipboardHistory,
				label: label,
				description: hasPassword
					? Strings.SavedHistoryPasswordDescription
					: Strings.NewSavedHistoryPasswordDescription,
				mode: hasPassword
					? PasswordPromptMode.Verify
					: PasswordPromptMode.Create).ConfigureAwait(true);

			if (password.IsEmpty)
			{
				return;
			}

			using PinnedBuffer passwordBytes = password.ToUtf8Buffer();

			ClipboardLogStatus status = await _clipboardLogPersistence
				.TryUnlockAndMergeAsync(passwordBytes)
				.ConfigureAwait(true);

			if (status == ClipboardLogStatus.Unlocked)
			{
				return;
			}

			// Only a wrong password is worth asking again; the rest no password can fix.
			if (status != ClipboardLogStatus.WrongPassword)
			{
				string text = status == ClipboardLogStatus.Damaged
					? Strings.EncryptedDataIsDamaged
					: Strings.FailedToUnlockClipboardHistory;

				_notification.ShowErrorSnackbar(text);

				return;
			}

			label = $"{Strings.IncorrectPassword}. {Strings.TryAgain}";
		}
	}
	#endregion
}
