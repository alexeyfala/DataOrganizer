using Setup.CustomActions.Models;
using System.Collections.Generic;
using System.Linq;

namespace Setup.CustomActions.Helpers;

/// <summary>
/// The list of product codes in the value of a property, as FindRelatedProducts writes it.
/// </summary>
public static class ProductCodeList
{
	#region Data
	/// <summary>
	/// Separator of the product codes in the value.
	/// </summary>
	private const string Separator = ";";
	#endregion

	#region Methods
	/// <summary>
	/// Returns the codes of the products joined by ';', or an empty string for none.
	/// </summary>
	public static string Join(IEnumerable<RelatedProduct> products)
	{
		return string.Join(
			Separator,
			products.Select(static x => x.ProductCode));
	}
	#endregion
}
