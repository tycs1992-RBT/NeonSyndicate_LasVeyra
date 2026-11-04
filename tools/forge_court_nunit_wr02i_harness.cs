// Directly executes eight pure NUnit assertion cases in the managed reference assembly, outside Unity.
using System;
using System.Reflection;

public static class ForgeCourtNunitHarnessWR02I
{
    public static int Main(string[] args)
    {
        try
        {
            Assembly assembly = Assembly.LoadFrom(args[0]);
            Type type = assembly.GetType("NeonSyndicate.Tests.ForgeCourtWR02ITests", true);
            object fixture = Activator.CreateInstance(type); int cases = 0;
            MethodInfo frame = type.GetMethod("OnlyTheKnownUnchangedForgeFrameIsEligible");
            foreach (object[] row in new[] {
                new object[] { "forge", 30f, 30f, false, true }, new object[] { "lofts", 30f, 30f, false, false },
                new object[] { "archive", 30f, 48f, false, false }, new object[] { "forge", 30f, 30f, true, false },
                new object[] { "forge", 30f, 31f, false, false }, new object[] { "forge", float.NaN, 30f, false, false } })
            { frame.Invoke(fixture, row); cases++; }
            foreach (string name in new[] { "PartialShellCannotBecomeAReplacement", "ShippedPhysicalMeshesProduceCompleteBoundedArchitecture" })
            { type.GetMethod(name).Invoke(fixture, null); cases++; }
            Console.WriteLine("PASS " + cases + " pure NUnit assertion cases invoked directly. Unity EditMode/PlayMode NOT RUN."); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
