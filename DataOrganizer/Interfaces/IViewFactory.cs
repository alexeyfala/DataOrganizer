using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides methods for creating views.
/// </summary>
public interface IViewFactory
{
	#region Methods
	/// <summary>
	/// Creates a <see cref="UserControl" /> from application's <see cref="ServiceCollection" />.
	/// </summary>
	T CreateUserControl<T>() where T : UserControl;

	/// <summary>
	/// Creates a view model resolving its constructor parameters from application's
	/// <see cref="ServiceCollection" />. The returned instance is NOT registered with
	/// the root provider's tracked-disposables list, so its <see cref="IDisposable.Dispose" />
	/// will be called only when the caller invokes it explicitly.
	/// </summary>
	T CreateViewModel<T>();

	/// <summary>
	/// Creates a <see cref="Window" /> resolving constructor parameters from application's
	/// <see cref="ServiceCollection" />, except the ones supplied explicitly in
	/// <paramref name="args" /> — those are matched to constructor parameters by type
	/// and used as-is. The window itself is NOT registered with the root provider's
	/// tracked-disposables list.
	/// </summary>
	T CreateWindow<T>(params object[] args) where T : Window;
	#endregion
}
