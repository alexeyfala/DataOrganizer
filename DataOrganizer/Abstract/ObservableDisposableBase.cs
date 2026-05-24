using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Reactive.Disposables;
using System.Threading;

namespace DataOrganizer.Abstract;

public abstract class ObservableDisposableBase : ObservableObject, IDisposable
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the object has already been disposed.
	/// </summary>
	public bool IsDisposed => _isDisposed;
	#endregion

	#region Data
	/// <inheritdoc cref="CompositeDisposable" />
	protected readonly CompositeDisposable _disposables = [];

	/// <summary>
	/// Backing field for <see cref="IsDisposed" />.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		GC.SuppressFinalize(this);

		_disposables.Dispose();

		AfterDispose();
	}

	/// <summary>
	/// Called after main <see cref="Dispose" /> method.
	/// </summary>
	protected virtual void AfterDispose()
	{
	}
	#endregion
}
