namespace DataOrganizer.GuardTests.Dto;

/// <summary>
/// A registration a test makes in its container, as its source writes it.
/// </summary>
internal sealed record ContainerRegistration
{
	#region Properties
	/// <summary>
	/// Type the container builds for the registration, or <c>null</c> for an instance or a delegate.
	/// </summary>
	public required string? Implementation { get; init; }

	/// <summary>
	/// Services the registration provides, or <c>null</c> when the source does not show them.
	/// </summary>
	public required string[]? Services { get; init; }

	/// <summary>
	/// Registration call as the source writes it.
	/// </summary>
	public required string Text { get; init; }
	#endregion
}
