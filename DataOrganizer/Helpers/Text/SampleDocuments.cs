using Bogus;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Texts of data formats made up for trying the application, with values from a generator.
/// </summary>
internal static class SampleDocuments
{
	#region Data
	/// <summary>
	/// Format of a moment in the texts.
	/// </summary>
	private const string DateFormat = "yyyy-MM-ddTHH:mm:ss";

	/// <summary>
	/// Share of the users with a note.
	/// </summary>
	private const float NoteShare = 0.7f;

	/// <summary>
	/// Number of products in the catalog.
	/// </summary>
	private const int ProductCount = 12;

	/// <summary>
	/// Number of users in the settings.
	/// </summary>
	private const int UserCount = 8;

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
	#endregion

	#region Methods
	/// <summary>
	/// Returns a JSON text of the settings of an application with its users.
	/// </summary>
	public static string CreateJson(Faker faker)
	{
		JsonObject settings = new()
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

		return settings.ToJsonString(JsonOptions);
	}

	/// <summary>
	/// Returns an XML text of a catalog of products, with a comment of several lines at its head.
	/// </summary>
	public static string CreateXml(Faker faker)
	{
		string indent = new(' ', 2);

		string lines = faker.Lorem.Sentences(3, Environment.NewLine + indent);

		XDocument document = new(
			new XDeclaration("1.0", "utf-8", null),
			new XComment($"{Environment.NewLine}{indent}{lines}{Environment.NewLine}"),
			new XElement(
				"catalog",
				new XAttribute("updated", FormatDate(faker.Date.Past(1, DateReference))),
				Enumerable
					.Range(1, ProductCount)
					.Select(id => CreateProduct(faker, id))));

		// The text of a document leaves its declaration out.
		return $"{document.Declaration}{Environment.NewLine}{document}";
	}
	#endregion

	#region Helpers
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
	#endregion
}
