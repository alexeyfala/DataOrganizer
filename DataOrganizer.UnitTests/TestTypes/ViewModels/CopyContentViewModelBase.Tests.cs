using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using CommonTestHelpers.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.ViewModels;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(CopyContentViewModelBase)}"" type")]
internal class CopyContentViewModelBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="CopyContentViewModelBase.CopyContentAsync" />: encrypted content is flagged sensitive (written via <see cref="IClipboardAccessor.SetDataAsync" />), plaintext content uses <see cref="IClipboardAccessor.SetTextAsync" />.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentAsync_Flags_Sensitive_When_Encrypted([Values] bool isEncrypted)
	{
		// Arrange
		string content = AppUtils.CreateRandomString(20);

		FileModelDto file = TestUtils.CreateFileDto(
			encryptionStatus: isEncrypted ? EncryptionStatus.Encrypted : EncryptionStatus.None);

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		dbAccess
			.IsExistsAsync(file.Id, Arg.Any<CancellationToken>())
			.Returns(true);

		dbAccess
			.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
			.Returns(new ContentsIsValidPair
			{
				Contents = TestUtils.CreateRandomBytes(8),
				IsValid = true
			});

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		entityEncryption
			.TryToDecryptContentsAsync(Arg.Any<FileModelDto>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Encoding.UTF8.GetBytes(content));

		TestCopyContentViewModel sut = new(
			Application.Current!,
			clipboard,
			dbAccess,
			Substitute.For<IDialogService>(),
			entityEncryption,
			Substitute.For<ILogger>(),
			Substitute.For<IMessenger>(),
			Substitute.For<ITaskExceptionHandler>());

		// Act
		await sut.InvokeCopyContentAsync(file, new ItemsControl());

		// Assert
		if (isEncrypted)
		{
			await clipboard
				.Received(1)
				.SetDataAsync(Arg.Any<DataTransfer>());

			await clipboard
				.DidNotReceive()
				.SetTextAsync(Arg.Any<string>());
		}
		else
		{
			await clipboard
				.Received(1)
				.SetTextAsync(content);

			await clipboard
				.DidNotReceive()
				.SetDataAsync(Arg.Any<DataTransfer>());
		}
	}
	#endregion

	#region Nested Types
	/// <summary>
	/// Minimal concrete <see cref="CopyContentViewModelBase" /> exposing the protected copy operation.
	/// </summary>
	private sealed class TestCopyContentViewModel : CopyContentViewModelBase
	{
		#region Constructors
		public TestCopyContentViewModel(
			Application app,
			IClipboardAccessor clipboard,
			IDbAccess dbAccess,
			IDialogService dialogService,
			IEntityEncryption entityEncryption,
			ILogger logger,
			IMessenger messenger,
			ITaskExceptionHandler exceptionHandler) : base(
				app,
				clipboard,
				dbAccess,
				dialogService,
				entityEncryption,
				logger,
				messenger,
				exceptionHandler)
		{
		}
		#endregion

		#region Methods
		public Task InvokeCopyContentAsync(FileModelDto file, ItemsControl container) =>
			CopyContentAsync(file, container, updateView: false);
		#endregion
	}
	#endregion
}
