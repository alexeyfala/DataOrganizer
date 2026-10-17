using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Helpers;
using DataOrganizer.Messages.Editor;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Extensions;

/// <summary>
/// Sends the requests and notifications of the shell: the flush of the open editors and the progress bar of the editor.
/// </summary>
public static class MessengerExtensions
{
	#region Methods
	/// <summary>
	/// Asks every open editor to persist its pending changes and awaits all of them; <c>True</c> when all did.
	/// </summary>
	internal static async Task<bool> FlushEditorsAsync(this IMessenger messenger, CancellationToken token = default)
	{
		FlushEditorsMessage request = new();

		messenger.Send(request);

		IReadOnlyCollection<bool> responses = await request
			.GetResponsesAsync(token)
			.ConfigureAwait(true);

		return responses.All(x => x);
	}

	/// <summary>
	/// Shows the progress bar of the editor; the returned scope hides it again.
	/// </summary>
	internal static ProgressScope ShowProgress(this IMessenger messenger)
	{
		messenger.Send(new ShowProgressBarMessage(true));

		return new(messenger);
	}
	#endregion
}
