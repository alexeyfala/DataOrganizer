namespace DataOrganizer.DTO.Updates;

/// <summary>
/// Outcome of an application update check.
/// </summary>
public sealed record UpdateCheckResult
{
	#region Properties
	/// <summary>
	/// Result indicating that no newer version is available.
	/// </summary>
	public static UpdateCheckResult None { get; } = new();

	/// <summary>
	/// Indicates whether a newer version is available.
	/// </summary>
	public bool UpdateAvailable { get; init; }

	/// <summary>
	/// Version string of the newer release, when available.
	/// </summary>
	public string? LatestVersion { get; init; }

	/// <summary>
	/// Web page of the newer release, when available.
	/// </summary>
	public string? ReleaseUrl { get; init; }
	#endregion
}
