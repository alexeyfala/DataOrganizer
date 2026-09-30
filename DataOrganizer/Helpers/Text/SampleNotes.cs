using Bogus;
using System;
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
	/// Note that explains why the files with made-up extensions open as plain text.
	/// </summary>
	public static string RandomFolder { get; } = """
		The extensions are made up, and no grammar knows them.
		So the files open as plain text.
		""".ReplaceLineEndings();

	/// <summary>
	/// Note that tells what each folder of a run of samples holds.
	/// </summary>
	public static string RunFolder { get; } = """
		Samples made by --fillobjects.
		Code, Web, Project: highlighting and folding.
		Scripts: running files.
		Data, Documents: data and text formats.
		Datasets: records of every type.
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
	#endregion

	#region Methods
	/// <summary>
	/// Returns a made-up note whose shape follows from its number, so that the shapes take turns.
	/// </summary>
	public static string Create(Faker faker, int number) => Shapes[number % Shapes.Length](faker);
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
