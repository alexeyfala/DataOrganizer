// Marks every test of the assembly, so that a run can leave the ones that need a real
// database file out with --filter TestCategory!=Integration.
[assembly: Category("Integration")]
