using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Input;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Services.Clipboard;
using NSubstitute;
using Shared.Common;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(SensitiveClipboardWriter)}"" type")]
internal class SensitiveClipboardWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="SensitiveClipboardWriter.Write" />: the text reaches the clipboard as a marked payload.
	/// </summary>
	[Test]
	public async Task Write_Places_A_Marked_Payload()
	{
		// Arrange
		string text = RandomString.Create(16);

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(clipboard));

		SensitiveClipboardWriter sut = mock.Create<SensitiveClipboardWriter>();

		// Act
		sut.Write(text);

		// Assert
		await clipboard
			.Received(1)
			.SetDataAsync(Arg.Is<DataTransfer>(x => x.TryGetText() == text
				&& ClipboardSensitivityMarkerWriter.ContainsOwnershipMarker(x.Formats)));
	}
	#endregion
}
