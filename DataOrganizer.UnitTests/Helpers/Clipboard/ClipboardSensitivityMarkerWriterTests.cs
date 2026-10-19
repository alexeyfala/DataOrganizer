using Avalonia.Input;
using AwesomeAssertions;
using DataOrganizer.Helpers.Clipboard;
using Shared.Common;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardSensitivityMarkerWriter)}"" type")]
internal class ClipboardSensitivityMarkerWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardSensitivityMarkerWriter.AttachSensitivityMarkers" />: attaches the ownership marker, and
	/// <see cref="ClipboardSensitivityMarkerWriter.ContainsOwnershipMarker" /> detects it.
	/// </summary>
	[Test]
	public void AttachSensitivityMarkers_Attaches_Ownership_Marker()
	{
		// Arrange
		DataTransferItem item = new();

		// Act
		ClipboardSensitivityMarkerWriter.AttachSensitivityMarkers(item);

		// Assert
		ClipboardSensitivityMarkerWriter
			.ContainsOwnershipMarker(item.Formats)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardSensitivityMarkerWriter.AttachSensitivityMarkers" />: attaches the platform sensitivity marker.
	/// </summary>
	[Test]
	public void AttachSensitivityMarkers_Attaches_Platform_Marker()
	{
		// Arrange
		string identifier = ExpectedMarkerIdentifier();

		DataTransferItem item = new();

		// Act
		ClipboardSensitivityMarkerWriter.AttachSensitivityMarkers(item);

		// Assert
		item.Formats.Select(static x => x.Identifier)
			.Should()
			.Contain(identifier);
	}

	/// <summary>
	/// <see cref="ClipboardSensitivityMarkerWriter.ContainsOwnershipMarker" />: returns <c>false</c> for foreign clipboard content.
	/// </summary>
	[Test]
	public void ContainsOwnershipMarker_Is_False_For_Foreign_Content()
	{
		// Arrange
		DataTransferItem item = new();

		item.SetText(RandomString.Create(16));

		DataTransfer transfer = new();

		transfer.Add(item);

		// Assert
		ClipboardSensitivityMarkerWriter
			.ContainsOwnershipMarker(transfer.Formats)
			.Should()
			.BeFalse();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the sensitivity marker identifier the current platform writes.
	/// </summary>
	private static string ExpectedMarkerIdentifier()
	{
		if (OperatingSystem.IsWindows())
		{
			return ClipboardSensitivityMarkers.ExcludeFromMonitorProcessing;
		}

		if (OperatingSystem.IsLinux())
		{
			return ClipboardSensitivityMarkers.KdePasswordManagerHint;
		}

		return ClipboardSensitivityMarkers.NsPasteboardConcealedType;
	}
	#endregion
}
