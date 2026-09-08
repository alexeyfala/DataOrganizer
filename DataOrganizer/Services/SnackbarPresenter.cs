using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using Material.Styles.Controls;
using Material.Styles.Models;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace DataOrganizer.Services;

public sealed class SnackbarPresenter : ISnackbarPresenter
{
	#region Data
	/// <summary>
	/// Name Material gives to the part that holds the shown messages.
	/// </summary>
	private const string MessagesPart = "PART_SnackbarHostItemsContainer";

	/// <summary>
	/// The message handed over to the host; <c>null</c> while none is shown.
	/// </summary>
	private SnackbarModel? _posted;
	#endregion

	#region Properties
	/// <inheritdoc />
	/// <remarks>
	/// Read from the registry of the hosts, which Material keeps to itself.
	/// </remarks>
	public bool IsHostLoaded => GetHost() is not null;

	/// <inheritdoc />
	/// <remarks>
	/// Only the cards of the messages count, not the window content the host wraps.
	/// </remarks>
	public bool IsPointerOverMessage => GetMessages() is { } messages && messages
		.GetVisualDescendants()
		.OfType<Card>()
		.Any(x => x.IsPointerOver);
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Post(SnackbarContent content)
	{
		// The level travels with the content so that the snackbar template colours its own text.
		// Zero duration leaves the message on the screen until it is removed here.
		_posted = new(content, TimeSpan.Zero);

		SnackbarHost.Post(
			_posted,
			null,
			DispatcherPriority.Normal);
	}

	/// <inheritdoc />
	public void Remove()
	{
		if (_posted is { } posted && IsHostLoaded)
		{
			SnackbarHost.Remove(
				posted,
				null,
				DispatcherPriority.Normal);
		}

		_posted = null;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the host messages are shown in, the way Material picks it for a message without a host name.
	/// </summary>
	private static SnackbarHost? GetHost()
	{
		return typeof(SnackbarHost)
			.GetField("SnackbarHostDictionary", BindingFlags.NonPublic | BindingFlags.Static)
			?.GetValue(null) is IDictionary registered
			? registered
				.Values
				.OfType<SnackbarHost>()
				.FirstOrDefault()
			: null;
	}

	/// <summary>
	/// Returns the part of the host the shown messages live in.
	/// </summary>
	private static ItemsControl? GetMessages()
	{
		return GetHost()?
			.GetVisualDescendants()
			.OfType<ItemsControl>()
			.FirstOrDefault(x => x.Name == MessagesPart);
	}
	#endregion
}
