using Microsoft.Extensions.DependencyInjection;
using System;

namespace DataOrganizer.Interfaces.Views;

/// <summary>
/// Provides a method for creating view models.
/// </summary>
public interface IViewModelFactory
{
	#region Methods
	/// <summary>
	/// Creates a view model resolving its constructor parameters from application's
	/// <see cref="ServiceCollection" />. The returned instance is NOT registered with
	/// the root provider's tracked-disposables list, so its <see cref="IDisposable.Dispose" />
	/// will be called only when the caller invokes it explicitly.
	/// </summary>
	T CreateViewModel<T>();
	#endregion
}
