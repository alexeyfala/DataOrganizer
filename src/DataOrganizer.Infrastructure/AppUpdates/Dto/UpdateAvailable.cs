namespace DataOrganizer.Infrastructure.AppUpdates.Dto;

/// <summary>
/// A newer version is available.
/// </summary>
public sealed record UpdateAvailable : UpdateCheckResult
{
	#region Properties
	/// <summary>
	/// Version string of the newer release.
	/// </summary>
	public required string LatestVersion { get; init; }

	/// <summary>
	/// Web page of the newer release; <c>null</c> when the release names none.
	/// </summary>
	public required string? ReleaseUrl { get; init; }
	#endregion
}
