using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Interfaces.Updates;
using DataOrganizer.Messages.Updates;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Updates;

/// <summary>
/// Hands the update question to the main window that is shown; with no window to answer, the update is declined.
/// </summary>
public sealed class UpdatePromptRelay : IUpdatePrompt
{
	#region Data
	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	public UpdatePromptRelay(IMessenger messenger) => _messenger = messenger;
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> ConfirmUpdateAsync(string text, CancellationToken token = default)
	{
		ConfirmUpdateMessage request = _messenger.Send(new ConfirmUpdateMessage(text, token));

		if (!request.HasReceivedResponse)
		{
			return false;
		}

		return await request.Response.ConfigureAwait(true);
	}
	#endregion
}
