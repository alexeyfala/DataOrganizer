using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Services.Views;
using NSubstitute;

namespace DataOrganizer.UnitTests.Services.Views;

[TestFixture(Description = $@"Tests of ""{nameof(DesktopAccessor)}"" type")]
internal class DesktopAccessorTests
{
	#region Methods
	/// <summary>
	/// <see cref="DesktopAccessor.Shutdown" />: with no desktop lifetime to shut down, the process is ended.
	/// </summary>
	[Test]
	public void Shutdown_Ends_The_Process_Without_A_Desktop_Lifetime()
	{
		// Arrange
		IProcessTerminator processTerminator = Substitute.For<IProcessTerminator>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(processTerminator));

		DesktopAccessor sut = mock.Create<DesktopAccessor>();

		// Act
		sut.Shutdown();

		// Assert
		processTerminator
			.Received(1)
			.Terminate(Arg.Any<int>());
	}

	/// <summary>
	/// <see cref="DesktopAccessor.Shutdown" />: the desktop application shuts down, and the process is left to it.
	/// </summary>
	[Test]
	public void Shutdown_Shuts_The_Desktop_Down()
	{
		// Arrange
		IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

		IProcessTerminator processTerminator = Substitute.For<IProcessTerminator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			Application app = Substitute.For<Application>();

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();

			builder.RegisterInstance(processTerminator);
		});

		DesktopAccessor sut = mock.Create<DesktopAccessor>();

		// Act
		sut.Shutdown();

		// Assert
		lifetime
			.Received(1)
			.Shutdown(Arg.Any<int>());

		processTerminator
			.DidNotReceiveWithAnyArgs()
			.Terminate();
	}
	#endregion
}
