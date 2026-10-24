using System;
using System.Reactive;

namespace Diagnostics.Interfaces;

/// <summary>
/// Log records meant for the console, kept until the console takes them.
/// </summary>
public interface IConsoleLog
{
	#region Properties
	/// <summary>
	/// Signals each written record on the thread that wrote it.
	/// </summary>
	IObservable<Unit> Written { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Takes the records written so far, oldest first, and keeps none of them.
	/// </summary>
	string[] TakeAll();

	/// <summary>
	/// Keeps <paramref name="record" /> for the console; any thread may write.
	/// </summary>
	void Write(string record);
	#endregion
}
