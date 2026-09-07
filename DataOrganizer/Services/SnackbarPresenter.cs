using Avalonia.Threading;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using Material.Styles.Controls;
using Material.Styles.Models;
using System;
using System.Collections;
using System.Reflection;

namespace DataOrganizer.Services;

public sealed class SnackbarPresenter : ISnackbarPresenter
{
	#region Properties
	/// <inheritdoc />
	/// <remarks>
	/// Read from the registry of the hosts, which Material keeps to itself.
	/// </remarks>
	public bool IsHostLoaded => typeof(SnackbarHost)
		.GetField("SnackbarHostDictionary", BindingFlags.NonPublic | BindingFlags.Static)
		?.GetValue(null) is IDictionary registered && registered.Count > 0;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Post(SnackbarContent content, TimeSpan duration)
	{
		// The level travels with the content so that the snackbar template colours its own text.
		SnackbarHost.Post(
			new SnackbarModel(content, duration),
			null,
			DispatcherPriority.Normal);
	}
	#endregion
}
