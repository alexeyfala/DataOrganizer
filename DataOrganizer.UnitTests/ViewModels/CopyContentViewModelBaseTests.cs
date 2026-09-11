using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.ViewModels;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Dto;

namespace DataOrganizer.UnitTests.ViewModels;

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
		string content = RandomString.Create(20);

		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: isEncrypted
			? EncryptionStatus.Encrypted
			: EncryptionStatus.None);

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(8),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(Encoding.UTF8.GetBytes(content));

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		TestCopyContentViewModel sut = mock.Create<TestCopyContentViewModel>();

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
			IContentCipher contentCipher,
			IDbAccess dbAccess,
			IDialogService dialogService,
			ILogger logger,
			IMessenger messenger,
			INotificationService notification,
			ITaskExceptionHandler exceptionHandler) : base(
				app,
				clipboard,
				contentCipher,
				dbAccess,
				dialogService,
				logger,
				messenger,
				notification,
				exceptionHandler)
		{
		}
		#endregion

		#region Methods
		public Task InvokeCopyContentAsync(FileDto file, ItemsControl container)
		{
			return CopyContentAsync(file, container, updateView: false);
		}
		#endregion
	}
	#endregion
}
