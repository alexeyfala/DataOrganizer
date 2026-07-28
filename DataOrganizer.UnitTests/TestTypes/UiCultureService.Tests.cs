using AwesomeAssertions;
using DataOrganizer.Services;
using Shared.Properties;
using System.Globalization;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(UiCultureService)}"" type")]
internal class UiCultureServiceTests
{
	#region Data
	/// <inheritdoc cref="_resourceCulture" />
	private CultureInfo _currentThreadCulture = CultureInfo.CurrentUICulture;

	/// <inheritdoc cref="_resourceCulture" />
	private CultureInfo? _defaultThreadCulture;

	/// <summary>
	/// Culture state captured before a test to be restored afterwards.
	/// </summary>
	private CultureInfo? _resourceCulture;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="UiCultureService.Apply" />: exposes the applied culture through
	/// <see cref="UiCultureService.Current" />.
	/// </summary>
	[Test]
	public void Apply_Exposes_Applied_Culture()
	{
		// Arrange
		UiCultureService sut = new();

		// Act
		sut.Apply("ru-ru");

		// Assert
		sut.Current
			.Name
			.Should()
			.Be("ru-RU");
	}

	/// <summary>
	/// <see cref="UiCultureService.Apply" />: applies the culture to threads started later.
	/// </summary>
	[Test]
	public void Apply_Sets_Culture_For_New_Threads()
	{
		// Arrange
		UiCultureService sut = new();

		// Act
		sut.Apply("ru-ru");

		// Assert
		CultureInfo.DefaultThreadCurrentUICulture
			.Should()
			.BeSameAs(sut.Current);
	}

	/// <summary>
	/// <see cref="UiCultureService.Apply" />: switches localized resources to English.
	/// </summary>
	[Test]
	public void Apply_Switches_Resources_To_English()
	{
		// Arrange
		UiCultureService sut = new();

		// Act
		sut.Apply("en-us");

		// Assert
		Strings.Search
			.Should()
			.Be("Search");
	}

	/// <summary>
	/// <see cref="UiCultureService.Apply" />: switches localized resources to Russian.
	/// </summary>
	[Test]
	public void Apply_Switches_Resources_To_Russian()
	{
		// Arrange
		UiCultureService sut = new();

		// Act
		sut.Apply("ru-ru");

		// Assert
		Strings.Search
			.Should()
			.Be("Поиск");
	}

	/// <summary>
	/// <see cref="UiCultureService.Current" />: falls back to the ambient UI culture until applied.
	/// </summary>
	[Test]
	public void Current_Falls_Back_To_Ambient_Culture()
	{
		// Arrange & Act
		UiCultureService sut = new();

		// Assert
		sut.Current
			.Should()
			.BeSameAs(CultureInfo.CurrentUICulture);
	}

	[SetUp]
	public void SetUp()
	{
		_resourceCulture = Strings.Culture;

		_defaultThreadCulture = CultureInfo.DefaultThreadCurrentUICulture;

		_currentThreadCulture = CultureInfo.CurrentUICulture;
	}

	[TearDown]
	public void TearDown()
	{
		Strings.Culture = _resourceCulture;

		CultureInfo.DefaultThreadCurrentUICulture = _defaultThreadCulture;

		CultureInfo.CurrentUICulture = _currentThreadCulture;
	}
	#endregion
}
