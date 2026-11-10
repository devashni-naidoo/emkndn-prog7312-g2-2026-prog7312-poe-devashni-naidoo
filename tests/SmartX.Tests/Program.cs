using SmartX.Tests;

Console.WriteLine("SmartX.Tests -- dependency-free verification of SmartX.Shared");
Console.WriteLine("(no NuGet-based test framework: this sandbox has no NuGet network access)");
Console.WriteLine();

TelemetryPacketTests.Run();
Console.WriteLine();
PowerMeterReadingTests.Run();
Console.WriteLine();
TelemetryHistoryBufferTests.Run();
Console.WriteLine();
ZoneLoadMatrixTests.Run();
Console.WriteLine();
DeploymentValidatorTests.Run();
Console.WriteLine();
Part2DataStructureTests.Run();
Console.WriteLine();
Part3DataStructureTests.Run();

Console.WriteLine();
Console.WriteLine($"---- {Check.Passed} passed, {Check.Failed} failed ----");

Environment.ExitCode = Check.Failed == 0 ? 0 : 1;
