using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Windows;
using Shared.Common;
using System.Linq;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(EditorWindow)}"" type")]
internal class EditorWindowTests
{
	#region Methods
	/// <summary>
	/// <see cref="EditorWindow()" />: the separator and the items of the sample seeding at the end of the menu are shown only
	/// in a debug build.
	/// </summary>
	[AvaloniaTest]
	public void Constructor_Shows_The_Seeding_Items_Only_In_A_Debug_Build()
	{
		// Act
		EditorWindow sut = new();

		// Assert
		Control[] items =
		[
			sut.SeedingSeparator,
			sut.SampleSeeding,
			sut.LargeSampleSeeding
		];

		// The failure names the elements: in an optimized build the message can quote another assertion.
		items.Where(x => x.IsVisible != AppInfo.IsDebug).Select(x => x.Name)
			.Should()
			.BeEmpty();
	}
	#endregion
}
