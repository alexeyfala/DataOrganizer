using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(FileTextCopier)}"" type")]
internal class FileTextCopierTests
{
	#region Data
	/// <summary>
	/// Header of the password prompt.
	/// </summary>
	private const string Header = "Copy content";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="FileTextCopier.CopyAsync" />: contents that hold no text put nothing on the clipboard, and their status
	/// comes back.
	/// </summary>
	[Test]
	public async Task CopyAsync_Copies_Nothing_Without_A_Text(
		[Values(FileTextStatus.Empty, FileTextStatus.NotText, FileTextStatus.Damaged, FileTextStatus.Closed)] FileTextStatus status)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IClipboardTextWriter clipboardWriter = Substitute.For<IClipboardTextWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileTextReader fileTextReader = Substitute.For<IFileTextReader>();

			fileTextReader
				.ReadAsync(file, Header, Arg.Any<CancellationToken>())
				.Returns(new FileTextReading
				{
					Status = status
				});

			builder.RegisterInstance(clipboardWriter);

			builder.RegisterInstance(fileTextReader);
		});

		FileTextCopier sut = mock.Create<FileTextCopier>();

		// Act
		FileTextStatus result = await sut.CopyAsync(file, Header);

		// Assert
		result
			.Should()
			.Be(status);

		await clipboardWriter
			.DidNotReceive()
			.WriteTextAsync(Arg.Any<string>(), Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="FileTextCopier.CopyAsync" />: the text read under the header of the prompt reaches the clipboard, as
	/// a sensitive one when the file is encrypted.
	/// </summary>
	[TestCase(EncryptionStatus.None, false)]
	[TestCase(EncryptionStatus.Encrypted, true)]
	[TestCase(EncryptionStatus.Decrypted, true)]
	public async Task CopyAsync_Copies_The_Text(EncryptionStatus encryptionStatus, bool isSensitive)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: encryptionStatus);

		IClipboardTextWriter clipboardWriter = Substitute.For<IClipboardTextWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileTextReader fileTextReader = Substitute.For<IFileTextReader>();

			fileTextReader
				.ReadAsync(file, Header, Arg.Any<CancellationToken>())
				.Returns(new FileTextReading
				{
					Status = FileTextStatus.Text,
					Text = "Hi"
				});

			builder.RegisterInstance(clipboardWriter);

			builder.RegisterInstance(fileTextReader);
		});

		FileTextCopier sut = mock.Create<FileTextCopier>();

		// Act
		FileTextStatus result = await sut.CopyAsync(file, Header);

		// Assert
		result
			.Should()
			.Be(FileTextStatus.Text);

		await clipboardWriter
			.Received(1)
			.WriteTextAsync("Hi", isSensitive);
	}
	#endregion
}
