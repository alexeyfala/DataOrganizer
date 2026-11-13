using Autofac;
using Avalonia.Controls;
using DataOrganizer.Interfaces.Views;
using System.Linq;

namespace DataOrganizer.UnitTests.Fakes;

/// <summary>
/// Test-only <see cref="IViewFactory" /> that resolves every view and view model in a new scope of the test container,
/// so each call gives a new instance, as the application's factory does.
/// </summary>
internal sealed class ScopeViewFactory : IViewFactory
{
	#region Data
	/// <inheritdoc cref="ILifetimeScope" />
	private readonly ILifetimeScope _scope;
	#endregion

	#region Constructors
	public ScopeViewFactory(ILifetimeScope scope) => _scope = scope;
	#endregion

	#region Methods
	/// <inheritdoc />
	public T CreateUserControl<T>(params object[] args) where T : UserControl => Resolve<T>(args);

	/// <inheritdoc />
	public T CreateViewModel<T>() => Resolve<T>([]);

	/// <inheritdoc />
	public T CreateWindow<T>(params object[] args) where T : Window => Resolve<T>(args);
	#endregion

	#region Helpers
	/// <summary>
	/// Resolves <typeparamref name="T" /> in a new scope, with <paramref name="args" /> matched to its parameters by type.
	/// </summary>
	private T Resolve<T>(object[] args)
	{
		return (T)_scope
			.BeginLifetimeScope()
			.Resolve(typeof(T), args.Select(static x => new TypedParameter(x.GetType(), x)));
	}
	#endregion
}
