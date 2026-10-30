using Diagnostics.Interfaces;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;

namespace Diagnostics.Services;

/// <inheritdoc cref="IConsoleLog" />
public sealed class ConsoleLog : IConsoleLog
{
	#region Properties
	/// <inheritdoc />
	public IObservable<Unit> Written => _written;
	#endregion

	#region Data
	/// <inheritdoc cref="Lock" />
	private readonly Lock _mutex = new();

	/// <summary>
	/// Records waiting for the console.
	/// </summary>
	private readonly List<string> _records = [];

	/// <summary>
	/// Source of <see cref="Written" />, serialized for writers on several threads.
	/// </summary>
	private readonly ISubject<Unit> _written = Subject.Synchronize(new Subject<Unit>());
	#endregion

	#region Methods
	/// <inheritdoc />
	public string[] TakeAll()
	{
		lock (_mutex)
		{
			string[] records = [.. _records];

			_records.Clear();

			return records;
		}
	}

	/// <inheritdoc />
	public void Write(string record)
	{
		lock (_mutex)
		{
			_records.Add(record);
		}

		_written.OnNext(Unit.Default);
	}
	#endregion
}
