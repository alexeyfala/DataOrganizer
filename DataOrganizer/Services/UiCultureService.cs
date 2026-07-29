using DataOrganizer.Interfaces;
using Shared.Properties;
using System.Globalization;

namespace DataOrganizer.Services;

public sealed class UiCultureService : IUiCultureService
{
	#region Properties
	/// <inheritdoc />
	public CultureInfo Current { get; private set; } = CultureInfo.CurrentUICulture;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Apply(string language)
	{
		Current = CultureInfo.GetCultureInfo(language);

		// The only writer of the resource culture; must run before any XAML is parsed.
		Strings.Culture = Current;

		// The current thread keeps the culture it was started with, hence both assignments.
		CultureInfo.CurrentUICulture = Current;

		CultureInfo.DefaultThreadCurrentUICulture = Current;
	}
	#endregion
}
