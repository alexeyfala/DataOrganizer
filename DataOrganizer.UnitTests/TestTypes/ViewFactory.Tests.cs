using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Services;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ViewFactory)}"" type")]
internal class ViewFactoryTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ViewFactory.CreateUserControl{T}" />.
	/// </summary>
	[AvaloniaTest]
	public void CreateUserControl_Resolves_UserControl_From_Service_Provider()
	{
		// Arrange
		UserControl expected = new();

		IServiceProvider provider = Substitute.For<IServiceProvider>();

		provider
			.GetService(typeof(UserControl))
			.Returns(expected);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(provider));

		ViewFactory sut = mock.Create<ViewFactory>();

		// Act
		UserControl result = sut.CreateUserControl<UserControl>();

		// Assert
		result
			.Should()
			.BeSameAs(expected);

		provider
			.Received()
			.GetService(typeof(UserControl));
	}

	/// <summary>
	/// Test of <see cref="ViewFactory.CreateWindow{T}" />.
	/// </summary>
	[AvaloniaTest]
	public void CreateWindow_Resolves_Window_From_Service_Provider()
	{
		// Arrange
		Window expected = new();

		IServiceProvider provider = Substitute.For<IServiceProvider>();

		provider
			.GetService(typeof(Window))
			.Returns(expected);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(provider));

		ViewFactory sut = mock.Create<ViewFactory>();

		// Act
		Window result = sut.CreateWindow<Window>();

		// Assert
		result
			.Should()
			.BeSameAs(expected);

		provider
			.Received()
			.GetService(typeof(Window));
	}
	#endregion
}
