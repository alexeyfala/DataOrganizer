using DataOrganizer.Helpers.Security;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Builds key derivation costs for the tests.
/// </summary>
internal static class Argon2SettingsFactory
{
	#region Methods
	/// <summary>
	/// The lowest supported cost: a test whose subject is not the cost pays milliseconds
	/// instead of the memory a shipped blob is written with.
	/// </summary>
	public static Argon2Settings CreateLowestCost() => new(
		MemorySize: 8192,
		NumberOfPasses: 1,
		DegreeOfParallelism: 1);

	/// <summary>
	/// A cost other than the lowest one and as cheap: what a blob written before a change
	/// of the cost carries.
	/// </summary>
	public static Argon2Settings CreateOutdatedCost() => new(
		MemorySize: 16384,
		NumberOfPasses: 1,
		DegreeOfParallelism: 1);
	#endregion
}
