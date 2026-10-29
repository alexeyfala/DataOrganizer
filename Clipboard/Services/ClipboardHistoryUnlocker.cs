using Clipboard.Enums;
using Clipboard.Interfaces;
using DataProtection.Enums;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using Notifications.Interfaces;
using Shared.Properties;
using System.Threading.Tasks;

namespace Clipboard.Services;

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

			// Stryker disable once all : the wording of the prompt
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

			// Stryker disable once Block,Statement : only an error message follows
			if (status == ClipboardLogStatus.Unlocked)
			{
				return;
			}

			// Only a wrong password is worth asking again; the rest no password can fix.
			if (status != ClipboardLogStatus.WrongPassword)
			{
				// Stryker disable once all : the wording of the message
				string text = status == ClipboardLogStatus.Damaged
					? Strings.EncryptedDataIsDamaged
					: Strings.FailedToUnlockClipboardHistory;

				_notification.ShowErrorSnackbar(text);

				return;
			}

			// Stryker disable once all : the wording of the prompt
			label = $"{Strings.IncorrectPassword}. {Strings.TryAgain}";
		}
	}
	#endregion
}
