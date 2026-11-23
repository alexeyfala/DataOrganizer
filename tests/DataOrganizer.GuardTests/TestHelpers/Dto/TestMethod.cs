namespace DataOrganizer.GuardTests.TestHelpers.Dto;

/// <summary>
/// A method of a class in a test file, as its source writes it without comments and literals.
/// </summary>
internal sealed record TestMethod
{
	#region Properties
	/// <summary>
	/// Text of the method, from its modifiers to the end of its body.
	/// </summary>
	public required string Code { get; init; }

	/// <summary>
	/// Path of the test file inside the tests folder.
	/// </summary>
	public required string FilePath { get; init; }

	/// <summary>
	/// Whether the method is a test that NUnit runs.
	/// </summary>
	public required bool IsTest { get; init; }

	/// <summary>
	/// Name of the method.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Type the method returns, empty for a constructor.
	/// </summary>
	public required string ReturnType { get; init; }
	#endregion
}
