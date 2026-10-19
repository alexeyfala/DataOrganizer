using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace DataOrganizer.Interfaces.Views;

/// <summary>
/// Provides methods for creating views and their view models.
/// </summary>
public interface IViewFactory : IViewModelFactory
{
	#region Methods
	/// <summary>
	/// Creates a <see cref="UserControl" /> resolving constructor parameters from application's
	/// <see cref="ServiceCollection" />, except the ones supplied explicitly in
	/// <paramref name="args" /> — those are matched to constructor parameters by type
	/// and used as-is. The control itself is NOT registered with the root provider's
	/// tracked-disposables list.
	/// </summary>
	T CreateUserControl<T>(params object[] args) where T : UserControl;


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
