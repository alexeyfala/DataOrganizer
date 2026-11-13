using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Documents.Editor.Messages;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Documents.Editor.Extensions;

/// <summary>
/// Sends the requests of the shell to the open editors: the flush of their pending changes.
/// </summary>
public static class MessengerExtensions
{
	#region Methods
	/// <summary>
	/// Asks every open editor to persist its pending changes and awaits all of them; <c>True</c> when all did.
	/// </summary>
	public static async Task<bool> FlushEditorsAsync(this IMessenger messenger, CancellationToken token = default)
	{
		FlushEditorsMessage request = new();

		messenger.Send(request);

		IReadOnlyCollection<bool> responses = await request
			.GetResponsesAsync(token)
			.ConfigureAwait(true);

		return responses.All(x => x);
	}
	#endregion
}
