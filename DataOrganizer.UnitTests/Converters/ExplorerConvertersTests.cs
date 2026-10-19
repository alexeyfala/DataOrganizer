using Avalonia.Data.Converters;
using AwesomeAssertions;
using DataOrganizer.Converters;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions.Hotkeys;
using DataOrganizer.UnitTests.Factories;
using System.Globalization;
using System.Linq;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(ExplorerConverters)}"" type")]
internal class ExplorerConvertersTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExplorerConverters.HotkeysToToolTip" />: the tip of a file with hotkeys ends with their keys.
	/// </summary>
	[Test]
	public void HotkeysToToolTip_Lists_The_Keys_Of_The_Hotkeys()
	{
		// Arrange
		HotkeyDto[] hotkeys = [.. HotkeyFactory.CreateHotkeyDtos(3)];

		// Act
		object? result = Convert(ExplorerConverters.HotkeysToToolTip, hotkeys);

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.EndWith(hotkeys.ToKeyStrokes().ToArray().GetHotkeysPresentation());
	}

	/// <summary>
	/// <see cref="ExplorerConverters.HotkeysToToolTip" />: a file without hotkeys has no tip.
	/// </summary>
	[Test]
	public void HotkeysToToolTip_Without_Hotkeys_Is_Empty()
	{
		// Act
		object? result = Convert(ExplorerConverters.HotkeysToToolTip, []);

		// Assert
		result
			.Should()
			.BeNull();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Runs a converter over the hotkeys of a file and their count, as the binding hands them over.
	/// </summary>
	private static object? Convert(IMultiValueConverter converter, HotkeyDto[] hotkeys)
	{
		return converter.Convert(
			[hotkeys, hotkeys.Length],
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}
	#endregion
}
