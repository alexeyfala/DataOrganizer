using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Threading;

namespace DataOrganizer.Messages.Updates;

/// <summary>
/// Request asking the main window whether to open the page of a newer application version.
/// </summary>
public sealed class ConfirmUpdateMessage : AsyncRequestMessage<bool>
{
	#region Properties
	/// <summary>
	/// The text of the question.
	/// </summary>
	public string Text { get; }

	/// <summary>
	/// Cancels the question.
	/// </summary>
	public CancellationToken Token { get; }
	#endregion

	#region Constructors
	public ConfirmUpdateMessage(string text, CancellationToken token)
	{
		Text = text;

		Token = token;
	}
	#endregion
}
