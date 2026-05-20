using Avalonia.Controls;
using DataOrganizer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DataOrganizer.Services;

/// <inheritdoc cref="IViewFactory" />
internal sealed class ViewFactory : IViewFactory
{
	#region Data
	/// <inheritdoc cref="IServiceProvider" />
	private readonly IServiceProvider _serviceProvider;
	#endregion

	#region Constructors
	public ViewFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
	#endregion

	#region Methods
	/// <inheritdoc />
	public T CreateViewModel<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);

	/// <inheritdoc />
	public T CreateWindow<T>(params object[] args) where T : Window
		=> ActivatorUtilities.CreateInstance<T>(_serviceProvider, args);

	/// <inheritdoc />
	public T CreateUserControl<T>() where T : UserControl => _serviceProvider.GetRequiredService<T>();
	#endregion
}
