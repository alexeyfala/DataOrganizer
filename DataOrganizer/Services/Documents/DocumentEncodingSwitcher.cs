using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Notifications;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System.Globalization;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Documents;

public sealed class DocumentEncodingSwitcher : IDocumentEncodingSwitcher
{
	#region Data
	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public DocumentEncodingSwitcher(
		IDispatcherAccessor dispatcher,
		ILogger logger,
		INotificationService notification)
	{
		_dispatcher = dispatcher;

		_logger = logger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<bool> SwitchAsync(
		IEncodedDocument editor,
		string? encoding,
		string snackbarHostIdentifier,
		string textName)
	{
		// The choice comes from a binding still writing it, which would not pass a refusal written back at once on to the
		// list of the encodings.
		return _dispatcher.PostAsync(() => Switch(
			editor,
			encoding,
			snackbarHostIdentifier,
			textName));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reads the text of an editor again in the chosen encoding; an encoding that cannot read its bytes leaves the text as
	/// it was.
	/// </summary>
	private bool Switch(
		IEncodedDocument editor,
		string? encoding,
		string snackbarHostIdentifier,
		string textName)
	{
		// The encoding of the text comes back here when it is shown and when a choice is refused, and a text not read yet
		// has no encoding to change.
		if (encoding is null || editor.Codec.Encoding is not { } current || encoding == current)
		{
			return false;
		}

		if (editor.Codec.Reread(editor.Document.Text, encoding) is not { } change)
		{
			editor.RefreshEncoding();

			_notification.ShowWarningSnackbar(Strings.EncodingNotChanged, snackbarHostIdentifier);

			return false;
		}

		if (change.Text is not { } text)
		{
			editor.RefreshEncoding();

			_notification.ShowWarningSnackbar(
				string.Format(
					CultureInfo.CurrentCulture,
					Strings.ContentsUnreadableInEncodingFormat,
					change.Name),
				snackbarHostIdentifier);

			_logger.LogWarning($"The text of {textName} cannot be read in {encoding}");

			return false;
		}

		editor.ReplaceText(text);

		return true;
	}
	#endregion
}
