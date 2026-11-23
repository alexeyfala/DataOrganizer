namespace DataOrganizer.GuardTests.TestHelpers.Dto;

/// <summary>
/// A container a test sets up with <c>AutoMock.GetLoose</c>: its registrations, the statements of its setup and the types
/// the test builds from it.
/// </summary>
internal sealed record ContainerSetup
{
	#region Properties
	/// <summary>
	/// Name of the builder the setup takes.
	/// </summary>
	public required string Builder { get; init; }

	/// <summary>
	/// Types the test builds from the container.
	/// </summary>
	public required string[] BuiltTypes { get; init; }

	/// <summary>
	/// Code of the test method after the setup, the setups of all its containers left out.
	/// </summary>
	public required string CodeAfter { get; init; }

	/// <summary>
	/// Code of the test method before the setup, the setups of all its containers left out.
	/// </summary>
	public required string CodeBefore { get; init; }

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

	/// <summary>
	/// Statements of the setup in their order, its expression alone when it has no block.
	/// </summary>
	public required string[] Statements { get; init; }
	#endregion
}
