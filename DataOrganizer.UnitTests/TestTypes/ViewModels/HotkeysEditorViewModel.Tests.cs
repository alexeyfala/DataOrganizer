using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Input;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Extensions;
using DataOrganizer.ViewModels;
using Repository.DTO;
using Shared.Extensions;
using Shared.Properties;
using SharpHook;
using SharpHook.Data;
using SharpHook.Testing;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(HotkeysEditorViewModel)}"" type")]
internal class HotkeysEditorViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="HotkeysEditorViewModel.Clear" />: empties the buffer.
	/// </summary>
	[Test]
	public void Clear_Clears_The_Buffer()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		sut
			.Buffer
			.AddRange(TestUtils.CreateCodeMaskPairs(5));

		// Act
		sut.Clear();

		// Assert
		sut.Buffer
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: clears the buffer and disposes the hook.
	/// </summary>
	[Test]
	public void Dispose_Disposes_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>(
			TypedParameter.From<IGlobalHook>(hook));

		sut
			.Buffer
			.AddRange(TestUtils.CreateCodeMaskPairs(5));

		// Act
		sut.Dispose();

		// Assert
		sut.Buffer
			.Should()
			.BeEmpty();

		hook.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.HandleKeyReleased" />: adds the code-mask pair to the buffer when a modifier mask is active.
	/// </summary>
	[Test]
	public void HandleKeyReleased_Adds_Pair_To_Buffer_When_Mask_Is_Active()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		// Act
		sut.HandleKeyReleased(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		sut.Buffer
			.Should()
			.HaveCount(1);

		sut.Buffer[0].Code
			.Should()
			.Be(KeyCode.VcA);

		sut.Buffer[0].Mask
			.Should()
			.Be(EventMask.LeftCtrl);
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.HandleKeyReleased" />: never adds more pairs than the maximum hotkey count.
	/// </summary>
	[Test]
	public void HandleKeyReleased_Adds_Values_No_More_Than_Maximum_Value()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		CodeMaskPair[] pairs = [.. Enumerable.Repeat(new CodeMaskPair()
		{
			Code = KeyCode.VcA,
			Mask = EventMask.LeftCtrl
		}, 100)];

		// Act
		pairs.ForEach(x => sut.HandleKeyReleased(x.Mask, x.Code));

		// Assert
		sut.Buffer.Count
			.Should()
			.Be(HotkeysEditorViewModel.MaxHotkeys);
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.HandleKeyReleased" />: does nothing when the mask is empty.
	/// </summary>
	[Test]
	public void HandleKeyReleased_Does_Nothing_When_Mask_Is_Empty()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		// Act
		sut.HandleKeyReleased(EventMask.None, KeyCode.VcA);

		// Assert
		sut.Buffer
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.HandleKeyReleased" />: ignores standalone modifier keys.
	/// </summary>
	[Test]
	public void HandleKeyReleased_Ignores_Modifier_Keys()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		// Act
		sut.HandleKeyReleased(EventMask.LeftCtrl, KeyCode.VcLeftShift);

		// Assert
		sut.Buffer
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.HandleKeyReleased" />: strips the NumLock flag from the stored mask.
	/// </summary>
	[Test]
	public void HandleKeyReleased_Strips_NumLock_From_Mask()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		// Act
		sut.HandleKeyReleased(EventMask.LeftCtrl | EventMask.NumLock, KeyCode.VcA);

		// Assert
		sut.Buffer
			.Should()
			.HaveCount(1);

		sut.Buffer[0].Mask
			.Should()
			.Be(EventMask.LeftCtrl);
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.KeyUp" />: does nothing when the event args are null.
	/// </summary>
	[Test]
	public void KeyUp_Does_Nothing_When_Args_Is_Null()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		// Act
		sut.KeyUp(null);

		// Assert
		sut.IsSaved
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.KeyUp" />: does nothing when the pressed key is not Enter.
	/// </summary>
	[Test]
	public void KeyUp_Does_Nothing_When_Key_Is_Not_Enter()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		// Act
		sut.KeyUp(new()
		{
			Key = Key.A,
			KeyModifiers = KeyModifiers.None
		});

		// Assert
		sut.IsSaved
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.KeyUp" />: does nothing when a modifier is set alongside Enter.
	/// </summary>
	[Test]
	public void KeyUp_Does_Nothing_When_Modifier_Is_Set()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		// Act
		sut.KeyUp(new()
		{
			Key = Key.Enter,
			KeyModifiers = KeyModifiers.Control
		});

		// Assert
		sut.IsSaved
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.KeyUp" />: saves the hotkeys when Enter is pressed without modifiers.
	/// </summary>
	[Test]
	public void KeyUp_Saves_Hotkeys_By_Enter_Key_Pressed()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		// Act
		sut.KeyUp(new()
		{
			Key = Key.Enter,
			KeyModifiers = KeyModifiers.None
		});

		// Assert
		sut.IsSaved
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.MakePreview" />: shows the hotkeys presentation when the buffer is non-empty, otherwise the assigning placeholder.
	/// </summary>
	[Test]
	public void MakePreview_Creates_Preview_For_Hotkeys([Values] bool isAnyInBuffer)
	{
		// Arrange
		CodeMaskPair[] pairs = [.. TestUtils.CreateCodeMaskPairs(5)];

		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		if (isAnyInBuffer)
		{
			sut
				.Buffer
				.AddRange(pairs);
		}

		// Act
		sut.MakePreview();

		// Assert
		sut.Preview
			.Should()
			.Be(isAnyInBuffer ? pairs.GetHotkeysPresentation() : Strings.AssigningHotkeys);
	}

	/// <summary>
	/// <see cref="HotkeysEditorViewModel.SaveAndClose" />: sets the saved flag.
	/// </summary>
	[Test]
	public void SaveAndClose_Sets_Property()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		HotkeysEditorViewModel sut = mock.Create<HotkeysEditorViewModel>();

		// Act
		sut.SaveAndClose();

		// Assert
		sut.IsSaved
			.Should()
			.BeTrue();
	}
	#endregion
}
