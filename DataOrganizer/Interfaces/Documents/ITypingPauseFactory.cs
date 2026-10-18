using System;

namespace DataOrganizer.Interfaces.Documents;

/// <summary>
/// Creates pauses in typing.
/// </summary>
public interface ITypingPauseFactory
{
	#region Methods
	/// <summary>
	/// Creates a pause in typing that runs <paramref name="action" />.
	/// </summary>
	ITypingPause Create(Action action);
	#endregion
}
