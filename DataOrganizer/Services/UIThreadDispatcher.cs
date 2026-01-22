using Avalonia.Threading;
using DataOrganizer.Interfaces;
using System;

namespace DataOrganizer.Services;

/// <inheritdoc cref="IUIThreadDispatcher" />
internal sealed class UIThreadDispatcher : IUIThreadDispatcher
{
	#region Methods
	/// <inheritdoc />
	public void Post(Action action, DispatcherPriority priority = default)
	{
		Dispatcher
			.UIThread
			.Post(action, priority);
	}
	#endregion
}
