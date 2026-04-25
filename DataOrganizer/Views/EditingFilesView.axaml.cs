using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using DataOrganizer.ViewModels;
using Shared.Extensions;
using System;

namespace DataOrganizer.Views;

/// <summary>
/// <see cref="UserControl" /> for editing files.
/// </summary>
internal sealed partial class EditingFilesView : UserControl
{
	#region Constructors
	public EditingFilesView()
	{
		InitializeComponent();

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		DataContext = Ioc
			.Default
			.GetRequiredService<EditingFilesViewModel>();
	}
	#endregion
}