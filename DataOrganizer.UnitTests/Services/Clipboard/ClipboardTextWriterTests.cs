using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Services.Clipboard;
using NSubstitute;
using Shared.Common;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardTextWriter)}"" type")]
internal class ClipboardTextWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardTextWriter.WriteTextAsync" />: a sensitive text starts the auto-clear once it is on the clipboard.
	/// </summary>
	[Test]
	public async Task WriteTextAsync_Arms_The_Auto_Clear_After_A_Sensitive_Copy()
	{
		// Arrange
		string text = RandomString.Create(16);

		IClipboardAutoClear autoClear = Substitute.For<IClipboardAutoClear>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			clipboard
				.SetTextAsync(text, html: null, rtf: null, isSensitive: true, isRestored: false)
				.Returns(true);

			builder.RegisterInstance(autoClear);

			builder.RegisterInstance(clipboard);
		});

		ClipboardTextWriter sut = mock.Create<ClipboardTextWriter>();

		// Act
		await sut.WriteTextAsync(text, isSensitive: true);

		// Assert
		autoClear
			.Received(1)
			.Arm();
	}

	/// <summary>
	/// <see cref="ClipboardTextWriter.WriteTextAsync" />: a sensitive text reaches the clipboard marked as a secret.
	/// </summary>
	[Test]
	public async Task WriteTextAsync_Marks_A_Sensitive_Text_As_Secret()
	{
		// Arrange
		string text = RandomString.Create(16);

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			clipboard
				.SetTextAsync(text, html: null, rtf: null, isSensitive: true, isRestored: false)
				.Returns(true);

			builder.RegisterInstance(clipboard);
		});

		ClipboardTextWriter sut = mock.Create<ClipboardTextWriter>();

		// Act
		bool result = await sut.WriteTextAsync(text, isSensitive: true);

		// Assert
		result
			.Should()
			.BeTrue();

		await clipboard
			.Received(1)
			.SetTextAsync(text, html: null, rtf: null, isSensitive: true, isRestored: false);

		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: false, isRestored: false);
	}

	/// <summary>
	/// <see cref="ClipboardTextWriter.WriteTextAsync" />: an ordinary text reaches the clipboard as plain text
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
			clipboard
				.SetTextAsync(text, html: null, rtf: null, isSensitive: false, isRestored: false)
				.Returns(true);

			builder.RegisterInstance(autoClear);

			builder.RegisterInstance(clipboard);
		});

		ClipboardTextWriter sut = mock.Create<ClipboardTextWriter>();

		// Act
		bool result = await sut.WriteTextAsync(text, isSensitive: false);

		// Assert
		result
			.Should()
			.BeTrue();

		await clipboard
			.Received(1)
			.SetTextAsync(text, html: null, rtf: null, isSensitive: false, isRestored: false);

		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: true, isRestored: false);

		autoClear
			.DidNotReceive()
			.Arm();
	}

	/// <summary>
	/// <see cref="ClipboardTextWriter.WriteTextAsync" />: a clipboard that refuses the text is reported
	/// and starts no auto-clear.
	/// </summary>
	[Test]
	public async Task WriteTextAsync_Reports_A_Refused_Write([Values] bool isSensitive)
	{
		// Arrange
		string text = RandomString.Create(16);

		IClipboardAutoClear autoClear = Substitute.For<IClipboardAutoClear>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

			// The clipboard refuses the text.
			clipboard
				.SetTextAsync(text, html: null, rtf: null, isSensitive, isRestored: false)
				.Returns(false);

			builder.RegisterInstance(autoClear);

			builder.RegisterInstance(clipboard);
		});

		ClipboardTextWriter sut = mock.Create<ClipboardTextWriter>();

		// Act
		bool result = await sut.WriteTextAsync(text, isSensitive);

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
