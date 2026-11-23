using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.SystemIntegration.Clipboard.Enums;
using DataOrganizer.SystemIntegration.Clipboard.Messages;
using System.Collections.Generic;

namespace DataOrganizer.SystemIntegration.UnitTests.Clipboard.TestHelpers;

/// <summary>
/// Messages of the clipboard history that a test records.
/// </summary>
internal static class MessengerExtensions
{
	#region Methods
	/// <summary>
	/// Records the kinds of every <see cref="ClipboardLogChangedMessage" /> sent on <paramref name="messenger" />.
	/// </summary>
	public static List<ClipboardLogChangeKind> CaptureChanges(this IMessenger messenger)
	{
		List<ClipboardLogChangeKind> received = [];

		messenger.Register<ClipboardLogChangedMessage>(
			received,
			static (recipient, message) => ((List<ClipboardLogChangeKind>)recipient).Add(message.Kind));

		return received;
	}

	/// <summary>
	/// Records every <see cref="ClipboardLogEntryCountChangedMessage" /> sent on <paramref name="messenger" />.
	/// </summary>
	public static List<ClipboardLogEntryCountChangedMessage> CaptureCountChanges(this IMessenger messenger)
	{
		List<ClipboardLogEntryCountChangedMessage> received = [];

		messenger.Register<ClipboardLogEntryCountChangedMessage>(
			received,
			static (recipient, message) => ((List<ClipboardLogEntryCountChangedMessage>)recipient).Add(message));

		return received;
	}
	#endregion
}
