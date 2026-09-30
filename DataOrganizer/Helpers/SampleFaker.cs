using Bogus;

namespace DataOrganizer.Helpers;

/// <summary>
/// Source of the made-up values of the samples, the same from run to run.
/// </summary>
internal static class SampleFaker
{
	#region Data
	/// <summary>
	/// Seed of the made-up values, which keeps them the same from run to run.
	/// </summary>
	private const int Seed = 20260930;
	#endregion

	#region Methods
	/// <summary>
	/// Creates a generator of made-up values that starts from the same seed each time.
	/// </summary>
	public static Faker Create() => new()
	{
		Random = new Randomizer(Seed)
	};
	#endregion
}
