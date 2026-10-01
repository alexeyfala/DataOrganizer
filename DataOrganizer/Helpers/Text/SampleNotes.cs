using Bogus;
using DataOrganizer.Extensions;
using Repository.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Notes of the objects made up for trying the application: fixed ones that explain a few folders and made-up ones of
/// several shapes.
/// </summary>
internal static class SampleNotes
{
	#region Data
	/// <summary>
	/// Number of items in a checklist.
	/// </summary>
	private const int ChecklistItemCount = 4;

	/// <summary>
	/// Domain of the links, reserved for examples, so that no link leads to a real site.
	/// </summary>
	private const string LinkDomain = "example.com";

	/// <summary>
	/// Number of words in the path of a long link.
	/// </summary>
	private const int LongLinkWordCount = 16;

	/// <summary>
	/// Number of paragraphs in a long note.
	/// </summary>
	private const int LongTextParagraphCount = 6;

	/// <summary>
	/// Locale of the notes in Russian.
	/// </summary>
	private const string RussianLocale = "ru";

	/// <summary>
	/// Makers of the made-up notes, one for each shape, in the order the shapes take turns.
	/// </summary>
	private static readonly Func<Faker, string>[] Shapes =
	[
		static x => x.Lorem.Sentence(),
		static x => x.Lorem.Paragraph(),
		CreateLongText,
		CreateChecklist,
		CreateLinks,
		CreateLongLink,
		CreateRussianText
	];
	#endregion

	#region Properties
	/// <summary>
	/// Note of a file whose contents are binary data.
	/// </summary>
	public static string BinaryContents { get; } = "The contents are binary data, not text.";

	/// <summary>
	/// Note of an encrypted file with one byte of its contents changed.
	/// </summary>
	public static string DamagedContents { get; } = "One byte of the encrypted contents has been changed.";

	/// <summary>
	/// Note of a file named after a device.
	/// </summary>
	public static string DeviceName { get; } = "Windows reserves this name for a device.";

	/// <summary>
	/// Note of a file that shares its name with another one in its folder.
	/// </summary>
	public static string DuplicateName { get; } = "Another file in this folder has the same name.";

	/// <summary>
	/// Note of a file with emoji in its name.
	/// </summary>
	public static string EmojiName { get; } = "The name holds emoji, some of them made of several characters.";

	/// <summary>
	/// Note that tells how an encrypted script is run.
	/// </summary>
	public static string EncryptedScript { get; } = """
		The script runs from a decrypted copy in the sandbox.
		The copy is erased afterwards.
		""".ReplaceLineEndings();

	/// <summary>
	/// Note of a file with characters in its name that Windows refuses.
	/// </summary>
	public static string ForbiddenCharacters { get; } = "Windows does not allow these characters in a file name.";

	/// <summary>
	/// Note of a folder under a password, which is encrypted together with the folder.
	/// </summary>
	public static string Keeper { get; } = """
		This note is encrypted together with the folder.
		It shows once the password is entered.
		""".ReplaceLineEndings();

	/// <summary>
	/// Note of a file whose name is longer than a file system allows.
	/// </summary>
	public static string LongName { get; } = "The name is longer than a file system allows.";

	/// <summary>
	/// Note that explains why the files with made-up extensions open as plain text.
	/// </summary>
	public static string RandomFolder { get; } = """
		The extensions are made up, and no grammar knows them.
		So the files open as plain text.
		""".ReplaceLineEndings();

	/// <summary>
	/// Note of the recovery codes of an account.
	/// </summary>
	public static string RecoveryCodes { get; } = "Each code works once.";

	/// <summary>
	/// Note that tells what each folder of a run of samples holds.
	/// </summary>
	public static string RunFolder { get; } = """
		Samples made by "Sample Seeding" in the menu.
		Code, Web, Project: highlighting and folding.
		Scripts: running files.
		Data, Documents: data and text formats.
		Datasets: records of every type.
		Encrypted: folders under a password.
		Broken: files with damaged contents.
		Names: unusual file names.
		Random: files of unknown types.
		""".ReplaceLineEndings();

