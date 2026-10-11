using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Input;
using AwesomeAssertions;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Services.Clipboard;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Common;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(SensitiveClipboardWriter)}"" type")]
internal class SensitiveClipboardWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="SensitiveClipboardWriter.WriteTextAsync" />: a sensitive text starts the auto-clear once it is on the clipboard.
	/// </summary>
	[Test]
	public async Task WriteTextAsync_Arms_The_Auto_Clear_After_A_Sensitive_Copy()
	{
		// Arrange
		IClipboardAutoClear autoClear = Substitute.For<IClipboardAutoClear>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(autoClear));

		SensitiveClipboardWriter sut = mock.Create<SensitiveClipboardWriter>();

		// Act
		await sut.WriteTextAsync(RandomString.Create(16), isSensitive: true);

		// Assert
		autoClear
			.Received(1)
			.Arm();
	}

	/// <summary>
	/// <see cref="SensitiveClipboardWriter.WriteTextAsync" />: a sensitive text reaches the clipboard as a marked payload.
	/// </summary>
	[Test]
	public async Task WriteTextAsync_Places_A_Marked_Payload_For_A_Sensitive_Text()
	{
		// Arrange
		string text = RandomString.Create(16);

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(clipboard));

		SensitiveClipboardWriter sut = mock.Create<SensitiveClipboardWriter>();

		// Act
		bool result = await sut.WriteTextAsync(text, isSensitive: true);

		// Assert
		result
			.Should()
			.BeTrue();

		await clipboard
			.Received(1)
			.SetDataAsync(Arg.Is<DataTransfer>(x => x.TryGetText() == text
				&& ClipboardSensitivityMarkerWriter.ContainsOwnershipMarker(x.Formats)));

		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="SensitiveClipboardWriter.WriteTextAsync" />: an ordinary text reaches the clipboard as plain text
	/// and leaves the auto-clear alone.
	/// </summary>
	[Test]
	public async Task WriteTextAsync_Places_Plain_Text_For_An_Ordinary_Text()
	{
		// Arrange
		string text = RandomString.Create(16);

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		IClipboardAutoClear autoClear = Substitute.For<IClipboardAutoClear>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(autoClear);

			builder.RegisterInstance(clipboard);
		});

		SensitiveClipboardWriter sut = mock.Create<SensitiveClipboardWriter>();

		// Act
		bool result = await sut.WriteTextAsync(text, isSensitive: false);

		// Assert
		result
			.Should()
			.BeTrue();

		await clipboard
			.Received(1)
			.SetTextAsync(text);

		await clipboard
			.DidNotReceive()
			.SetDataAsync(Arg.Any<DataTransfer>());

		autoClear
			.DidNotReceive()
			.Arm();
	}

	/// <summary>
	/// <see cref="SensitiveClipboardWriter.WriteTextAsync" />: a clipboard that refuses the text is reported
	/// and starts no auto-clear.
	/// </summary>
	[Test]
	public async Task WriteTextAsync_Reports_A_Refused_Write([Values] bool isSensitive)
	{
		// Arrange
		IClipboardAutoClear autoClear = Substitute.For<IClipboardAutoClear>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			// Each row reaches only the write of its own kind.
			if (isSensitive)
			{
				clipboard
					.SetDataAsync(Arg.Any<DataTransfer>())
					.ThrowsAsync(new InvalidOperationException());
			}
			else
			{
				clipboard
					.SetTextAsync(Arg.Any<string>())
					.ThrowsAsync(new InvalidOperationException());
			}

			builder.RegisterInstance(autoClear);

			builder.RegisterInstance(clipboard);
		});

		SensitiveClipboardWriter sut = mock.Create<SensitiveClipboardWriter>();

		// Act
		bool result = await sut.WriteTextAsync(RandomString.Create(16), isSensitive);

		// Assert
		result
			.Should()
			.BeFalse();

		autoClear
			.DidNotReceive()
			.Arm();
	}
	#endregion
}
