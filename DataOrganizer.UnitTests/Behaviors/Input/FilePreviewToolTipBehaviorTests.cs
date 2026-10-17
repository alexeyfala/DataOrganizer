using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Input;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.UnitTests.Factories;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Behaviors.Input;

[TestFixture(Description = $@"Tests of ""{nameof(FilePreviewToolTipBehavior)}"" type")]
internal class FilePreviewToolTipBehaviorTests
{
	#region Data
	/// <summary>
	/// Hover delay of the behavior under test, in milliseconds.
	/// </summary>
	private const int Delay = 400;

	/// <summary>
	/// A point over the hover target.
	/// </summary>
	private static readonly Point Inside = new(25.0, 25.0);

	/// <summary>
	/// A point in the window away from the hover target.
	/// </summary>
	private static readonly Point Outside = new(150.0, 150.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="FilePreviewToolTipBehavior" />: a hover over a file that has no text to show opens no tip.
	/// </summary>
	[AvaloniaTest]
	public void Hover_Opens_No_Tip_Without_A_Preview()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		FakeTimeProvider time = new();

		IFilePreviewReader reader = Substitute.For<IFilePreviewReader>();

		reader
			.ReadPreviewAsync(file)
			.Returns((string?)null);

		Border target = new()
		{
			Background = Brushes.Transparent,
			DataContext = file,
			Height = 50.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = 50.0
		};

		Window window = new()
		{
			Content = target,
			Height = 200.0,
			Width = 200.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		FilePreviewToolTipBehavior sut = new()
		{
			Delay = Delay,
			Reader = reader,
			RunOnce = (action, delay) => time.CreateTimer(_ => action(), null, delay, Timeout.InfiniteTimeSpan)
		};

		sut.Attach(target);

		window.MouseMove(Inside);

		// Act
		time.Advance(TimeSpan.FromMilliseconds(Delay));

		object? tip = ToolTip.GetTip(target);

		window.Close();

		// Assert
		tip
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FilePreviewToolTipBehavior" />: a hover while the preview is still read starts no second reading.
	/// </summary>
	[AvaloniaTest]
	public void Hover_Reads_Once_While_The_Preview_Is_Read()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		FakeTimeProvider time = new();

		IFilePreviewReader reader = Substitute.For<IFilePreviewReader>();

		// The reading never ends, as a slow one has not ended yet.
		reader
			.ReadPreviewAsync(file)
			.Returns(new TaskCompletionSource<string?>().Task);

		Border target = new()
		{
			Background = Brushes.Transparent,
			DataContext = file,
			Height = 50.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = 50.0
		};

		Window window = new()
		{
			Content = target,
			Height = 200.0,
			Width = 200.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		FilePreviewToolTipBehavior sut = new()
		{
			Delay = Delay,
			Reader = reader,
			RunOnce = (action, delay) => time.CreateTimer(_ => action(), null, delay, Timeout.InfiniteTimeSpan)
		};

		sut.Attach(target);

		window.MouseMove(Inside);

		time.Advance(TimeSpan.FromMilliseconds(Delay));

		window.MouseMove(Outside);

		window.MouseMove(Inside);

		// Act
		time.Advance(TimeSpan.FromMilliseconds(Delay));

		window.Close();

		// Assert
		reader
			.Received(1)
			.ReadPreviewAsync(file);
	}

	/// <summary>
	/// <see cref="FilePreviewToolTipBehavior" />: a hover over a file shows the preview of its text in an open tip.
	/// </summary>
	[AvaloniaTest]
	public void Hover_Shows_The_Preview_In_An_Open_Tip()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		FakeTimeProvider time = new();

		IFilePreviewReader reader = Substitute.For<IFilePreviewReader>();

		reader
			.ReadPreviewAsync(file)
			.Returns("Hi");

		Border target = new()
		{
			Background = Brushes.Transparent,
			DataContext = file,
			Height = 50.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = 50.0
		};

		Window window = new()
		{
			Content = target,
			Height = 200.0,
			Width = 200.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		FilePreviewToolTipBehavior sut = new()
		{
			Delay = Delay,
			Reader = reader,
			RunOnce = (action, delay) => time.CreateTimer(_ => action(), null, delay, Timeout.InfiniteTimeSpan)
		};

		sut.Attach(target);

		window.MouseMove(Inside);

		// Act
		time.Advance(TimeSpan.FromMilliseconds(Delay));

		object? tip = ToolTip.GetTip(target);

		bool isOpen = ToolTip.GetIsOpen(target);

		ToolTip.SetIsOpen(target, false);

		window.Close();

		// Assert
		tip
			.Should()
			.Be("Hi");

		isOpen
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FilePreviewToolTipBehavior" />: the pointer leaving the target takes its tip away.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Leaving_Takes_The_Tip_Away()
	{
		// Arrange
		Border target = new()
		{
			Background = Brushes.Transparent,
			Height = 50.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = 50.0
		};

		ToolTip.SetTip(target, "Hi");

		Window window = new()
		{
			Content = target,
			Height = 200.0,
			Width = 200.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		FilePreviewToolTipBehavior sut = new();

		sut.Attach(target);

		window.MouseMove(Inside);

		// Act
		window.MouseMove(Outside);

		object? tip = ToolTip.GetTip(target);

		window.Close();

		// Assert
		tip
			.Should()
			.BeNull();
	}
	#endregion
}
