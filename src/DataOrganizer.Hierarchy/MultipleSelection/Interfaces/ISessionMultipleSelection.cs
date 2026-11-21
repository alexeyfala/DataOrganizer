namespace DataOrganizer.Hierarchy.MultipleSelection.Interfaces;

/// <summary>
/// Multiple selection mode of the tree for the work session.
/// </summary>
public interface ISessionMultipleSelection
{
	#region Properties
	/// <summary>
	/// <c>True</c> while the check of every shown object is on.
	/// </summary>
	bool IsCheckAll { get; set; }

	/// <summary>
	/// <c>True</c> while the tree shows check boxes.
	/// </summary>
	bool IsMultipleSelection { get; set; }
	#endregion
}
