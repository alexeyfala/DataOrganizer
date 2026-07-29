using System.Globalization;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Applies the UI culture used for localized resources.
/// </summary>
public interface IUiCultureService
{
	#region Properties
	/// <summary>
	/// Currently applied UI culture.
	/// </summary>
	CultureInfo Current { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Applies the culture with the specified <paramref name="language" /> name.
	/// </summary>
	void Apply(string language);
	#endregion
}
