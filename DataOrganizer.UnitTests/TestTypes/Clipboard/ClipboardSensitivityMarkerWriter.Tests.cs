using Avalonia.Input;
using AwesomeAssertions;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces.Clipboard;
using NSubstitute;
using Shared.Common;

namespace DataOrganizer.UnitTests.TestTypes.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardSensitivityMarkerWriter)}"" type")]
internal class ClipboardSensitivityMarkerWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardSensitivityMarkerWriter.ContainsOwnershipMarker" />: returns <c>false</c> for foreign clipboard content.
	/// </summary>
	[Test]
	public void ContainsOwnershipMarker_Is_False_For_Foreign_Content()
	{
		// Arrange
		DataTransferItem item = new();

		item.SetText(AppUtils.CreateRandomString(16));

		DataTransfer transfer = new();

		transfer.Add(item);

		// Assert
		ClipboardSensitivityMarkerWriter
			.ContainsOwnershipMarker(transfer.Formats)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardSensitivityMarkerWriter.CreateSensitiveText" />: arms the configured auto-clear scheduler.
	/// </summary>
	[Test]
	public void CreateSensitiveText_Arms_AutoClear()
	{
		// Arrange
		IClipboardAutoClear autoClear = Substitute.For<IClipboardAutoClear>();

		ClipboardSensitivityMarkerWriter.Configure(autoClear);

		// Act
		ClipboardSensitivityMarkerWriter.CreateSensitiveText(AppUtils.CreateRandomString(16));

		// Assert
		autoClear
			.Received(1)
			.Arm();
	}

	/// <summary>
	/// <see cref="ClipboardSensitivityMarkerWriter.CreateSensitiveText" />: attaches the ownership marker, and <see cref="ClipboardSensitivityMarkerWriter.ContainsOwnershipMarker" /> detects it.
	/// </summary>
	[Test]
	public void CreateSensitiveText_Attaches_Ownership_Marker()
	{
		// Act
		DataTransfer transfer = ClipboardSensitivityMarkerWriter.CreateSensitiveText(AppUtils.CreateRandomString(16));

		// Assert
		ClipboardSensitivityMarkerWriter
			.ContainsOwnershipMarker(transfer.Formats)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardSensitivityMarkerWriter.CreateSensitiveText" />: attaches the platform sensitivity marker.
	/// </summary>
	[Test]
	public void CreateSensitiveText_Attaches_Platform_Marker()
	{
		// Arrange
		string identifier = ExpectedMarkerIdentifier();

		// Act
		IDataTransfer transfer = ClipboardSensitivityMarkerWriter.CreateSensitiveText(AppUtils.CreateRandomString(16));

		// Assert
		transfer
			.Contains(DataFormat.CreateBytesPlatformFormat(identifier))
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardSensitivityMarkerWriter.CreateSensitiveText" />: carries the text payload.
	/// </summary>
	[Test]
	public void CreateSensitiveText_Carries_Text()
	{
		// Arrange
		string text = AppUtils.CreateRandomString(16);

		// Act
		IDataTransfer transfer = ClipboardSensitivityMarkerWriter.CreateSensitiveText(text);

		// Assert
		transfer
			.TryGetText()
			.Should()
			.Be(text);
	}

	/// <summary>
	/// Resets the static auto-clear wiring so the arming test does not leak into other tests.
	/// </summary>
	[TearDown]
	public void ResetAutoClear() => ClipboardSensitivityMarkerWriter.Configure(null);
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the sensitivity marker identifier the current platform writes.
	/// </summary>
	private static string ExpectedMarkerIdentifier()
	{
		if (AppUtils.IsWindows)
		{
			return ClipboardSensitivityMarkers.ExcludeFromMonitorProcessing;
		}

		if (AppUtils.IsLinux)
		{
			return ClipboardSensitivityMarkers.KdePasswordManagerHint;
		}

		return ClipboardSensitivityMarkers.NsPasteboardConcealedType;
	}
	#endregion
}
