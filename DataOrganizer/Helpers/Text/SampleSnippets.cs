using Bogus;
using System;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Short texts made up for copying with a hotkey: contact and bank details, and the secrets of a household.
/// </summary>
internal static class SampleSnippets
{
	#region Data
	/// <summary>
	/// Pattern of the PIN of a card, where each hash stands for a digit.
	/// </summary>
	private const string CardPinFormat = "####";

	/// <summary>
	/// Pattern of the code of a door, where each hash stands for a digit.
	/// </summary>
	private const string DoorCodeFormat = "######";

	/// <summary>
	/// Number of characters of the password of a router.
	/// </summary>
	private const int RouterPasswordLength = 16;

	/// <summary>
	/// Number of characters of the password of a wireless network.
	/// </summary>
	private const int WifiPasswordLength = 12;
	#endregion

	#region Methods
	/// <summary>
	/// Returns a postal address in three lines.
	/// </summary>
	public static string CreateAddress(Faker faker) => string.Join(
		Environment.NewLine,
		faker.Address.StreetAddress(),
		$"{faker.Address.ZipCode()} {faker.Address.City()}",
		faker.Address.Country());

	/// <summary>
	/// Returns the details of a bank account: its IBAN and the BIC of its bank.
	/// </summary>
	public static string CreateBankDetails(Faker faker) => string.Join(
		Environment.NewLine,
		$"IBAN: {faker.Finance.Iban(formatted: true)}",
		$"BIC: {faker.Finance.Bic()}");

	/// <summary>
	/// Returns the PIN of a card.
	/// </summary>
	public static string CreateCardPin(Faker faker) => faker.Random.Replace(CardPinFormat);

	/// <summary>
	/// Returns the code of a door.
	/// </summary>
	public static string CreateDoorCode(Faker faker) => faker.Random.Replace(DoorCodeFormat);

	/// <summary>
	/// Returns an email address.
	/// </summary>
	public static string CreateEmail(Faker faker) => faker.Internet.Email();

	/// <summary>
	/// Returns a phone number.
	/// </summary>
	public static string CreatePhoneNumber(Faker faker) => faker.Phone.PhoneNumber();

	/// <summary>
	/// Returns the password of the administrator of a router.
	/// </summary>
	public static string CreateRouterPassword(Faker faker) => faker.Internet.Password(RouterPasswordLength);

	/// <summary>
	/// Returns the signature of a letter: a name, a job at a company, a phone number and an email address.
	/// </summary>
	public static string CreateSignature(Faker faker)
	{
		string firstName = faker.Name.FirstName();

		string lastName = faker.Name.LastName();

		return string.Join(
			Environment.NewLine,
			$"{firstName} {lastName}",
			$"{faker.Name.JobTitle()}, {faker.Company.CompanyName()}",
			faker.Phone.PhoneNumber(),
			faker.Internet.Email(firstName, lastName));
	}

	/// <summary>
	/// Returns the name of a wireless network and its password.
	/// </summary>
	public static string CreateWifi(Faker faker) => string.Join(
		Environment.NewLine,
		$"Network: {faker.Internet.DomainWord()}",
		$"Password: {faker.Internet.Password(WifiPasswordLength)}");
	#endregion
}
