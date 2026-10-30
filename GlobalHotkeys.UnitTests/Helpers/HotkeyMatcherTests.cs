using AwesomeAssertions;
using GlobalHotkeys.Extensions;
using GlobalHotkeys.Helpers;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using Repository.Dto;
using Shared.Extensions;
using SharpHook.Data;

namespace GlobalHotkeys.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(HotkeyMatcher)}"" type")]
internal class HotkeyMatcherTests
{
	#region Methods
	/// <summary>
	/// <see cref="HotkeyMatcher.FindFile" />: the file whose hotkey the typed keys end with is found, in a folder too.
	/// </summary>
	[Test]
	public void FindFile_Finds_The_File_Whose_Hotkey_Ends_The_Typed_Keys()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		file
			.Hotkeys
			.AddRange(new KeyStroke[]
			{
				new()
				{
					Code = KeyCode.VcA,
					Mask = EventMask.LeftCtrl
				},
				new()
				{
					Code = KeyCode.VcB,
					Mask = EventMask.LeftCtrl
				}
			}.ToHotkeyDtos());

		folder.Children.Add(file);

		KeyStroke[] typed =
		[
			new()
			{
				Code = KeyCode.VcX,
				Mask = EventMask.LeftCtrl
			},
			new()
			{
				Code = KeyCode.VcA,
				Mask = EventMask.LeftCtrl
			},
			new()
			{
				Code = KeyCode.VcB,
				Mask = EventMask.LeftCtrl
			}
		];

		// Act
		FileDto? result = HotkeyMatcher.FindFile([folder], typed);

		// Assert
		result
			.Should()
			.BeSameAs(file);
	}

	/// <summary>
	/// <see cref="HotkeyMatcher.FindFile" />: a stored hotkey matches whatever lock keys were on when it was recorded.
	/// </summary>
	[Test]
	public void FindFile_Ignores_The_Lock_Keys_Of_A_Stored_Hotkey()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		file
			.Hotkeys
			.AddRange(new KeyStroke[]
			{
				new()
				{
					Code = KeyCode.VcA,
					Mask = EventMask.LeftCtrl | EventMask.CapsLock
				}
			}.ToHotkeyDtos());

		KeyStroke[] typed =
		[
			new()
			{
				Code = KeyCode.VcA,
				Mask = EventMask.LeftCtrl
			}
		];

		// Act
		FileDto? result = HotkeyMatcher.FindFile([file], typed);

		// Assert
		result
			.Should()
			.BeSameAs(file);
	}

	/// <summary>
	/// <see cref="HotkeyMatcher.FindFile" />: a file without hotkeys matches no typed keys.
	/// </summary>
	[Test]
	public void FindFile_Passes_Over_A_File_Without_Hotkeys()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		KeyStroke[] typed =
		[
			new()
			{
				Code = KeyCode.VcA,
				Mask = EventMask.LeftCtrl
			}
		];

		// Act
		FileDto? result = HotkeyMatcher.FindFile([file], typed);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="HotkeyMatcher.FindFile" />: keys that end no hotkey find no file.
	/// </summary>
	[Test]
	public void FindFile_Without_A_Matching_Hotkey_Is_Null()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		file
			.Hotkeys
			.AddRange(new KeyStroke[]
			{
				new()
				{
					Code = KeyCode.VcA,
					Mask = EventMask.LeftCtrl
				}
			}.ToHotkeyDtos());

		KeyStroke[] typed =
		[
			new()
			{
				Code = KeyCode.VcB,
				Mask = EventMask.LeftCtrl
			}
		];

		// Act
		FileDto? result = HotkeyMatcher.FindFile([file], typed);

		// Assert
		result
			.Should()
			.BeNull();
	}
	#endregion
}
