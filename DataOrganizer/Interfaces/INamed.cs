namespace DataOrganizer.Interfaces;

/// <summary>
/// An object that carries a name.
/// </summary>
internal interface INamed
{
	#region Properties
	/// <summary>
	/// Name.
	/// </summary>
	public string Name { get; }
	#endregion
}
