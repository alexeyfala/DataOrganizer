using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.UnitTests.TestHelpers.Factories;
using DataOrganizer.SystemIntegration.GlobalHotkeys.Services;
using DataOrganizer.SystemIntegration.UnitTests.GlobalHotkeys.TestHelpers.Factories;
using SharpHook.Data;

namespace DataOrganizer.SystemIntegration.UnitTests.GlobalHotkeys.Services;

/// <summary>
/// Tests of <see cref="UnreadableHotkeysFinder" /> type.
/// </summary>
[TestFixture]
internal sealed class UnreadableHotkeysFinderTests
{
	#region Methods
	/// <summary>
	/// <see cref="UnreadableHotkeysFinder.Find" />: finds the files anywhere in the tree whose hotkeys could not be read,
	/// and only them.
	/// </summary>
	[Test]
	public void Find_Finds_The_Files_Whose_Hotkeys_Could_Not_Be_Read()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FolderDto subfolder = ItemDtoFactory.CreateFolderDto();

		FileDto unreadableKey = HotkeyFactory.CreateFileWithHotkey(KeyCode.VcUndefined, EventMask.LeftCtrl);

		FileDto unreadableMask = HotkeyFactory.CreateFileWithHotkey(KeyCode.VcA, EventMask.None);

		subfolder
			.Children
			.Add(unreadableMask);

		folder
			.Children
			.Add(unreadableKey);

		folder
			.Children
			.Add(HotkeyFactory.CreateFileWithHotkey(KeyCode.VcA, EventMask.LeftCtrl));

		folder
			.Children
			.Add(subfolder);

		using AutoMock mock = AutoMock.GetLoose();

		UnreadableHotkeysFinder sut = mock.Create<UnreadableHotkeysFinder>();

		// Act
		FileDto[] result = sut.Find([folder, ItemDtoFactory.CreateFileDto()]);

		// Assert
		result
			.Should()
			.HaveCount(2)
			.And
			.Contain(unreadableKey)
			.And
			.Contain(unreadableMask);
	}
	#endregion
}
