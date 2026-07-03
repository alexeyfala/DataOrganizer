using Avalonia.Input;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Services.Clipboard;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.Clipboard;

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
		Context context = CreateContext(ownershipPresent: true);

		// Act
		context.Sut.Arm();

		context.Time.Advance(Timeout);

		await context.Scheduled!;

		// Assert
		await context.Clipboard
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
		Context context = CreateContext(ownershipPresent: false);

		// Act
		context.Sut.Arm();

		context.Time.Advance(Timeout);

		await context.Scheduled!;

		// Assert
		await context.Clipboard
			.DidNotReceive()
			.ClearAsync();
	}

	/// <summary>
	/// <see cref="ClipboardAutoClear.Arm" />: re-arming restarts the countdown from scratch.
	/// </summary>
	[Test]
	public async Task Arm_Restarts_Countdown_On_ReArm()
	{
		// Arrange
		Context context = CreateContext(ownershipPresent: true);

		// Act
		context.Sut.Arm();

		context.Time.Advance(TimeSpan.FromSeconds(10.0));

		// Re-arm before the first window elapses.
		context.Sut.Arm();

		context.Time.Advance(TimeSpan.FromSeconds(10.0));

		// Assert
		await context.Clipboard
			.DidNotReceive()
			.ClearAsync();

		// The second window now elapses.
		context.Time.Advance(TimeSpan.FromSeconds(5.0));

		await context.Scheduled!;

		await context.Clipboard
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
		Context context = CreateContext(ownershipPresent: true);

		// Act
		context.Sut.Arm();

		context.Sut.Dispose();

		await context.Scheduled!;

		context.Time.Advance(Timeout);

		// Assert
		await context.Clipboard
			.DidNotReceive()
			.ClearAsync();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds a service under test wired with a fake clock and a captured scheduled task.
	/// </summary>
	private static Context CreateContext(bool ownershipPresent)
	{
		IReadOnlyList<DataFormat> formats = ownershipPresent
			? [DataFormat.CreateBytesApplicationFormat(ClipboardSensitivityMarkers.AutoClearOwnership)]
			: [];

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		clipboard
			.GetDataFormatsAsync()
			.Returns(formats);

		ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

		FakeTimeProvider time = new();

		Context context = new()
		{
			Clipboard = clipboard,
			Sut = new ClipboardAutoClear(
				clipboard,
				new ClipboardGate(),
				Substitute.For<ILogger>(),
				exceptionHandler,
				time),
			Time = time
		};

		exceptionHandler
			.When(static x => x.Watch(Arg.Any<Task>()))
			.Do(callInfo => context.Scheduled = callInfo.Arg<Task>());

		return context;
	}
	#endregion

	#region Nested Types
	/// <summary>
	/// Bundles the service under test with its captured collaborators.
	/// </summary>
	private sealed class Context
	{
		#region Properties
		public required IClipboardAccessor Clipboard { get; init; }

		public Task? Scheduled { get; set; }

		public required ClipboardAutoClear Sut { get; init; }

		public required FakeTimeProvider Time { get; init; }
		#endregion
	}
	#endregion
}
