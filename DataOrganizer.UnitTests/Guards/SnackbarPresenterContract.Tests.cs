using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Enums;
using DataOrganizer.Services;
using Material.Styles.Controls;
using System.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that the presenter still finds its way around Material's snackbar host")]
internal class SnackbarPresenterContractTests
{
	#region Methods
	/// <summary>
	/// <see cref="SnackbarPresenter.IsPointerOverMessage" />: the pointer resting on the shown message is seen.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Over_The_Shown_Message_Is_Seen()
	{
		// Arrange
		Window window = CreateWindow(out SnackbarPresenter sut);

		sut.Post(new("Snackbar text", SnackbarMessageLevel.Information));

		Card card = ShowMessage(window);

		// Act
		window.MouseMove(Center(window, card));

		// Assert
		sut
			.IsPointerOverMessage
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SnackbarPresenter.IsPointerOverMessage" />: the window content the host wraps is not the message.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Somewhere_Else_Is_Not_Taken_For_The_Message()
	{
		// Arrange
		Window window = CreateWindow(out SnackbarPresenter sut);

		sut.Post(new("Snackbar text", SnackbarMessageLevel.Information));

		ShowMessage(window);

		// Act
		window.MouseMove(new(1.0, 1.0));

		// Assert
		sut
			.IsPointerOverMessage
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SnackbarPresenter.Post" />: the message reaches the attached host and stays until it is removed.
	/// </summary>
	[AvaloniaTest]
	public void Posted_Message_Stays_Until_It_Is_Removed()
	{
		// Arrange
		Window window = CreateWindow(out SnackbarPresenter sut);

		SnackbarHost host = (SnackbarHost)window.Content!;

		// Act
		sut.Post(new("Snackbar text", SnackbarMessageLevel.Information));

		ShowMessage(window);

		// Assert
		host
			.SnackbarModels
			.Should()
			.HaveCount(1);

		sut.Remove();

		Dispatcher.UIThread.RunJobs();

		host
			.SnackbarModels
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="SnackbarPresenter.AttachHost" />: a host is known while it is on the screen and forgotten afterwards.
	/// </summary>
	[AvaloniaTest]
	public void Shown_Host_Is_The_One_Messages_Go_To()
	{
		// Arrange
		Window window = CreateWindow(out SnackbarPresenter sut);

		// Assert
		sut
			.CanShow
			.Should()
			.BeTrue();

		// Act
		sut.DetachHost((SnackbarHost)window.Content!);

		// Assert
		sut
			.CanShow
			.Should()
			.BeFalse();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the point in the middle of the control.
	/// </summary>
	private static Point Center(Visual root, Visual target)
	{
		return target.TranslatePoint(
			new(
				target.Bounds.Width / 2.0,
				target.Bounds.Height / 2.0),
			root) ?? default;
	}

	/// <summary>
	/// Shows a window with a host the presenter has been given.
	/// </summary>
	private static Window CreateWindow(out SnackbarPresenter presenter)
	{
		SnackbarHost host = new()
		{
			HostName = nameof(SnackbarPresenterContractTests)
		};

		Window window = new()
		{
			Content = host,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		presenter = new();

		presenter.AttachHost(host);

		return window;
	}

	/// <summary>
	/// Lets the posted message reach the screen and returns the card it occupies.
	/// </summary>
	private static Card ShowMessage(Window window)
	{
		Dispatcher.UIThread.RunJobs();

		window.UpdateLayout();

		return window
			.GetVisualDescendants()
			.OfType<Card>()
			.First();
	}
	#endregion
}
