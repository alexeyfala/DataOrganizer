using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DataOrganizer.Dto;
using DataOrganizer.Interfaces;
using Material.Styles.Controls;
using Material.Styles.Models;
using System;
using System.Linq;

namespace DataOrganizer.Services;

public sealed class SnackbarPresenter : ISnackbarPresenter
{
	#region Data
	/// <summary>
	/// The host handed over by the window that carries it; <c>null</c> while no such window is open.
	/// </summary>
	private SnackbarHost? _host;

	/// <summary>
	/// The visual the shown message occupies; <c>null</c> until it is looked up.
	/// </summary>
	private Control? _messageVisual;

	/// <summary>
	/// The message handed over to the host; <c>null</c> while none is shown.
	/// </summary>
	private SnackbarModel? _posted;
	#endregion

	#region Properties
	/// <inheritdoc />
	public bool CanShow => _host is not null;

	/// <inheritdoc />
	/// <remarks>
	/// Only the message counts, not the window content the host wraps.
	/// </remarks>
	public bool IsPointerOverMessage => GetMessageVisual()?.IsPointerOver == true;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void AttachHost(SnackbarHost host) => _host = host;

	/// <inheritdoc />
	public void DetachHost(SnackbarHost host)
	{
		// A host of a window that has already been replaced says nothing about the current one.
		if (!ReferenceEquals(_host, host))
		{
			return;
		}

		_host = null;

		_messageVisual = null;

		_posted = null;
	}

	/// <inheritdoc />
	public void Post(SnackbarContent content)
	{
		if (_host is not { } host)
		{
			return;
		}

		// The level travels with the content so that the snackbar template colours its own text.
		// Zero duration leaves the message on the screen until it is removed here.
		_posted = new(content, TimeSpan.Zero);

		_messageVisual = null;

		SnackbarHost.Post(
			_posted,
			host.HostName,
			DispatcherPriority.Normal);
	}

	/// <inheritdoc />
	public void Remove()
	{
		if (_posted is { } posted && _host is { } host)
		{
			SnackbarHost.Remove(
				posted,
				host.HostName,
				DispatcherPriority.Normal);
		}

		_messageVisual = null;

		_posted = null;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the visual of the shown message, found by the model it carries and kept until the message goes.
	/// </summary>
	private Control? GetMessageVisual()
	{
		if (_messageVisual is not null)
		{
			return _messageVisual;
		}

		if (_host is not { } host || _posted is not { } posted)
		{
			return null;
		}

		_messageVisual = host
			.GetVisualDescendants()
			.OfType<Control>()
			.FirstOrDefault(x => ReferenceEquals(x.DataContext, posted));

		return _messageVisual;
	}
	#endregion
}