	/// <summary>
	/// Note that tells how each type of script is run.
	/// </summary>
	public static string Scripts { get; } = """
		Each script prints a line and waits for a key.
		.bat runs in cmd, .js in Windows Script Host.
		.ps1 opens in Notepad by default.
		The others run only if their interpreter is installed.
		""".ReplaceLineEndings();

	/// <summary>
	/// Note of a file whose name ends with a dot.
	/// </summary>
	public static string TrailingDot { get; } = "Windows drops a dot at the end of a file name.";

	/// <summary>
	/// Note of a dataset whose records break off in the middle.
	/// </summary>
	public static string TruncatedRecords { get; } = "The JSON of the records breaks off in the middle.";
	#endregion

	#region Methods
	/// <summary>
	/// Returns a made-up note whose shape follows from its number, so that the shapes take turns.
	/// </summary>
	public static string Create(Faker faker, int number) => Shapes[number % Shapes.Length](faker);

	/// <summary>
	/// Returns the note that gives the password of the encrypted folders inside a folder.
	/// </summary>
	public static string CreateEncryptedFolder(string password) => $"""
		The folders inside are encrypted.
		Password: {password}
		""".ReplaceLineEndings();

	/// <summary>
	/// Returns the note that lists the hotkeys of the snippets by the names of their files and gives the password of
	/// their encrypted folder.
	/// </summary>
	public static string CreateSnippetsFolder(IEnumerable<(string Name, KeyStroke[]? Hotkey)> snippets, string password)
	{
		IEnumerable<string> hotkeys = snippets.Select(static x => $"{x.Hotkey?.GetHotkeysPresentation() ?? "No free hotkey"}  {x.Name}");

		return string.Join(
			Environment.NewLine,
			[
				.. hotkeys,
				$"Password of the encrypted folder: {password}",
				"When a hotkey is taken, its first key moves to the right: Q, W, E and so on."
			]);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns a checklist with some of its items done.
	/// </summary>
	private static string CreateChecklist(Faker faker) => string.Join(
		Environment.NewLine,
		Enumerable
			.Range(0, ChecklistItemCount)
			.Select(_ => $"- [{(faker.Random.Bool() ? 'x' : ' ')}] {faker.Hacker.Phrase()}"));

	/// <summary>
	/// Returns the start of a sentence that a link ends.
	/// </summary>
	private static string CreateLead(Faker faker) => faker.Lorem.Sentence().TrimEnd('.');

	/// <summary>
	/// Returns a link to a page of the domain for examples.
	/// </summary>
	private static string CreateLink(Faker faker) => faker.Internet.UrlWithPath("https", LinkDomain);

	/// <summary>
	/// Returns two sentences with links: one before the full stop and one in brackets.
	/// </summary>
	private static string CreateLinks(Faker faker) => string.Join(
		Environment.NewLine,
		$"{CreateLead(faker)} {CreateLink(faker)}.",
		$"{CreateLead(faker)} ({CreateLink(faker)}).");

	/// <summary>
	/// Returns a sentence that ends with a link too long for one line.
	/// </summary>
	private static string CreateLongLink(Faker faker) => $"{CreateLead(faker)} https://{LinkDomain}/{string.Join('/', faker.Lorem.Words(LongLinkWordCount))}.";

	/// <summary>
	/// Returns several paragraphs, more than fit in the height of a note.
	/// </summary>
	private static string CreateLongText(Faker faker) => faker.Lorem.Paragraphs(
		LongTextParagraphCount,
		Environment.NewLine + Environment.NewLine);

	/// <summary>
	/// Returns a paragraph in Russian.
	/// </summary>
	private static string CreateRussianText(Faker faker)
	{
		// The generator in Russian draws from the same source, so its text stays the same from run to run.
		Faker russian = new(RussianLocale)
		{
			Random = faker.Random
		};

		return russian.Lorem.Paragraph();
	}
	#endregion
}
