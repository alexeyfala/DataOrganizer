using Autofac;
using Autofac.Extras.Moq;
using Clipboard.Interfaces;
using Clipboard.Services;
using Diagnostics.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System;
using System.Threading.Tasks;

namespace Clipboard.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardAutoClear)}"" type")]
internal class ClipboardAutoClearTests
{
	#region Data
	/// <summary>
	/// Mirrors the service's internal auto-clear timeout.
	/// </summary>
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ClipboardAutoClear.Arm" />: clears the clipboard once the timeout elapses and the ownership marker is still present.
	/// </summary>
	[Test]
	public async Task Arm_Clears_Clipboard_When_Still_Owned()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		FakeTimeProvider time = new();

		Task? scheduled = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			clipboard
				.ContainsOwnershipMarkerAsync()
				.Returns(true);

			exceptionHandler.Watch(Arg.Do<Task>(task => scheduled = task));

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<TimeProvider>(time);

			builder.RegisterType<ClipboardGate>().As<IClipboardGate>();
		});

		ClipboardAutoClear sut = mock.Create<ClipboardAutoClear>();

		// Act
		sut.Arm();

		time.Advance(Timeout);

		await scheduled!;

		// Assert
		await clipboard
			.Received(1)
			.ClearAsync();
	}

	/// <summary>
	/// <see cref="ClipboardAutoClear.Arm" />: leaves the clipboard alone when the ownership marker is gone (something else was copied).
	/// </summary>
	[Test]
	public async Task Arm_Does_Not_Clear_When_Ownership_Marker_Absent()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		FakeTimeProvider time = new();

		Task? scheduled = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			// Something else was copied since.
			clipboard
				.ContainsOwnershipMarkerAsync()
				.Returns(false);

			exceptionHandler.Watch(Arg.Do<Task>(task => scheduled = task));

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<TimeProvider>(time);

			builder.RegisterType<ClipboardGate>().As<IClipboardGate>();
		});

		ClipboardAutoClear sut = mock.Create<ClipboardAutoClear>();

		// Act
		sut.Arm();

		time.Advance(Timeout);

		await scheduled!;

		// Assert
		await clipboard
			.DidNotReceive()
			.ClearAsync();
	}

	/// <summary>
	/// <see cref="ClipboardAutoClear.Arm" />: the clear frees the gate it shares with the log of the clipboard.
	/// </summary>
	[Test]
	public async Task Arm_Frees_The_Gate_After_The_Clear()
	{
		// Arrange
		FakeTimeProvider time = new();

		IClipboardGate gate = Substitute.For<IClipboardGate>();

		Task? scheduled = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			clipboard
				.ContainsOwnershipMarkerAsync()
				.Returns(true);

			exceptionHandler.Watch(Arg.Do<Task>(task => scheduled = task));

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<TimeProvider>(time);

			builder.RegisterInstance(gate);
		});

		ClipboardAutoClear sut = mock.Create<ClipboardAutoClear>();

		// Act
		sut.Arm();

		time.Advance(Timeout);

		await scheduled!;

		// Assert
		gate
			.Received(1)
			.Release();
	}

	/// <summary>
	/// <see cref="ClipboardAutoClear.Arm" />: re-arming restarts the countdown from scratch.
	/// </summary>
	[Test]
	public async Task Arm_Restarts_Countdown_On_ReArm()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		FakeTimeProvider time = new();

		Task? scheduled = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			clipboard
				.ContainsOwnershipMarkerAsync()
				.Returns(true);

			exceptionHandler.Watch(Arg.Do<Task>(task => scheduled = task));

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<TimeProvider>(time);

			builder.RegisterType<ClipboardGate>().As<IClipboardGate>();
		});

		ClipboardAutoClear sut = mock.Create<ClipboardAutoClear>();

		// Act
		sut.Arm();

		time.Advance(TimeSpan.FromSeconds(10.0));

		// Re-arm before the first window elapses.
		sut.Arm();

		time.Advance(TimeSpan.FromSeconds(10.0));

		// Assert
		await clipboard
			.DidNotReceive()
			.ClearAsync();

		// The second window now elapses.
		time.Advance(TimeSpan.FromSeconds(5.0));

		await scheduled!;

		await clipboard
			.Received(1)
			.ClearAsync();
	}

	/// <summary>
	/// <see cref="ClipboardAutoClear.Dispose" />: a pending countdown is cancelled and does not clear.
	/// </summary>
	[Test]
	public async Task Dispose_Cancels_Pending_Clear()
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		FakeTimeProvider time = new();

		Task? scheduled = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(task => scheduled = task));

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<TimeProvider>(time);

			builder.RegisterType<ClipboardGate>().As<IClipboardGate>();
		});

		ClipboardAutoClear sut = mock.Create<ClipboardAutoClear>();

		// Act
		sut.Arm();

		sut.Dispose();

		await scheduled!;

		time.Advance(Timeout);

		// Assert
		await clipboard
			.DidNotReceive()
			.ClearAsync();
	}
	#endregion
}
