using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using AwesomeAssertions;
using DataOrganizer.Abstract;
using DataOrganizer.Extensions;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ApplicationExtensions)}"" type")]
internal class ApplicationExtensionsTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ApplicationExtensions.CloseAllWindows" />.
	/// </summary>
	[Test]
	public void CloseAllWindows_Does_Nothing_When_Application_Has_No_Windows()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		// Act
		Action act = app.CloseAllWindows;

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// Test of <see cref="ApplicationExtensions.FindBaseDataContext" />.
	/// </summary>
	[Test]
	public void FindBaseDataContext_Returns_Null_When_Application_Has_No_Windows()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		// Act
		ViewModelBase? result = app.FindBaseDataContext();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="ApplicationExtensions.FindClipboard" />.
	/// </summary>
	[Test]
	public void FindClipboard_Returns_Null_When_Application_Has_No_Windows()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		// Act
		IClipboard? result = app.FindClipboard();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="ApplicationExtensions.FindDataContext{T}(Application)" />.
	/// </summary>
	[Test]
	public void FindDataContext_Returns_Default_When_Application_Has_No_Windows()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		// Act
		object? result = app.FindDataContext<object>();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="ApplicationExtensions.FindWindow{T}(Application)" />.
	/// </summary>
	[Test]
	public void FindWindow_Returns_Null_When_Application_Has_No_Windows()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		// Act
		Window? result = app.FindWindow<Window>();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="ApplicationExtensions.FindWindow{T}(Application, Predicate{T})" />.
	/// </summary>
	[Test]
	public void FindWindow_With_Predicate_Returns_Null_When_Application_Has_No_Windows()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		// Act
		Window? result = app.FindWindow<Window>(_ => true);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="ApplicationExtensions.GetAllWindows" />.
	/// </summary>
	[Test]
	public void GetAllWindows_Returns_Empty_Sequence_When_No_Lifetime()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		// Act
		IReadOnlyList<Window> result = app.GetAllWindows();

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="ApplicationExtensions.IsAnyWindow{T}(Application)" />.
	/// </summary>
	[Test]
	public void IsAnyWindow_Returns_False_When_Application_Has_No_Windows()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		// Act
		bool result = app.IsAnyWindow<Window>();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="ApplicationExtensions.IsAnyWindow{T}(Application, Predicate{T})" />.
	/// </summary>
	[Test]
	public void IsAnyWindow_With_Predicate_Returns_False_When_Application_Has_No_Windows()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		// Act
		bool result = app.IsAnyWindow<Window>(_ => true);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="ApplicationExtensions.IsDesktop" />.
	/// </summary>
	[Test]
	public void IsDesktop_Returns_False_When_Application_Has_No_Lifetime()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		// Act
		bool result = app.IsDesktop(out IClassicDesktopStyleApplicationLifetime? desktop);

		// Assert
		result
			.Should()
			.BeFalse();

		desktop
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="ApplicationExtensions.IsDesktop" />.
	/// </summary>
	[Test]
	public void IsDesktop_Returns_True_When_Application_Has_Classic_Desktop_Lifetime()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

		app.ApplicationLifetime = lifetime;

		// Act
		bool result = app.IsDesktop(out IClassicDesktopStyleApplicationLifetime? desktop);

		// Assert
		result
			.Should()
			.BeTrue();

		desktop
			.Should()
			.BeSameAs(lifetime);
	}

	/// <summary>
	/// Test of <see cref="ApplicationExtensions.IsDialogHostOpened" />.
	/// </summary>
	[Test]
	public void IsDialogHostOpened_Returns_False_When_Application_Has_No_Windows()
	{
		// Arrange
		Application app = Substitute.For<Application>();

		// Act
		bool result = app.IsDialogHostOpened();

		// Assert
		result
			.Should()
			.BeFalse();
	}
	#endregion
}
