using Bogus;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Texts of data and document formats made up for trying the application, with values from a generator.
/// </summary>
internal static partial class SampleDocuments
{
	#region Data
	/// <summary>
	/// Number of rows in the table of comma-separated values.
	/// </summary>
	private const int CsvRowCount = 50;

	/// <summary>
	/// Format of a moment in the texts.
	/// </summary>
	private const string DateFormat = "yyyy-MM-ddTHH:mm:ss";

	/// <summary>
	/// Format of a day in the texts.
	/// </summary>
	private const string DayFormat = "yyyy-MM-dd";

	/// <summary>
	/// Level of a line of the log that reports an error.
	/// </summary>
	private const string ErrorLevel = "ERR";

	/// <summary>
	/// Format of a moment in the log.
	/// </summary>
	private const string LogDateFormat = "yyyy-MM-dd HH:mm:ss.fff";

	/// <summary>
	/// Number of lines in the log, not counting the exceptions.
	/// </summary>
	private const int LogLineCount = 200;

	/// <summary>
	/// Share of the lines of the log that name a request.
	/// </summary>
	private const float LogRequestShare = 0.3f;

	/// <summary>
	/// Share of the users with a note.
	/// </summary>
	private const float NoteShare = 0.7f;

	/// <summary>
	/// Number of products in the catalog.
	/// </summary>
	private const int ProductCount = 12;

	/// <summary>
	/// Number of recovery codes of an account.
	/// </summary>
	private const int RecoveryCodeCount = 10;

	/// <summary>
	/// Pattern of a recovery code, where each star stands for a letter or a digit.
	/// </summary>
	private const string RecoveryCodeFormat = "****-****";

	/// <summary>
	/// Number of bytes of the signing key among the secrets.
	/// </summary>
	private const int SigningKeySize = 32;

	/// <summary>
	/// Number of rows that the SQL script inserts.
	/// </summary>
	private const int SqlRowCount = 20;

	/// <summary>
	/// Number of rows in the tables of Markdown and HTML.
	/// </summary>
	private const int TableRowCount = 5;

	/// <summary>
	/// Number of paragraphs in the plain text.
	/// </summary>
	private const int TextParagraphCount = 5;

	/// <summary>
	/// Number of users in the settings.
	/// </summary>
	private const int UserCount = 8;

	/// <summary>
	/// Characters that put a value of comma-separated values in quotes.
	/// </summary>
	private static readonly SearchValues<char> CsvQuotedCharacters = SearchValues.Create(",\"\r\n");

	/// <summary>
	/// Moment that the made-up dates go back from, which keeps them the same from run to run.
	/// </summary>
	private static readonly DateTime DateReference = new(2026, 1, 1);

