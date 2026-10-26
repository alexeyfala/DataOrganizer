using Avalonia;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Dto.Notifications;
using DataOrganizer.Helpers.Views;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Exchange;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Text;
using DataOrganizer.Services.Documents;
using DataOrganizer.Services.Exchange;
using DataOrganizer.Services.Execution;
using DataOrganizer.Services.Runtime;
using DataOrganizer.Services.Text;
using DataOrganizer.UnitTests.Guards;
using DataOrganizer.ViewModels.Windows;
using Entities.Models;
using Hierarchy.Dto;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Repository.Services.Database;
using Serilog;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TestSupport.Models;

namespace DataOrganizer.UnitTests;

[TestFixture(Description = $@"Tests of ""{nameof(App)}"" type")]
internal partial class AppTests
{
	#region Data
	/// <summary>
	/// Assemblies whose types the application registers itself.
	/// </summary>
	private static readonly Assembly[] OwnAssemblies =
	[
		typeof(App).Assembly,
		typeof(DbAccess).Assembly,
		typeof(FileSystem).Assembly
	];
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="App.RegisterServices" />: the view model of the console, which the view factory builds with no
	/// registration of its own, finds every dependency, the text of its log included.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Builds_The_Console_View_Model()
	{
		// Arrange
		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, []);

		// The environment of the test keeps the folders of the application untouched.
		services.AddSingleton(Substitute.For<IAppEnvironment>());

		using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		using ConsoleViewModel result = ActivatorUtilities.CreateInstance<ConsoleViewModel>(provider);

		// Assert
		result.Document
			.Should()
			.BeOfType<AvaloniaLogText>();
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the logger that also writes into the console is built, though the console and
	/// the services it uses log through that logger.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Builds_The_Logger_That_Writes_Into_The_Console()
	{
		// Arrange
		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, [CommandLineOptions.ConsoleArg]);

		// The environment of the test keeps the folders of the application untouched.
		services.AddSingleton(Substitute.For<IAppEnvironment>());

		using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		Task<ILogger> task = Task.Run(() => provider.GetRequiredService<ILogger>());

		// Assert
		task.Wait(TimeSpan.FromSeconds(10))
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the mapper of the application knows the entity rules,
	/// whichever service resolves it first.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Gives_A_Mapper_With_The_Entity_Rules()
	{
		// Arrange
		FileEntity file = EntityFactory.CreateFile();

		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, []);

		using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		ExplorerItemDtoBase result = provider
			.GetRequiredService<IMapper>()
			.Map<ExplorerItemBase, ExplorerItemDtoBase>(file);

		// Assert
		result
			.Should()
			.BeOfType<FileDto>();
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: every view model the view factory builds, which needs no registration of its
	/// own, finds the dependencies of its constructor, so a missing one is named here instead of when its view opens.
	/// </summary>
	[AvaloniaTest]
	[Guard]
	public void RegisterServices_Gives_Every_View_Model_Of_The_View_Factory_Its_Dependencies()
	{
		// Arrange
		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, []);

		Type[] viewModels =
		[
			.. RepositoryFiles
				.EnumerateProjectCode()
				.SelectMany(static x => CreatedViewModelRegex().Matches(x.Text))
				.Select(static x => x.Groups["name"].Value)
				.Distinct(StringComparer.Ordinal)
				.Select(static x => typeof(App).Assembly.GetTypes().Single(type => type.Name == x))
		];

		// Act
		string[] unsatisfied =
		[
			.. viewModels
				.Select(x => DescribeMissingDependencies(services, x))
				.Where(static x => x.Length > 0)
		];

		// Assert
		viewModels
			.Should()
			.NotBeEmpty();

		unsatisfied
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the view models get the registry of grammars the editor controls use, so
	/// the grammars are read once.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Gives_The_Syntax_Registry_Of_The_Editor_Controls()
	{
		// Arrange
		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, []);

		using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		ISyntaxRegistry result = provider.GetRequiredService<ISyntaxRegistry>();

