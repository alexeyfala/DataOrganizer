using AwesomeAssertions;
using Entities.Models;
using Entities.Serialization;
using SharpHook.Data;
using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Entities.UnitTests.Serialization;

[TestFixture(Description = $@"Tests of ""{nameof(HotkeyXmlSanitizer)}"" type")]
internal class HotkeyXmlSanitizerTests
{
	#region Methods
	/// <summary>
	/// <see cref="HotkeyXmlSanitizer.Sanitize" />: a key the library still has is left alone.
	/// </summary>
	[Test]
	public void Keeps_A_Key_The_Library_Knows()
	{
		// Arrange
		XDocument document = CreateDocument(KeyCode.VcA, EventMask.LeftCtrl);

		// Act
		HotkeyXmlSanitizer.Sanitize(document);

		// Assert
		ReadHotkey(document, nameof(HotkeyEntity.Code))
			.Should()
			.Be(nameof(KeyCode.VcA));
	}

	/// <summary>
	/// <see cref="HotkeyXmlSanitizer.Sanitize" />: a mask of several flags is left exactly as the serializer wrote it.
	/// </summary>
	[Test]
	public void Keeps_A_Mask_Written_By_The_Serializer()
	{
		// Arrange
		XDocument document = CreateDocument(KeyCode.VcA, EventMask.LeftCtrl | EventMask.LeftShift);

		string mask = ReadHotkey(document, nameof(HotkeyEntity.Mask));

		// Act
		HotkeyXmlSanitizer.Sanitize(document);

		// Assert
		ReadHotkey(document, nameof(HotkeyEntity.Mask))
			.Should()
			.Be(mask);
	}

	/// <summary>
	/// <see cref="HotkeyXmlSanitizer.Sanitize" />: a document with a key the library lost is read
	/// instead of being rejected as a whole.
	/// </summary>
	[Test]
	public void Makes_A_Document_With_A_Lost_Key_Readable()
	{
		// Arrange
		XDocument document = CreateDocument(KeyCode.VcA, EventMask.LeftCtrl | EventMask.LeftShift);

		WriteHotkey(document, nameof(HotkeyEntity.Code), "VcKanji");

		// Act
		HotkeyXmlSanitizer.Sanitize(document);

		// Assert
		HotkeyEntity result = Deserialize(document);

		result
			.Code
			.Should()
			.Be(KeyCode.VcUndefined);

		result
			.Mask
			.Should()
			.Be(EventMask.LeftCtrl | EventMask.LeftShift);
	}

	/// <summary>
	/// <see cref="HotkeyXmlSanitizer.Sanitize" />: a key the library no longer has is replaced with undefined.
	/// </summary>
	[Test]
	public void Replaces_A_Key_The_Library_Lost()
	{
		// Arrange
		XDocument document = CreateDocument(KeyCode.VcA, EventMask.LeftCtrl);

		WriteHotkey(document, nameof(HotkeyEntity.Code), "VcKanji");

		// Act
		HotkeyXmlSanitizer.Sanitize(document);

		// Assert
		ReadHotkey(document, nameof(HotkeyEntity.Code))
			.Should()
			.Be(nameof(KeyCode.VcUndefined));
	}

	/// <summary>
	/// <see cref="HotkeyXmlSanitizer.Sanitize" />: a mask holding a flag the library no longer has
	/// is replaced with no mask.
	/// </summary>
	[Test]
	public void Replaces_A_Mask_With_A_Lost_Flag()
	{
		// Arrange
		XDocument document = CreateDocument(KeyCode.VcA, EventMask.LeftCtrl);

		WriteHotkey(document, nameof(HotkeyEntity.Mask), "LeftShift LeftHyper");

		// Act
		HotkeyXmlSanitizer.Sanitize(document);

		// Assert
		ReadHotkey(document, nameof(HotkeyEntity.Mask))
			.Should()
			.Be(nameof(EventMask.None));
	}

	/// <summary>
	/// <see cref="HotkeyXmlSanitizer.Sanitize" />: an empty mask is replaced with no mask.
	/// </summary>
	[Test]
	public void Replaces_An_Empty_Mask()
	{
		// Arrange
		XDocument document = CreateDocument(KeyCode.VcA, EventMask.LeftCtrl);

		WriteHotkey(document, nameof(HotkeyEntity.Mask), string.Empty);

		// Act
		HotkeyXmlSanitizer.Sanitize(document);

		// Assert
		ReadHotkey(document, nameof(HotkeyEntity.Mask))
			.Should()
			.Be(nameof(EventMask.None));
	}

	/// <summary>
	/// <see cref="HotkeyXmlSanitizer.Sanitize" />: a document without hotkeys is left alone.
	/// </summary>
	[Test]
	public void Sanitizes_A_Document_Without_Hotkeys()
	{
		// Arrange
		XDocument document = XDocument.Parse(Serialize([new FolderEntity { Name = "folder" }]));

		string xml = document.ToString();

		// Act
		HotkeyXmlSanitizer.Sanitize(document);

		// Assert
		document
			.ToString()
			.Should()
			.Be(xml);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a document holding a single file with a single hotkey.
	/// </summary>
	private static XDocument CreateDocument(KeyCode code, EventMask mask)
	{
		FileEntity file = new()
		{
			Id = Guid.NewGuid(),
			Name = "file"
		};

		file
			.Hotkeys
			.Add(new()
			{
				Code = code,
				Id = Guid.NewGuid(),
				Mask = mask,
				OwnerId = file.Id
			});

		return XDocument.Parse(Serialize([file]));
	}

	/// <summary>
	/// Reads the single hotkey of a document back.
	/// </summary>
	private static HotkeyEntity Deserialize(XDocument document)
	{
		XmlSerializer serializer = new(typeof(ExplorerItemBase[]));

		using XmlReader documentReader = document.CreateReader();

		using XmlReader reader = XmlReader.Create(documentReader, new XmlReaderSettings());

		return ((ExplorerItemBase[])serializer.Deserialize(reader)!)
			.OfType<FileEntity>()
			.Single()
			.Hotkeys
			.Single();
	}

	/// <summary>
	/// Returns the value of the given element of the single hotkey of a document.
	/// </summary>
	private static string ReadHotkey(XDocument document, string element)
	{
		return document
			.Descendants(HotkeyEntity.HotkeyElementName)
			.Single()
			.Element(element)!
			.Value;
	}

	/// <summary>
	/// Writes entities as the application does when exporting them.
	/// </summary>
	private static string Serialize(ExplorerItemBase[] entities)
	{
		XmlSerializer serializer = new(typeof(ExplorerItemBase[]));

		using StringWriter writer = new();

		serializer.Serialize(writer, entities);

		return writer.ToString();
	}

	/// <summary>
	/// Sets the value of the given element of the single hotkey of a document.
	/// </summary>
	private static void WriteHotkey(XDocument document, string element, string value)
	{
		document
			.Descendants(HotkeyEntity.HotkeyElementName)
			.Single()
			.Element(element)!
			.Value = value;
	}
	#endregion
}
