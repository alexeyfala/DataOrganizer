using Avalonia.Threading;
using System;

namespace DataOrganizer.Interfaces;

/// <inheritdoc cref="Dispatcher" />
public interface IUIThreadDispatcher
{
	#region Methods
	/// <inheritdoc cref="Dispatcher.Post(Action, DispatcherPriority)" />
	void Post(Action action, DispatcherPriority priority = default);
	#endregion
}
