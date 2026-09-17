using Avalonia.Headless;
using DataOrganizer.UnitTests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

// Bounds the workers the fixtures marked parallel may take: a single key derivation holds
// its memory cost for the whole run, so a pool of the machine's size would claim gibibytes.
[assembly: LevelOfParallelism(6)]
