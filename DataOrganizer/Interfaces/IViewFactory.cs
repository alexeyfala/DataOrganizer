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
	/// Creates a <see cref="Window" /> from application's <see cref="ServiceCollection" />.
	/// </summary>
	T CreateWindow<T>() where T : Window;
	#endregion
}
