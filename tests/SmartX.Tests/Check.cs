namespace SmartX.Tests;

/// <summary>
/// Minimal hand-rolled assertion helper. A real project would use xUnit/NUnit,
/// but this sandboxed build has no NuGet network access, so verification here
/// is done with a small dependency-free runner instead — every check still
/// exercises the real production code in SmartX.Shared.
/// </summary>
public static class Check
{
    public static int Passed;
    public static int Failed;

    public static void That(bool condition, string description)
    {
        if (condition)
        {
            Passed++;
            Console.WriteLine($"  PASS  {description}");
        }
        else
        {
            Failed++;
            Console.WriteLine($"  FAIL  {description}");
        }
    }

    public static void AreEqual<T>(T expected, T actual, string description)
    {
        bool ok = EqualityComparer<T>.Default.Equals(expected, actual);
        That(ok, ok ? description : $"{description} (expected {expected}, got {actual})");
    }
}
