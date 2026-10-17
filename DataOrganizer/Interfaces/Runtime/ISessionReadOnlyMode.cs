namespace DataOrganizer.Interfaces.Runtime;

/// <summary>
/// Read-only mode of the editor for the work session.
/// </summary>
public interface ISessionReadOnlyMode
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the contents cannot be edited.
	/// </summary>
	bool IsReadOnly { get; set; }
	#endregion
}
