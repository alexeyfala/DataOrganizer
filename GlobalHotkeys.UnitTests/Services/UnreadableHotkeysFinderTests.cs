using Autofac.Extras.Moq;
using AwesomeAssertions;
using GlobalHotkeys.Services;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using SharpHook.Data;
using System;

namespace GlobalHotkeys.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(UnreadableHotkeysFinder)}"" type")]
internal class UnreadableHotkeysFinderTests
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

		FileDto unreadableKey = CreateFileWithHotkey(KeyCode.VcUndefined, EventMask.LeftCtrl);

		FileDto unreadableMask = CreateFileWithHotkey(KeyCode.VcA, EventMask.None);

		subfolder
			.Children
			.Add(unreadableMask);

		folder
			.Children
			.Add(unreadableKey);

		folder
			.Children
			.Add(CreateFileWithHotkey(KeyCode.VcA, EventMask.LeftCtrl));

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

	#region Helpers
	/// <summary>
	/// Creates a file with one hotkey of the given key and mask.
	/// </summary>
	private static FileDto CreateFileWithHotkey(KeyCode code, EventMask mask)
	{
		FileDto file = ItemDtoFactory.CreateFileDto();

		file
			.Hotkeys
			.Add(new()
			{
				Code = code,
				Id = Guid.NewGuid(),
				Index = 0,
				Mask = mask,
				OwnerId = file.Id
			});

		return file;
	}
	#endregion
}
