using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Input;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Abstract;
using DataOrganizer.Extensions;
using DataOrganizer.ViewModels;
using Repository.DTO;
using Shared.Extensions;
using Shared.Properties;
using SharpHook;
using SharpHook.Data;
using SharpHook.Testing;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(HotkeysEditorViewModel)}"" type")]
internal class HotkeysEditorViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="HotkeysEditorViewModel.Clear" />.
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
	/// Test of <see cref="ObservableDisposable.Dispose" />.
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
	/// Test of <see cref="HotkeysEditorViewModel.HandleKeyReleased" />.
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
	/// Test of <see cref="HotkeysEditorViewModel.KeyUp" />.
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
	/// Test of <see cref="HotkeysEditorViewModel.MakePreview" />.
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
	/// Test of <see cref="HotkeysEditorViewModel.SaveAndClose" />.
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
