using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Messages;
using System;
using System.Threading;

namespace DataOrganizer.Helpers;

/// <summary>
/// Hides the progress bar of the editor when disposed, so the operation showing it cannot leave it on.
/// </summary>
internal sealed class ProgressScope : IDisposable
{
	#region Data
	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <summary>
	/// <c>True</c> when the progress bar has already been hidden.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public ProgressScope(IMessenger messenger) => _messenger = messenger;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		_messenger.Send(new ShowProgressBarMessage(false));
	}
	#endregion
}
