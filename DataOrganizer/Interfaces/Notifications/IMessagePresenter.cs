namespace DataOrganizer.Interfaces;

/// <summary>
/// Shows one message at a time on a surface of its own.
/// </summary>
public interface IMessagePresenter<in TContent>
{
	#region Properties
	/// <summary>
	/// <c>True</c> while the surface is able to show messages.
	/// </summary>
	bool CanShow { get; }

	/// <summary>
	/// <c>True</c> while the pointer rests on the shown message.
	/// </summary>
	bool IsPointerOverMessage { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Shows a message until it is removed.
	/// </summary>
	void Post(TContent content);

	/// <summary>
	/// Takes the shown message off the screen.
	/// </summary>
	void Remove();
	#endregion
}
