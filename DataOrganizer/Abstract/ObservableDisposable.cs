using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Reactive.Disposables;

namespace DataOrganizer.Abstract;

public abstract class ObservableDisposable : ObservableObject, IDisposable
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> if the object has been disposed.
	/// </summary>
	public bool IsDisposed { get; private set; }
	#endregion

	#region Data
	/// <inheritdoc cref="CompositeDisposable" />
	protected readonly CompositeDisposable _disposables = [];
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		GC.SuppressFinalize(this);

		_disposables.Dispose();

		AfterDispose();

		IsDisposed = true;
	}

	/// <summary>
	/// Called after main <see cref="Dispose" /> method.
	/// </summary>
	protected virtual void AfterDispose()
	{
	}
	#endregion
}