		// Assert
		result
			.Should()
			.BeSameAs(SyntaxRegistry.Instance);
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the program that opens a file is chosen the way of the operating system the
	/// application runs on.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Picks_The_App_Resolver_Of_The_Operating_System()
	{
		// Arrange
		ServiceCollection services = [];

		// Act
		((App)Application.Current!).RegisterServices(services, []);

		// Assert
		services
			.Should()
			.ContainSingle(x => x.ServiceType == typeof(IAppResolver))
			.Which
			.ImplementationType
			.Should()
			.Be(OperatingSystem.IsWindows() ? typeof(WindowsAppResolver) : typeof(SystemAppResolver));
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: every registered type can be built from the registrations
	/// alone, so a missing one is named here instead of failing on the first resolve at startup.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Registers_Every_Dependency()
	{
		// Arrange
		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, []);

		// Act
		string[] unsatisfied =
		[
			.. services
				.Select(GetImplementationType)
				.Where(IsOwnType)
				.Distinct()
				.Select(x => DescribeMissingDependencies(services, x!))
				.Where(x => x.Length > 0)
		];

		// Assert
		unsatisfied
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the exchange formats are registered in the order the file picker lists them.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Registers_The_Exchange_Formats_In_The_Order_Of_The_File_Picker()
	{
		// Arrange
		ServiceCollection services = [];

		// Act
		((App)Application.Current!).RegisterServices(services, []);

		// Assert
		services.Where(static x => x.ServiceType == typeof(IExchangeFormat)).Select(static x => x.ImplementationType)
			.Should()
			.Equal(typeof(JsonExchangeFormat), typeof(XmlExchangeFormat), typeof(SqliteExchangeFormat));
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the services the markup resolves are registered, though no constructor names
	/// them.
	/// </summary>
	[AvaloniaTest]
	[TestCase(typeof(IClipboardTextWriter))]
	[TestCase(typeof(IFilePreviewReader))]
	[TestCase(typeof(INoteReader))]
	[TestCase(typeof(ISnackbarPresenter))]
	public void RegisterServices_Registers_The_Services_The_Markup_Resolves(Type serviceType)
	{
		// Arrange
		ServiceCollection services = [];

		// Act
		((App)Application.Current!).RegisterServices(services, []);

		// Assert
		services
			.Should()
			.Contain(x => x.ServiceType == serviceType);
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the clipboard log is one for the whole application, so the entries the user
	/// works with are the ones the background tracking captures and the stored log merges into.
	/// </summary>
	[AvaloniaTest]
	public async Task RegisterServices_Shares_One_Clipboard_Log()
	{
		// Arrange
		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, []);

		await using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		object[] roles =
		[
			provider.GetRequiredService<IClipboardLogMerger>(),
			provider.GetRequiredService<IClipboardLogRunner>(),
			provider.GetRequiredService<IClipboardLogService>(),
			provider.GetRequiredService<IClipboardLogMerger>()
		];

		// Assert
		roles
			.Should()
			.AllSatisfy(x => x.Should().BeSameAs(roles[0]));
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the copy history is one for the whole application, so every window and the
	/// hotkeys add to the same list, and the list read and stored is that one.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Shares_One_Copy_History()
	{
		// Arrange
		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, []);

		using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		object[] roles =
		[
			provider.GetRequiredService<ICopyHistory>(),
			provider.GetRequiredService<ICopyHistoryRecorder>(),
			provider.GetRequiredService<ICopyHistoryStore>(),
			provider.GetRequiredService<ICopyHistory>()
		];

		// Assert
		roles
			.Should()
			.AllSatisfy(x => x.Should().BeSameAs(roles[0]));
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the executing files have one owner for the whole application, so every main
	/// window lists the same files.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Shares_One_Owner_Of_The_Executing_Files()
	{
		// Arrange
		ServiceCollection services = [];

		// Act
		((App)Application.Current!).RegisterServices(services, []);

		// Assert
		services
			.Should()
			.ContainSingle(x => x.ServiceType == typeof(IExecutingFiles))
			.Which
			.Lifetime
			.Should()
			.Be(ServiceLifetime.Singleton);
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the settings view, the dialog and the editor share one settings session, so the
	/// edits the view makes are the ones the editor saves.
	/// </summary>
	[AvaloniaTest]
	public async Task RegisterServices_Shares_One_Settings_Session()
	{
		// Arrange
		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, []);

		await using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		object[] roles =
		[
			provider.GetRequiredService<ISettingsCloseGuard>(),
			provider.GetRequiredService<ISettingsCommit>(),
			provider.GetRequiredService<ISettingsDraft>(),
			provider.GetRequiredService<ISettingsDraft>()
		];

		// Assert
		roles
			.Should()
			.AllSatisfy(x => x.Should().BeSameAs(roles[0]));
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the notifications post to the presenter that the snackbar host of the window
	/// is handed to.
	/// </summary>
	[AvaloniaTest]
	[TestCase(SnackbarHostIdentifiers.Main)]
	[TestCase(SnackbarHostIdentifiers.Notepad)]
	public void RegisterServices_Shares_One_Snackbar_Presenter_Per_Host(string hostIdentifier)
	{
		// Arrange
		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, []);

		using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		ISnackbarPresenter attached = provider.GetRequiredKeyedService<ISnackbarPresenter>(hostIdentifier);

		IMessagePresenter<SnackbarContent> posted = provider.GetRequiredKeyedService<IMessagePresenter<SnackbarContent>>(
			hostIdentifier);

		// Assert
		posted
			.Should()
			.BeSameAs(attached);
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the splits of the editors have one store for the whole application, so a file
	/// opened again gets its split back.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Shares_One_Store_Of_Editor_Splits()
	{
		// Arrange
		ServiceCollection services = [];

		// Act
		((App)Application.Current!).RegisterServices(services, []);

		// Assert
		services
			.Should()
			.ContainSingle(x => x.ServiceType == typeof(IEditorSplitStore))
			.Which
			.Lifetime
			.Should()
			.Be(ServiceLifetime.Singleton);
	}

	/// <summary>
	/// <see cref="App.RegisterServices" />: the tree with its expanded and selected objects, the editor tabs and the
	/// read-only mode live in one work session for the whole application, so every main window sees the same.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Shares_One_Work_Session()
	{
		// Arrange
		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, []);

		using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		object[] roles =
		[
			provider.GetRequiredService<ISessionEditorTabs>(),
			provider.GetRequiredService<ISessionHierarchy>(),
			provider.GetRequiredService<ISessionReadOnlyMode>(),
			provider.GetRequiredService<ISessionTreeState>()
		];

		// Assert
		roles
			.Should()
			.AllSatisfy(x => x.Should().BeSameAs(roles[0]));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when a parameter can be obtained from the registrations.
	/// </summary>
	private static bool CanResolve(ServiceCollection services, ParameterInfo parameter)
	{
		if (parameter.HasDefaultValue || parameter.ParameterType == typeof(IServiceProvider))
		{
			return true;
		}

		Type type = parameter.ParameterType;

		if (parameter.GetCustomAttribute<FromKeyedServicesAttribute>() is { } keyed)
		{
			return services.Any(x => x.IsKeyedService
				&& x.ServiceType == type
				&& Equals(x.ServiceKey, keyed.Key));
		}

		if (services.Any(x => !x.IsKeyedService && x.ServiceType == type))
		{
			return true;
		}

		if (!type.IsGenericType)
		{
			return false;
		}

		Type definition = type.GetGenericTypeDefinition();

		// A sequence is satisfied by any number of registrations, an open generic by a closed request.
		return definition == typeof(IEnumerable<>)
			|| services.Any(x => !x.IsKeyedService && x.ServiceType == definition);
	}

	/// <summary>
	/// A view model the code asks the view factory for, by the name of its type.
	/// </summary>
	[GeneratedRegex(@"CreateViewModel<(?<name>\w+ViewModel)>")]
	private static partial Regex CreatedViewModelRegex();

	/// <summary>
	/// Names the dependencies no constructor of a type can obtain; empty when the type can be built.
	/// </summary>
	private static string DescribeMissingDependencies(ServiceCollection services, Type implementationType)
	{
		ConstructorInfo[] constructors = implementationType.GetConstructors();

		if (constructors.Length == 0)
		{
			return string.Empty;
		}

		List<string> missingPerConstructor = [];

		foreach (ConstructorInfo constructor in constructors)
		{
			string[] missing =
			[
				.. constructor
					.GetParameters()
					.Where(x => !CanResolve(services, x))
					.Select(x => x.ParameterType.Name)
			];

			if (missing.Length == 0)
			{
				return string.Empty;
			}

			missingPerConstructor.Add(string.Join(", ", missing));
		}

		return $"{implementationType.Name} needs {string.Join(" | ", missingPerConstructor)}";
	}

	/// <summary>
	/// Type a registration builds, or <c>null</c> when it hands out an instance or a factory.
	/// </summary>
	private static Type? GetImplementationType(ServiceDescriptor descriptor)
	{
		return descriptor.IsKeyedService
			? descriptor.KeyedImplementationType
			: descriptor.ImplementationType;
	}

	/// <summary>
	/// <c>True</c> for a closed type of the application itself.
	/// </summary>
	private static bool IsOwnType(Type? type)
	{
		return type is { ContainsGenericParameters: false }
			&& OwnAssemblies.Contains(type.Assembly);
	}
	#endregion
}
