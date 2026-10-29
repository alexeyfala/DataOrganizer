using Autofac;
using Autofac.Extras.Moq;
using DialogContracts.Dto;
using DialogContracts.Interfaces;
using GlobalHotkeys.Interfaces;
using GlobalHotkeys.Services;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using Repository.Dto;
using SharpHook.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GlobalHotkeys.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(HotkeysEditingScenario)}"" type")]
internal class HotkeysEditingScenarioTests
{
	#region Methods
	/// <summary>
	/// <see cref="HotkeysEditingScenario.EditAsync" />: a cancelled editor saves nothing, and the tracking of the hotkeys
	/// goes on.
	/// </summary>
	[Test]
	public async Task EditAsync_Saves_Nothing_When_The_Editor_Is_Cancelled()
	{
		// Arrange
		IFileHotkeyEditor fileHotkeyEditor = Substitute.For<IFileHotkeyEditor>();

		IHotkeyTrackingControl hotkeyTrackingControl = Substitute.For<IHotkeyTrackingControl>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHotkeysPrompt hotkeysPrompt = Substitute.For<IHotkeysPrompt>();

			hotkeysPrompt
				.EditHotkeysAsync(Arg.Any<IEnumerable<KeyStroke>>())
				.Returns(new EditingHotkeysResult(false, []));

			builder.RegisterInstance(fileHotkeyEditor);

			builder.RegisterInstance(hotkeysPrompt);

			builder.RegisterInstance(hotkeyTrackingControl);
		});

		HotkeysEditingScenario sut = mock.Create<HotkeysEditingScenario>();

		// Act
		await sut.EditAsync(ItemDtoFactory.CreateFileDto(), []);

		// Assert
		await fileHotkeyEditor
			.DidNotReceiveWithAnyArgs()
			.OverwriteAsync(default!, default!, default!);

		hotkeyTrackingControl
			.Received(1)
			.ResumeHotkeyTracking();
	}

	/// <summary>
	/// <see cref="HotkeysEditingScenario.EditAsync" />: the tracking of the hotkeys pauses while the editor is open, the
	/// hotkeys it returns are saved, and the tracking goes on.
	/// </summary>
	[Test]
	public async Task EditAsync_Saves_The_Hotkeys_The_Editor_Returns()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		List<ExplorerItemDtoBase> hierarchy = [file];

		KeyStroke[] newHotkeys =
		[
			new()
			{
				Code = KeyCode.VcA,
				Mask = EventMask.LeftCtrl
			}
		];

		IFileHotkeyEditor fileHotkeyEditor = Substitute.For<IFileHotkeyEditor>();

		IHotkeyTrackingControl hotkeyTrackingControl = Substitute.For<IHotkeyTrackingControl>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHotkeysPrompt hotkeysPrompt = Substitute.For<IHotkeysPrompt>();

			hotkeysPrompt
				.EditHotkeysAsync(Arg.Any<IEnumerable<KeyStroke>>())
				.Returns(new EditingHotkeysResult(true, newHotkeys));

			builder.RegisterInstance(fileHotkeyEditor);

			builder.RegisterInstance(hotkeysPrompt);

			builder.RegisterInstance(hotkeyTrackingControl);
		});

		HotkeysEditingScenario sut = mock.Create<HotkeysEditingScenario>();

		// Act
		await sut.EditAsync(file, hierarchy);

		// Assert
		await hotkeyTrackingControl
			.Received(1)
			.PauseHotkeyTrackingAsync();

		await fileHotkeyEditor
			.Received(1)
			.OverwriteAsync(file, newHotkeys, hierarchy);

		hotkeyTrackingControl
			.Received(1)
			.ResumeHotkeyTracking();
	}
	#endregion
}
