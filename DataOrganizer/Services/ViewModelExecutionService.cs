using Avalonia;
using Avalonia.Threading;
using DataOrganizer.Abstract;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using System;

namespace DataOrganizer.Services;

public sealed class ViewModelExecutionService : IViewModelExecutionService
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;
	#endregion

	#region Constructors
	public ViewModelExecutionService(Application app, IDispatcher dispatcher)
	{
		_app = app;

		_dispatcher = dispatcher;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ExecuteInBaseViewModel(Action<ViewModelBase> action) => _dispatcher.Post(() =>
	{
		if (_app.FindBaseDataContext() is not { } viewModel)
		{
			return;
		}

		action(viewModel);
	});
	#endregion
}
