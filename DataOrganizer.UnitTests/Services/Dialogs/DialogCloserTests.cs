using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Helpers.Views;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Services;
using DataOrganizer.Services.Dialogs;
using DataOrganizer.Services.Views;
using DataOrganizer.UnitTests.Fakes;
using DialogHostAvalonia;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shared.Extensions;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(DialogCloser)}"" type")]
internal class DialogCloserTests
{
	#region Methods
	/// <summary>
	/// <see cref="DialogCloser.CloseAsync" />: the dialog closes in the named host, while another window has a host of its
	/// own.
	/// </summary>
	[AvaloniaTest]
	public void CloseAsync_Closes_The_Dialog_In_Its_Host()
	{
		// Arrange
		Window mainWindow = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Main
			}
		};

		Window notepadWindow = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Notepad
			}
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([mainWindow, notepadWindow]);

			app.ApplicationLifetime = lifetime;

			builder
				.RegisterInstance(app)
				.As<Application>();

			builder
				.RegisterType<DesktopAccessor>()
				.AsImplementedInterfaces();
		});

		DialogCloser sut = mock.Create<DialogCloser>();

		mainWindow.Show();

		notepadWindow.Show();

		Task dialogClosed = DialogHost.Show(new TextBlock(), DialogHostIdentifiers.Notepad);

		Dispatcher.UIThread.RunJobs();

		// Act
		_ = sut.CloseAsync(DialogHostIdentifiers.Notepad);

		bool isClosed = dialogClosed.IsCompleted;

		// Closed before the assertion: a closed window takes its host out of the list every headless test shares.
		mainWindow.Close();

		notepadWindow.Close();

		// Assert
		isClosed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DialogCloser.CloseAsync" />: a host that shows no dialog is left as it is, and the closing ends at once.
	/// </summary>
	[AvaloniaTest]
	public void CloseAsync_Leaves_A_Host_That_Shows_No_Dialog()
	{
		// Arrange
		Window window = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Main
			}
		};

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

		DialogCloser sut = mock.Create<DialogCloser>();

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		Task closing = sut.CloseAsync(DialogHostIdentifiers.Main);

		bool isCompleted = closing.IsCompleted;

		// Closed before the assertion: a closed window takes its host out of the list every headless test shares.
		window.Close();

		// Assert
		isCompleted
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DialogCloser.CloseAsync" />: closing ends once the closed dialog has faded out, not at once.
	/// </summary>
	[AvaloniaTest]
	public async Task CloseAsync_Waits_Until_The_Dialog_Is_Hidden()
	{
		// Arrange
		Window window = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Main
			}
		};

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

		DialogCloser sut = mock.Create<DialogCloser>();

		window.Show();

		_ = DialogHost.Show(new TextBlock(), DialogHostIdentifiers.Main);

		Dispatcher.UIThread.RunJobs();

		// Act
		Task closing = sut.CloseAsync(DialogHostIdentifiers.Main);

		bool isClosedAtOnce = closing.IsCompleted;

		await RunJobsUntilAsync(() => closing.IsCompleted);

		// Closed before the assertion: a closed window takes its host out of the list every headless test shares.
		window.Close();

		// Assert
		isClosedAtOnce
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DialogCloser.FindShownDialog" />: the wait of a dialog whose fade never ends runs out by itself after
	/// the fallback time, not earlier.
	/// </summary>
	[AvaloniaTest]
	public void FindShownDialog_Ends_The_Wait_When_The_Fade_Stalls()
	{
		// Arrange
		FakeTimeProvider time = new();

		Window window = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Main
			}
		};

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

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<UiTimer>()
				.As<IUiTimer>();
		});

		DialogCloser sut = mock.Create<DialogCloser>();

		window.Show();

		_ = DialogHost.Show(new TextBlock(), DialogHostIdentifiers.Main);

		Dispatcher.UIThread.RunJobs();

		IShownDialog shown = sut.FindShownDialog(DialogHostIdentifiers.Main)!;

		// Act
		// The dialog stays open, as a fade that never ends would leave it.
		Task hiding = shown.WaitUntilHiddenAsync();

		time.Advance(TimeSpan.FromSeconds(2.9));

		Dispatcher.UIThread.RunJobs();

		bool isHiddenEarly = hiding.IsCompleted;

		time.Advance(TimeSpan.FromSeconds(0.1));

		Dispatcher.UIThread.RunJobs();

		// Taken before the close, which would end the wait by itself.
		bool isHidden = hiding.IsCompleted;

		DialogHost.Close(DialogHostIdentifiers.Main);

		// Closed before the assertion: a closed window takes its host out of the list every headless test shares.
		window.Close();

		// Assert
		isHiddenEarly
			.Should()
			.BeFalse();

		isHidden
			.Should()
			.BeTrue();

	}

	/// <summary>
	/// <see cref="DialogCloser.FindShownDialog" />: a host that shows no dialog has none to find.


	/// </summary>
	[AvaloniaTest]
	public void FindShownDialog_Finds_None_While_The_Host_Shows_None()
	{
		// Arrange
		Window window = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Main
			}
		};

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

		DialogCloser sut = mock.Create<DialogCloser>();

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		IShownDialog? shown = sut.FindShownDialog(DialogHostIdentifiers.Main);

		window.Close();

		// Assert
		shown
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DialogCloser.FindShownDialog" />: a name that no host of the open windows has finds no dialog.
	/// </summary>
	[AvaloniaTest]
	public void FindShownDialog_Finds_None_Without_A_Host_Of_That_Name()
	{
		// Arrange
		Window window = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Main
			}
		};

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

		DialogCloser sut = mock.Create<DialogCloser>();

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		IShownDialog? shown = sut.FindShownDialog(nameof(FindShownDialog_Finds_None_Without_A_Host_Of_That_Name));

		window.Close();

		// Assert
		shown
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DialogCloser.FindShownDialog" />: the dialog the host shows is found, and its wait ends once the dialog
	/// has closed and faded out.
	/// </summary>
	[AvaloniaTest]
	public async Task FindShownDialog_Waits_Until_The_Closed_Dialog_Is_Hidden()
	{
		// Arrange
		Window window = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Main
			}
		};

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

		DialogCloser sut = mock.Create<DialogCloser>();

		window.Show();

		_ = DialogHost.Show(new TextBlock(), DialogHostIdentifiers.Main);

		Dispatcher.UIThread.RunJobs();

		IShownDialog shown = sut.FindShownDialog(DialogHostIdentifiers.Main)!;

		// Act
		DialogHost.Close(DialogHostIdentifiers.Main);

		Task hiding = shown.WaitUntilHiddenAsync();

		await RunJobsUntilAsync(() => hiding.IsCompleted);

		// Closed before the assertion: a closed window takes its host out of the list every headless test shares.
		window.Close();

		// Assert
		hiding.IsCompleted
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Runs the jobs of the UI thread until <paramref name="condition" /> holds or the wait runs out, before the fallback
	/// timeout of a stalled fade would end it.
	/// </summary>
	private static async Task RunJobsUntilAsync(Func<bool> condition)
	{
		Func<bool> isMet = () =>
		{
			Dispatcher.UIThread.RunJobs();

			return condition();
		};

		await isMet.WaitAsync(millisecondsDelay: 20, maxRepeats: 25);
	}
	#endregion
}
