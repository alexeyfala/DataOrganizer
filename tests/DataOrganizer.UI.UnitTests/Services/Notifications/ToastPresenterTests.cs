using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.UI.Services.Notifications;
using DataOrganizer.UI.Services.Views;
using DataOrganizer.UI.Windows;
using NSubstitute;

namespace DataOrganizer.UI.UnitTests.Services.Notifications;

/// <summary>
/// Tests of <see cref="ToastPresenter" /> type.
/// </summary>
[TestFixture]
internal sealed class ToastPresenterTests
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

			builder
				.RegisterInstance(app)
				.As<Application>();

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
