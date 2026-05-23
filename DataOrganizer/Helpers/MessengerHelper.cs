using CommunityToolkit.Mvvm.Messaging;
using Shared.Extensions;
using System;
using System.Linq;

namespace DataOrganizer.Helpers;

/// <summary>
/// Reflection helpers for objects that participate in the CommunityToolkit messenger.
/// </summary>
internal static class MessengerHelper
{
	#region Methods
	/// <summary>
	/// Formats a log line describing the message channels <paramref name="recipient" /> is
	/// about to unsubscribe from. Returns <c>null</c> when the recipient implements no
	/// <see cref="IRecipient{TMessage}" /> interfaces, so the caller can skip the log entry.
	/// </summary>
	public static string? FormatUnsubscriptionLog<T>(T recipient)
	{
		ArgumentNullException.ThrowIfNull(recipient);

		Type[] subscriptions = GetSubscribedMessageTypes(recipient);

		if (subscriptions.Length == 0)
		{
			return null;
		}

		return $@"Unregistering ""{recipient.GetType().Name}"" from {subscriptions.Length} message channel(s):{Environment.NewLine}{subscriptions.Select(x => x.Name).SplitAsString("," + Environment.NewLine)}";
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the message types <paramref name="recipient" /> is subscribed to via
	/// <see cref="IRecipient{TMessage}" /> implementations on its runtime type.
	/// Mirrors the set of channels picked up by
	/// <see cref="IMessengerExtensions.RegisterAll(IMessenger, object)" />.
	/// </summary>
	private static Type[] GetSubscribedMessageTypes<T>(T recipient)
	{
		ArgumentNullException.ThrowIfNull(recipient);

		return [.. recipient
			.GetType()
			.GetInterfaces()
			.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRecipient<>))
			.Select(x => x.GetGenericArguments()[0])];
	}
	#endregion
}