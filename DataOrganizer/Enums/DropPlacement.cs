namespace DataOrganizer.Enums;

/// <summary>
/// Placement of a dropped record relative to the target element.
/// </summary>
public enum DropPlacement
{
	/// <summary>
	/// Above the target element, as its sibling.
	/// </summary>
	Before,

	/// <summary>
	/// Below the target element, as its sibling.
	/// </summary>
	After,

	/// <summary>
	/// Inside the target group, as its child.
	/// </summary>
	Into
}
