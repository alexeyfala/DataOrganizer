using AwesomeAssertions;
using DataOrganizer.Database.Repository.Dto;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.UnitTests.TestHelpers.Factories;
using DataOrganizer.Shared.Helpers;
using DataOrganizer.SystemIntegration.GlobalHotkeys.Extensions;
using DataOrganizer.SystemIntegration.UnitTests.GlobalHotkeys.TestHelpers.Factories;
using SharpHook.Data;
using System;
using System.Linq;

namespace DataOrganizer.SystemIntegration.UnitTests.GlobalHotkeys.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(HotkeyExtensions)}"" type")]
internal class HotkeyExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="HotkeyExtensions.GetFilesWithUnreadableHotkeys" />: returns a file whose hotkey holds an unreadable key.
	/// </summary>
	[Test]
	public void GetFilesWithUnreadableHotkeys_Returns_File_With_Unreadable_Key()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FileDto damaged = HotkeyFactory.CreateFileWithHotkey(KeyCode.VcUndefined, EventMask.LeftCtrl);

		root.Children.Add(damaged);

		root.Children.Add(HotkeyFactory.CreateFileWithHotkey(KeyCode.VcA, EventMask.LeftCtrl));

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		FileDto[] result = [.. hierarchy.GetFilesWithUnreadableHotkeys()];

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(damaged);
	}

	/// <summary>
	/// <see cref="HotkeyExtensions.GetFilesWithUnreadableHotkeys" />: returns a file whose hotkey holds an unreadable mask.
	/// </summary>
	[Test]
	public void GetFilesWithUnreadableHotkeys_Returns_File_With_Unreadable_Mask()
	{
		// Arrange
		FileDto damaged = HotkeyFactory.CreateFileWithHotkey(KeyCode.VcA, EventMask.None);

		ExplorerItemDtoBase[] hierarchy = [damaged];

		// Act
		FileDto[] result = [.. hierarchy.GetFilesWithUnreadableHotkeys()];

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(damaged);
	}

	/// <summary>
	/// <see cref="HotkeyExtensions.GetFilesWithUnreadableHotkeys" />: skips a file that has no hotkeys.
	/// </summary>
	[Test]
	public void GetFilesWithUnreadableHotkeys_Skips_File_Without_Hotkeys()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [ItemDtoFactory.CreateFileDto()];

		// Act
		FileDto[] result = [.. hierarchy.GetFilesWithUnreadableHotkeys()];

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="HotkeyExtensions.GetFilesWithUnreadableHotkeys" />: skips a file whose hotkeys are readable.
	/// </summary>
	[Test]
	public void GetFilesWithUnreadableHotkeys_Skips_Readable_Hotkeys()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy =
		[
			HotkeyFactory.CreateFileWithHotkey(KeyCode.VcA, EventMask.LeftCtrl),
			HotkeyFactory.CreateFileWithHotkey(KeyCode.VcB, EventMask.LeftShift | EventMask.LeftAlt)
		];

		// Act
		FileDto[] result = [.. hierarchy.GetFilesWithUnreadableHotkeys()];

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="HotkeyExtensions.GetHotkeysPresentation" />: includes the mask prefix when it is not the default value.
	/// </summary>
	[Test]
	public void GetHotkeysPresentation_Includes_Mask_When_Not_Default()
	{
		// Arrange
		KeyStroke[] keyStrokes =
		[
			new() { Code = KeyCode.VcA, Mask = EventMask.LeftCtrl },
			new() { Code = KeyCode.VcB, Mask = EventMask.None }
		];

		// Act
		string result = keyStrokes.GetHotkeysPresentation();

		// Assert
		result
			.Should()
			.StartWith("LeftCtrl + ");

		result
			.Should()
			.Contain("A, B");
	}

	/// <summary>
	/// <see cref="HotkeyExtensions.GetHotkeysPresentation" />: returns an empty string for an empty sequence.
	/// </summary>
	[Test]
	public void GetHotkeysPresentation_Returns_Empty_String_For_Empty_Sequence()
	{
		// Arrange
		KeyStroke[] keyStrokes = [];

		// Act
		string result = keyStrokes.GetHotkeysPresentation();

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="HotkeyExtensions.GetHotkeysPresentation" />: omits the mask prefix when it is the default value.
	/// </summary>
	[Test]
	public void GetHotkeysPresentation_Skips_Mask_When_Default()
	{
		// Arrange
		KeyStroke[] keyStrokes =
		[
			new() { Code = KeyCode.VcA, Mask = EventMask.None },
			new() { Code = KeyCode.VcB, Mask = EventMask.None }
		];

		// Act
		string result = keyStrokes.GetHotkeysPresentation();

		// Assert
		result
			.Should()
			.Be("A, B");
	}

	/// <summary>
	/// <see cref="HotkeyExtensions.GetUnreadableHotkeysPresentation" />: counts the files left out when there are more than three,
	/// on a line of its own after the three names.
	/// </summary>
	[Test]
	public void GetUnreadableHotkeysPresentation_Counts_The_Rest()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5)];

		// Act
		string result = files.GetUnreadableHotkeysPresentation("header");

		// Assert
		result
			.Should()
			.Contain(QuoteBuilder.Build(files[2].Name));

		result
			.Should()
			.NotContain(QuoteBuilder.Build(files[3].Name));

		string[] lines = result.Split(Environment.NewLine);

		lines
			.Should()
			.HaveCount(5);

		lines[^1]
			.Should()
			.Contain("2")
			.And
			.NotStartWith(@"""");
	}

	/// <summary>
	/// <see cref="HotkeyExtensions.GetUnreadableHotkeysPresentation" />: names every file under the header when there are no
	/// more than three, and counts no rest.
	/// </summary>
	[Test]
	public void GetUnreadableHotkeysPresentation_Names_Every_File()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(3)];

		// Act
		string result = files.GetUnreadableHotkeysPresentation("header");

		// Assert
		result
			.Should()
			.StartWith($"header:{Environment.NewLine}");

		result.Split(Environment.NewLine)
			.Should()
			.HaveCount(4);

		result
			.Should()
			.ContainAll(files.Select(static x => $"{Environment.NewLine}{QuoteBuilder.Build(x.Name)}"));
	}

	/// <summary>
	/// <see cref="HotkeyExtensions.ToHotkeyDtos" />: maps keyStrokes to hotkey models with sequential indexes and shared ids.
	/// </summary>
	[Test]
	public void ToHotkeyDtos_Maps_Pairs_With_Sequential_Indexes()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		Guid ownerId = Guid.NewGuid();

		KeyStroke[] keyStrokes =
		[
			new() { Code = KeyCode.VcA, Mask = EventMask.LeftCtrl },
			new() { Code = KeyCode.VcB, Mask = EventMask.LeftShift }
		];

		// Act
		HotkeyDto[] result = [.. keyStrokes.ToHotkeyDtos(id, ownerId)];

		// Assert
		result
			.Should()
			.HaveCount(2);

		result[0].Index
			.Should()
			.Be(0);

		result[1].Index
			.Should()
			.Be(1);

		result
			.Should()
			.OnlyContain(x => x.Id == id && x.OwnerId == ownerId);

		result[0].Code
			.Should()
			.Be(KeyCode.VcA);

		result[1].Mask
			.Should()
			.Be(EventMask.LeftShift);
	}

	/// <summary>
	/// <see cref="HotkeyExtensions.ToKeyStrokes" />: maps each hotkey to a key stroke preserving code and mask.
	/// </summary>
	[Test]
	public void ToKeyStrokes_Maps_HotkeyDto_To_KeyStroke()
	{
		// Arrange
		HotkeyDto[] hotkeys = [.. HotkeyFactory.CreateHotkeyDtos(3)];

		// Act
		KeyStroke[] result = [.. hotkeys.ToKeyStrokes()];

		// Assert
		result.Select(static x => (x.Code, x.Mask))
			.Should()
			.Equal(hotkeys.Select(static x => (x.Code, x.Mask)));
	}
	#endregion
}
