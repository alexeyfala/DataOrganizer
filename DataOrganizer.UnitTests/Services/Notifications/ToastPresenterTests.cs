using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Services.Notifications;
using DataOrganizer.Services.Views;
using DataOrganizer.Windows;
using NSubstitute;

namespace DataOrganizer.UnitTests.Services.Notifications;

[TestFixture(Description = $@"Tests of ""{nameof(ToastPresenter)}"" type")]
internal class ToastPresenterTests
{
	#region Methods
	/// <summary>
	/// <see cref="ToastPresenter.Remove" />: a toast window left over from an earlier message is closed as well.
	/// </summary>
	[AvaloniaTest]
	public void Remove_Closes_A_Toast_Window_Left_Over()
	{
		// Arrange
		bool isClosed = false;

		ToastWindow window = new();

		window.Closed += (_, _) => isClosed = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([window]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();

			builder
				.RegisterType<DesktopAccessor>()
				.AsImplementedInterfaces();
		});

		ToastPresenter sut = mock.Create<ToastPresenter>();

		window.Show();

		// Act
		sut.Remove();

		// Assert
		isClosed
			.Should()
			.BeTrue();
	}
	#endregion
}
