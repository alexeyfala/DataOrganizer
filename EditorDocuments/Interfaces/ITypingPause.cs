using System;

namespace EditorDocuments.Interfaces;

/// <summary>
/// Runs an action on the UI thread once typing pauses; disposing it drops an action that still waits.
/// </summary>
public interface ITypingPause : IDisposable
{
	#region Methods
	/// <summary>
	/// Runs the waiting action at once.
	/// </summary>
	void Flush();

	/// <summary>
	/// Puts the action off until typing pauses again.
	/// </summary>
	void Restart();
	#endregion
}