	/// <summary>
	/// Options of a JSON text that keep the characters of its values as they are.
	/// </summary>
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		WriteIndented = true
	};

	/// <summary>
	/// Levels of the lines of the log.
	/// </summary>
	private static readonly string[] LogLevels = ["DBG", "INF", "WRN", ErrorLevel];

	/// <summary>
	/// Weights of <see cref="LogLevels" />, in their order.
	/// </summary>
	private static readonly float[] LogLevelWeights = [0.3f, 0.5f, 0.15f, 0.05f];
	#endregion

	#region Methods
	/// <summary>
	/// Returns a CSS text of a style sheet, with imports, comments, regions and a media query.
	/// </summary>
	public static string CreateCss(Faker faker)
	{
		return $$"""
			@import url("{{faker.System.CommonFileName("css")}}");
			@import url("{{faker.System.CommonFileName("css")}}");

			/*
			 * {{faker.Lorem.Sentence()}}
			 * {{faker.Lorem.Sentence()}}
			 */

			/* #region Layout */
			{{CreateCssRule(faker, "body")}}

			{{CreateCssRule(faker, $".{faker.Lorem.Word()}")}}
			/* #endregion */

			/* #region Links */
			{{CreateCssRule(faker, "a:hover")}}
			/* #endregion */

			@media (max-width: 600px) {
			{{IndentLines(CreateCssRule(faker, $".{faker.Lorem.Word()}"), Indent(1))}}
			}

			""".ReplaceLineEndings();
	}

	/// <summary>
	/// Returns a table of people in comma-separated values, with a header row.
	/// </summary>
	public static string CreateCsv(Faker faker)
	{
		IEnumerable<string> rows = CreatePeople(faker, CsvRowCount).Select(static (x, i) => string.Join(
			',',
			i + 1,
			QuoteCsv(x.Name),
			QuoteCsv(x.Email),
			QuoteCsv(x.City),
			QuoteCsv(x.Company),
			FormatDay(x.Birthday)));

		return string.Join(Environment.NewLine, ["Id,Name,Email,City,Company,Birthday", .. rows, string.Empty]);
	}

	/// <summary>
	/// Returns a unified diff of two versions of a text, in two hunks.
	/// </summary>
	public static string CreateDiff(Faker faker)
	{
		return $"""
			diff --git a/notes.txt b/notes.txt
			index {faker.Random.Hexadecimal(7, string.Empty)}..{faker.Random.Hexadecimal(7, string.Empty)} 100644
			--- a/notes.txt
			+++ b/notes.txt
			@@ -1,5 +1,6 @@
			 {faker.Lorem.Sentence()}
			 {faker.Lorem.Sentence()}
			-{faker.Lorem.Sentence()}
			+{faker.Lorem.Sentence()}
			+{faker.Lorem.Sentence()}
			 {faker.Lorem.Sentence()}
			 {faker.Lorem.Sentence()}
			@@ -12,3 +13,3 @@
			 {faker.Lorem.Sentence()}
			-{faker.Lorem.Sentence()}
			+{faker.Lorem.Sentence()}
			 {faker.Lorem.Sentence()}

			""".ReplaceLineEndings();
	}

	/// <summary>
	/// Returns an HTML text of a page with navigation, sections, a table and a comment of several lines.
	/// </summary>
	public static string CreateHtml(Faker faker)
	{
		string title = faker.Company.CompanyName();

		XElement html = new(
			"html",
			new XAttribute("lang", "en"),
			new XElement(
				"head",
				new XElement("meta", new XAttribute("charset", "utf-8")),
				new XElement("title", title),
				new XElement(
					"link",
					new XAttribute("rel", "stylesheet"),
					new XAttribute("href", "styles.css"))),
			new XElement(
				"body",
				new XComment(" #region Navigation "),
				new XElement(
					"nav",
					new XElement(
						"ul",
						Enumerable
							.Range(0, 4)
							.Select(_ => faker.Lorem.Word())
							.Select(static x => new XElement(
								"li",
								new XElement(
									"a",
									new XAttribute("href", $"#{x}"),
									x))))),
				new XComment(" #endregion "),
				new XElement(
					"main",
					new XElement("h1", title),
					new XElement("p", faker.Company.CatchPhrase()),
					Enumerable
						.Range(0, 3)
						.Select(_ => new XElement(
							"section",
							new XElement("h2", CreateHeading(faker)),
							new XElement("p", faker.Lorem.Paragraph()))),
					CreateHtmlTable(faker)),
				new XComment(CreateCommentText(faker, depth: 2)),
				new XElement(
					"footer",
					new XElement("p", $"{title}, {DateReference.Year}"))));

		return $"<!DOCTYPE html>{Environment.NewLine}{html}{Environment.NewLine}";
	}

	/// <summary>
	/// Returns an INI text of settings in sections, with a comment of several lines at its head.
	/// </summary>
	public static string CreateIni(Faker faker)
	{
		return $"""
			; {faker.Lorem.Sentence()}
			; {faker.Lorem.Sentence()}

			[General]
			name={faker.Company.CompanyName()}
			version={faker.System.Semver()}
			website={faker.Internet.Url()}

			[Database]
			engine={faker.Database.Engine()}
			host={faker.Internet.DomainName()}
			port={faker.Internet.Port()}
			user={faker.Internet.UserName()}

			[Paths]
			data={faker.System.DirectoryPath()}
			logs={faker.System.DirectoryPath()}
			backup={faker.System.FilePath()}

			""".ReplaceLineEndings();
	}

	/// <summary>
	/// Returns a JSON text of the settings of an application with its users.
	/// </summary>
	public static string CreateJson(Faker faker) => CreateSettings(faker).ToJsonString(JsonOptions);

	/// <summary>
	/// Returns a JSON text with comments of the settings of an editor.
	/// </summary>
	public static string CreateJsonWithComments(Faker faker)
	{
		return $$"""
			// Settings of an editor.
			// Comments like these are allowed in this format.
			{
			  /* The look of the editor. */
			  "theme": {{QuoteJson(faker.PickRandom("Light", "Dark"))}},
			  "fontSize": {{faker.Random.Int(10, 18)}},
			  "wordWrap": {{(faker.Random.Bool() ? "true" : "false")}},

			  // Files that open on start.
			  "recentFiles": [
			    {{QuoteJson(faker.System.FilePath())}},
			    {{QuoteJson(faker.System.FilePath())}},
			    {{QuoteJson(faker.System.FilePath())}}
			  ],

			  /*
			   * Keys of the commands,
			   * each a key with its modifiers.
			   */
			  "keys": {
			    "save": "Ctrl+S",
			    "find": "Ctrl+F",
			    "fold": "Ctrl+M"
			  }
			}

			""".ReplaceLineEndings();
	}

	/// <summary>
	/// Returns a LaTeX article with packages, sections, a list, an equation, comments and a region.
	/// </summary>
	public static string CreateLatex(Faker faker)
	{
		return $$"""
			\documentclass[11pt]{article}

			\usepackage[utf8]{inputenc}
			\usepackage{amsmath}
			\usepackage{hyperref}

			% {{faker.Lorem.Sentence()}}
			% {{faker.Lorem.Sentence()}}

			\title{{{faker.Commerce.ProductName()}}}
			\author{{{faker.Name.FullName()}}}
			\date{\today}

			\begin{document}

			\maketitle

			% region Introduction
			\section{{{CreateHeading(faker)}}}
			{{faker.Lorem.Paragraph()}}
			% endregion

			\section{{{CreateHeading(faker)}}}
			{{faker.Lorem.Paragraph()}}

			\begin{itemize}
			  \item {{faker.Lorem.Sentence()}}
			  \item {{faker.Lorem.Sentence()}}
			  \item {{faker.Lorem.Sentence()}}
			\end{itemize}

			\begin{equation}
			  E = mc^2
			\end{equation}

			\end{document}

			""".ReplaceLineEndings();
	}

	/// <summary>
	/// Returns a log of an application, where each error carries its exception.
	/// </summary>
	public static string CreateLog(Faker faker)
	{
		StringBuilder builder = new();

		DateTime moment = DateReference;

		for (int i = 0; i < LogLineCount; i++)
		{
			moment = moment.AddMilliseconds(faker.Random.Int(1, 5000));

			string level = faker.Random.WeightedRandom(LogLevels, LogLevelWeights);

			string request = faker.Random.Bool(LogRequestShare)
				? $" (request {faker.Random.Guid()} from {faker.Internet.Ip()})"
				: string.Empty;

			builder.AppendLine($"{moment.ToString(LogDateFormat, CultureInfo.InvariantCulture)} [{level}] {faker.Hacker.Phrase()}{request}");

			if (level == ErrorLevel)
			{
				builder.AppendLine(faker.System.Exception().ToString());
			}
		}

		return builder.ToString();
	}

	/// <summary>
	/// Returns a Markdown text with headings of three levels, lists, a quote, a table, comments, a region and a block of
	/// code.
	/// </summary>
	public static string CreateMarkdown(Faker faker)
	{
		string rows = string.Join(
			Environment.NewLine,
			CreatePeople(faker, TableRowCount).Select(static x => $"| {x.Name} | {x.Email} | {x.City} |"));

		return $"""
			# {faker.Commerce.ProductName()}

			{faker.Lorem.Paragraph()}

			<!--
			{faker.Lorem.Sentence()}
			{faker.Lorem.Sentence()}
			-->

			## {CreateHeading(faker)}

			{faker.Lorem.Paragraph()}

			- {faker.Lorem.Sentence()}
			- {faker.Lorem.Sentence()}
			- {faker.Lorem.Sentence()}

			### {CreateHeading(faker)}

			1. {faker.Lorem.Sentence()}
			2. {faker.Lorem.Sentence()}
			3. {faker.Lorem.Sentence()}

			> {faker.Company.CatchPhrase()}

			## {CreateHeading(faker)}

			<!-- #region People -->
			| Name | Email | City |
			| --- | --- | --- |
			{rows}
			<!-- #endregion -->

			## {CreateHeading(faker)}

			```json
			{CreateUser(faker, 1).ToJsonString(JsonOptions)}
			```

			[{faker.Company.CompanyName()}]({faker.Internet.Url()})

			""".ReplaceLineEndings();
	}

	/// <summary>
	/// Returns one-time recovery codes of an account, one on each line.
	/// </summary>
	public static string CreateRecoveryCodes(Faker faker)
	{
		IEnumerable<string> codes = Enumerable
			.Range(0, RecoveryCodeCount)
			.Select(_ => faker.Random.Replace(RecoveryCodeFormat));

		return string.Join(Environment.NewLine, [.. codes, string.Empty]);
	}

	/// <summary>
	/// Returns an SCSS text of a style sheet with modules, variables, nesting, a mixin, comments and a region.
	/// </summary>
	public static string CreateScss(Faker faker)
	{
		return $$"""
			@use "sass:color";
			@use "sass:math";

			// Colors of the theme.
			// A generator made them up.

			$primary: {{faker.Internet.Color()}};
			$accent: {{faker.Internet.Color()}};
			$gap: {{faker.Random.Int(4, 16)}}px;

			/* #region Layout */
			.{{faker.Lorem.Word()}} {
			  color: $primary;
			  padding: math.div($gap, 2);

			  &:hover {
			    color: color.adjust($primary, $lightness: 10%);
			  }

			  .{{faker.Lorem.Word()}} {
			    margin: $gap;
			    border: 1px solid $accent;
			  }
			}
			/* #endregion */

			@mixin card($radius: 4px) {
			  border-radius: $radius;
			  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.2);
			}

			.{{faker.Lorem.Word()}} {
			  @include card({{faker.Random.Int(2, 12)}}px);
			}

			""".ReplaceLineEndings();
	}

	/// <summary>
	/// Returns secrets of an application in JSON: a connection string, a mail account, API keys and a signing key.
	/// </summary>
	public static string CreateSecrets(Faker faker)
	{
		string host = faker.Internet.DomainName();

		JsonObject secrets = new()
		{
			["ConnectionStrings"] = new JsonObject
			{
				["Default"] = $"Server={host};Database={faker.Hacker.Noun()};User Id={faker.Internet.UserName()};Password={faker.Internet.Password()}"
			},
			["Smtp"] = new JsonObject
			{
				["Host"] = $"smtp.{host}",
				["User"] = faker.Internet.Email(),
				["Password"] = faker.Internet.Password()
			},
			["ApiKeys"] = new JsonObject
			{
				["Maps"] = faker.Random.Hash(),
				["Payments"] = faker.Random.Hash()
			},
			["Jwt"] = new JsonObject
			{
				["SigningKey"] = Convert.ToBase64String(faker.Random.Bytes(SigningKeySize))
			}
		};

		return secrets.ToJsonString(JsonOptions);
	}

	/// <summary>
	/// Returns an SQL script that creates a table, fills it and queries it, with comments and regions.
	/// </summary>
	public static string CreateSql(Faker faker)
	{
		string rows = string.Join(
			$",{Environment.NewLine}",
			CreatePeople(faker, SqlRowCount).Select(static (x, i) => $"    ({i + 1}, {QuoteSql(x.Name)}, {QuoteSql(x.Email)}, {QuoteSql(x.City)}, {QuoteSql(x.Company)}, '{FormatDay(x.Birthday)}')"));

		return $"""
			/*
			 * {faker.Lorem.Sentence()}
			 * {faker.Lorem.Sentence()}
			 */

			-- #region Schema
			CREATE TABLE Users
			(
			    Id INT PRIMARY KEY,
			    Name NVARCHAR(100) NOT NULL,
			    Email NVARCHAR(200) NOT NULL,
			    City NVARCHAR(100),
			    Company NVARCHAR(200),
			    Birthday DATE
			);
			-- #endregion

			-- #region Data
			INSERT INTO Users (Id, Name, Email, City, Company, Birthday)
			VALUES
			{rows};
			-- #endregion

			-- {faker.Lorem.Sentence()}
			-- {faker.Lorem.Sentence()}
			SELECT City, COUNT(*) AS UserCount
			FROM Users
			GROUP BY City
			ORDER BY UserCount DESC;

			""".ReplaceLineEndings();
	}

	/// <summary>
	/// Returns a plain text of several paragraphs.
	/// </summary>
	public static string CreateText(Faker faker) => faker.Lorem.Paragraphs(TextParagraphCount, Environment.NewLine + Environment.NewLine);

	/// <summary>
	/// Returns an XML text of a catalog of products, with a comment of several lines at its head.
	/// </summary>
	public static string CreateXml(Faker faker)
	{
		XDocument document = new(
			new XDeclaration("1.0", "utf-8", null),
			new XComment(CreateCommentText(faker, depth: 0)),
			new XElement(
				"catalog",
				new XAttribute("updated", FormatDate(faker.Date.Past(1, DateReference))),
				Enumerable
					.Range(1, ProductCount)
					.Select(id => CreateProduct(faker, id))));

		// The text of a document leaves its declaration out.
		return $"{document.Declaration}{Environment.NewLine}{document}";
	}

	/// <summary>
	/// Returns a YAML text of the settings of an application with its users, whose list stands in a region.
	/// </summary>
	public static string CreateYaml(Faker faker)
	{
		StringBuilder builder = new();

		builder.AppendLine($"# {faker.Lorem.Sentence()}");

		builder.AppendLine($"# {faker.Lorem.Sentence()}");

		builder.AppendLine();

		foreach ((string key, JsonNode? value) in CreateSettings(faker))
		{
			bool isRegion = value is JsonArray;

			if (isRegion)
			{
				builder.AppendLine($"# region {key}");
			}

			AppendYamlMember(
				builder,
				string.Empty,
				key,
				value,
				depth: 0);

			if (isRegion)
			{
				builder.AppendLine("# endregion");
			}
		}

		return builder.ToString();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Writes a member of an object as YAML after the start of its line, which holds its indentation or the dash of a list
	/// item.
	/// </summary>
	private static void AppendYamlMember(
		StringBuilder builder,
		string start,
		string key,
		JsonNode? value,
		int depth)
	{
		builder
			.Append(start)
			.Append(key)
			.Append(':');

		if (value is JsonObject members)
		{
			builder.AppendLine();

			AppendYamlObject(
				builder,
				members,
				depth + 1,
				isListItem: false);
		}
		else if (value is JsonArray items)
		{
			builder.AppendLine();

			foreach (JsonNode? item in items)
			{
				if (item is JsonObject itemMembers)
				{
					AppendYamlObject(
						builder,
						itemMembers,
						depth + 2,
						isListItem: true);
				}
				else
				{
					builder
						.Append(Indent(depth + 1))
						.Append("- ")
						.AppendLine(FormatYamlScalar(item));
				}
			}
		}
		else
		{
			builder
				.Append(' ')
				.AppendLine(FormatYamlScalar(value));
		}
	}

	/// <summary>
	/// Writes the members of an object as YAML at a depth; the first member of a list item follows the dash of the item.
	/// </summary>
	private static void AppendYamlObject(
		StringBuilder builder,
		JsonObject members,
		int depth,
		bool isListItem)
	{
		bool isFirst = true;

		foreach ((string key, JsonNode? value) in members)
		{
			string start = isListItem && isFirst ? $"{Indent(depth - 1)}- " : Indent(depth);

			AppendYamlMember(
				builder,
				start,
				key,
				value,
				depth);

			isFirst = false;
		}
	}

	/// <summary>
	/// Returns the text of a comment of several lines for a node at a depth, its lines one level deeper than the node.
	/// </summary>
	private static string CreateCommentText(Faker faker, int depth)
	{
		string indent = Indent(depth + 1);

		return $"{Environment.NewLine}{indent}{faker.Lorem.Sentences(2, Environment.NewLine + indent)}{Environment.NewLine}{Indent(depth)}";
	}

	/// <summary>
	/// Returns a rule of CSS for a selector.
	/// </summary>
	private static string CreateCssRule(Faker faker, string selector)
	{
		return $$"""
			{{selector}} {
			  color: {{faker.Internet.Color()}};
			  background-color: {{faker.Internet.Color()}};
			  margin: {{faker.Random.Int(0, 24)}}px {{faker.Random.Int(0, 24)}}px;
			  padding: {{faker.Random.Int(0, 16)}}px;
			  border: 1px solid {{faker.Internet.Color()}};
			}
			""";
	}

	/// <summary>
	/// Returns a heading of a few words that starts with a capital letter.
	/// </summary>
	private static string CreateHeading(Faker faker)
	{
		string phrase = faker.Company.Bs();

		return char.ToUpperInvariant(phrase[0]) + phrase[1..];
	}

	/// <summary>
	/// Returns an HTML table of people with its header.
	/// </summary>
	private static XElement CreateHtmlTable(Faker faker)
	{
		return new XElement(
			"table",
			new XElement(
				"thead",
				new XElement(
					"tr",
					new XElement("th", "Name"),
					new XElement("th", "Email"),
					new XElement("th", "City"))),
			new XElement(
				"tbody",
				CreatePeople(faker, TableRowCount).Select(static x => new XElement(
					"tr",
					new XElement("td", x.Name),
					new XElement("td", x.Email),
					new XElement("td", x.City)))));
	}

	/// <summary>
	/// Returns made-up people.
	/// </summary>
	private static (string Name, string Email, string City, string Company, DateTime Birthday)[] CreatePeople(Faker faker, int count)
	{
		return [.. Enumerable
			.Range(0, count)
			.Select(_ =>
			{
				string firstName = faker.Name.FirstName();

				string lastName = faker.Name.LastName();

				return (
					$"{firstName} {lastName}",
					faker.Internet.Email(firstName, lastName),
					faker.Address.City(),
					faker.Company.CompanyName(),
					faker.Date.Past(60, DateReference.AddYears(-18)));
			})];
	}

	/// <summary>
	/// Returns a product of the catalog.
	/// </summary>
	private static XElement CreateProduct(Faker faker, int id)
	{
		return new XElement(
			"product",
			new XAttribute("id", id),
			new XAttribute("available", faker.Random.Bool()),
			new XElement("name", faker.Commerce.ProductName()),
			new XElement("department", faker.Commerce.Department()),
			new XElement(
				"price",
				new XAttribute("currency", faker.Finance.Currency().Code),
				Math.Round(faker.Random.Decimal(1, 1000), 2)),
			new XElement("description", faker.Commerce.ProductDescription()),
			new XElement(
				"tags",
				faker
					.Commerce
					.Categories(faker.Random.Int(1, 3))
					.Select(static x => new XElement("tag", x))));
	}

	/// <summary>
	/// Returns the settings of an application with its users.
	/// </summary>
	private static JsonObject CreateSettings(Faker faker)
	{
		return new JsonObject
		{
			["application"] = new JsonObject
			{
				["name"] = faker.Company.CompanyName(),
				["version"] = faker.System.Semver(),
				["website"] = faker.Internet.Url(),
				["description"] = faker.Company.CatchPhrase(),
				["releasedAt"] = FormatDate(faker.Date.Past(2, DateReference)),
				["features"] = new JsonArray([.. Enumerable
					.Range(0, faker.Random.Int(3, 5))
					.Select(_ => new JsonObject
					{
						["name"] = faker.Hacker.Noun(),
						["enabled"] = faker.Random.Bool()
					})])
			},
			["logging"] = new JsonObject
			{
				["level"] = faker.PickRandom("Debug", "Information", "Warning", "Error"),
				["file"] = faker.System.FilePath(),
				["retainedDays"] = faker.Random.Int(7, 90)
			},
			["users"] = new JsonArray([.. Enumerable
				.Range(1, UserCount)
				.Select(id => CreateUser(faker, id))])
		};
	}

	/// <summary>
	/// Returns a user of the settings.
	/// </summary>
	private static JsonObject CreateUser(Faker faker, int id)
	{
		string firstName = faker.Name.FirstName();

		string lastName = faker.Name.LastName();

		return new JsonObject
		{
			["id"] = id,
			["name"] = $"{firstName} {lastName}",
			["email"] = faker.Internet.Email(firstName, lastName),
			["title"] = faker.Name.JobTitle(),
			["isActive"] = faker.Random.Bool(),
			["address"] = new JsonObject
			{
				["street"] = faker.Address.StreetAddress(),
				["city"] = faker.Address.City(),
				["zipCode"] = faker.Address.ZipCode(),
				["country"] = faker.Address.Country()
			},
			["lastSeenAt"] = FormatDate(faker.Date.Past(1, DateReference)),
			["note"] = faker.Random.Bool(NoteShare) ? faker.Lorem.Sentence() : null
		};
	}

	/// <summary>
	/// Returns a moment in the format of the texts.
	/// </summary>
	private static string FormatDate(DateTime value) => value.ToString(DateFormat, CultureInfo.InvariantCulture);

	/// <summary>
	/// Returns a day in the format of the texts.
	/// </summary>
	private static string FormatDay(DateTime value) => value.ToString(DayFormat, CultureInfo.InvariantCulture);

	/// <summary>
	/// Returns a scalar of YAML: a text as it is when YAML reads it back as the same text, otherwise the value as in JSON,
	/// which YAML reads alike.
	/// </summary>
	private static string FormatYamlScalar(JsonNode? value)
	{
		if (value is JsonValue scalar
			&& scalar.TryGetValue(out string? text)
			&& PlainYamlRegex().IsMatch(text))
		{
			return text;
		}

		return value?.ToJsonString(JsonOptions) ?? "null";
	}

	/// <summary>
	/// Returns the indentation of a depth in the texts.
	/// </summary>
	private static string Indent(int depth) => new(' ', depth * 2);

	/// <summary>
	/// Returns a text with every line indented.
	/// </summary>
	private static string IndentLines(string text, string indent) => indent + text.ReplaceLineEndings(Environment.NewLine + indent);

	/// <summary>
	/// Matches a text that YAML reads as a plain text: it starts with a letter, holds none of the marks of YAML and is not
	/// a word that YAML reads as another type.
	/// </summary>
	[GeneratedRegex(@"^(?!(?i:true|false|null|yes|no|on|off|y|n)$)[A-Za-z][\w .,'@/-]*(?<! )$")]
	private static partial Regex PlainYamlRegex();

	/// <summary>
	/// Returns a value of comma-separated values, in quotes when it holds a separator, a quote or a line break.
	/// </summary>
	private static string QuoteCsv(string value)
	{
		return value.AsSpan().ContainsAny(CsvQuotedCharacters)
			? $"\"{value.Replace("\"", "\"\"")}\""
			: value;
	}

	/// <summary>
	/// Returns a string of JSON.
	/// </summary>
	private static string QuoteJson(string value) => JsonSerializer.Serialize(value, JsonOptions);

	/// <summary>
	/// Returns a text literal of SQL.
	/// </summary>
	private static string QuoteSql(string value) => $"'{value.Replace("'", "''")}'";
	#endregion
}
