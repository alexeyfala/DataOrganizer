namespace DataOrganizer.GuardTests;

/// <summary>
/// A container a test sets up with <c>AutoMock.GetLoose</c>: its registrations and the types the test builds from it.
/// </summary>
internal sealed record ContainerSetup
{
	#region Properties
	/// <summary>
	/// Types the test builds from the container.
	/// </summary>
	public required string[] BuiltTypes { get; init; }

	/// <summary>
	/// Path of the test file inside the tests folder.
	/// </summary>
	public required string FilePath { get; init; }

	/// <summary>
	/// Test method that sets the container up.
	/// </summary>
	public required string Method { get; init; }

	/// <summary>
	/// Registrations the test makes.
	/// </summary>
	public required ContainerRegistration[] Registrations { get; init; }
	#endregion
}
