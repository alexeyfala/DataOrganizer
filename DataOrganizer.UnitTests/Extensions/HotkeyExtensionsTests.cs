using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.UnitTests.Factories;
using Repository.Dto;
using SharpHook.Data;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Extensions;

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

		FileDto damaged = CreateFileWithHotkey(KeyCode.VcUndefined, EventMask.LeftCtrl);

		root.Children.Add(damaged);

		root.Children.Add(CreateFileWithHotkey(KeyCode.VcA, EventMask.LeftCtrl));

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
		FileDto damaged = CreateFileWithHotkey(KeyCode.VcA, EventMask.None);

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
			CreateFileWithHotkey(KeyCode.VcA, EventMask.LeftCtrl),
			CreateFileWithHotkey(KeyCode.VcB, EventMask.LeftShift | EventMask.LeftAlt)
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
	/// <see cref="HotkeyExtensions.GetUnreadableHotkeysPresentation" />: counts the files left out when there are more than three.
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
			.Contain($@"""{files[2].Name}""");

		result
			.Should()
			.NotContain($@"""{files[3].Name}""");

		result
			.Split(Environment.NewLine)
			.Last()
			.Should()
			.Contain("2");
	}

	/// <summary>
	/// <see cref="HotkeyExtensions.GetUnreadableHotkeysPresentation" />: names every file when there are no more than three.
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
			.StartWith("header");

		foreach (FileDto file in files)
		{
			result
				.Should()
				.Contain($@"{Environment.NewLine}""{file.Name}""");
		}
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
		result
			.Should()
			.HaveCount(3);

		for (int i = 0; i < hotkeys.Length; i++)
		{
			result[i].Code
				.Should()
				.Be(hotkeys[i].Code);

			result[i].Mask
				.Should()
				.Be(hotkeys[i].Mask);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a file that owns a single hotkey.
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
