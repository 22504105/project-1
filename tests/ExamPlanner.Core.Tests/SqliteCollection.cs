using Xunit;

namespace ExamPlanner.Core.Tests;

// Test classes that use a real SqlitePlannerRepository call the global
// SQLiteAsyncConnection.ResetPool() in Dispose (needed to release the file lock
// on Windows). Grouping them in one non-parallel collection stops them from
// tearing down each other's connections when run concurrently.
[CollectionDefinition("Sqlite", DisableParallelization = true)]
public class SqliteCollection { }
