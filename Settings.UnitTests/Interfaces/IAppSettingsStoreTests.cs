using AwesomeAssertions;
using Settings.Interfaces;
using System.Linq;

namespace Settings.UnitTests.Interfaces;

[TestFixture(Description = $@"Tests of ""{nameof(IAppSettingsStore)}"" type")]
internal class IAppSettingsStoreTests
{
	#region Methods
	/// <summary>
	/// <see cref="IAppSettingsStore.Languages" />: the app speaks English and Russian, English first.
	/// </summary>
	[Test]
	public void Languages_Are_English_And_Russian()
	{
		// Act
		string[] result = [.. IAppSettingsStore.Languages.Select(static x => x.Name)];

		// Assert
		result
			.Should()
			.Equal("en-US", "ru-RU");
	}
	#endregion
}
