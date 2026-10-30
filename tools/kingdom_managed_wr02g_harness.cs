// Runs only seven pure authored cases from the combined managed reference build, outside the Unity Editor.
using System;
using System.Reflection;

public static class KingdomManagedHarnessWR02G
{
    public static int Main(string[] args)
    {
        try
        {
            Assembly assembly = Assembly.LoadFrom(args[0]); int count = 0;
            foreach (string spec in new[] {
                "NeonSyndicate.Tests.KingdomStreetWR02GTests|FlagstonesStayOnTheSupportedSurfaceAndClearTheOriginalOpening",
                "NeonSyndicate.Tests.KingdomStreetWR02GTests|MissingRouteAndInvalidOpeningRefuseTheCandidate",
                "NeonSyndicate.Tests.FantasyRoutePriorityWR02GTests|AutomaticPreviewRouteNeverReplacesAnExistingNavigationOwner",
                "NeonSyndicate.Tests.ServiceHeraldryWR02GTests|EachPurposeHasItsOwnMarkAndUnknownServicesHaveNone" })
            {
                string[] names = spec.Split('|'); Type type = assembly.GetType(names[0], true);
                object instance = Activator.CreateInstance(type); type.GetMethod(names[1]).Invoke(instance, null);
                Console.WriteLine("PASS " + names[1]); count++;
            }
            Type banner = assembly.GetType("NeonSyndicate.Tests.ServiceHeraldryWR02GTests", true);
            object owner = Activator.CreateInstance(banner);
            MethodInfo frame = banner.GetMethod("FabricUsesKnownExistingSupportsAndStaysClearOfTheDoor");
            foreach (object[] row in new[] {
                new object[] { "lofts", 30f, 7.7f, -.39f, 4.9f },
                new object[] { "forge", 30f, 7.7f, -.39f, 4.9f },
                new object[] { "archive", 48f, 14.35f, -.035f, 5.35f } })
            { frame.Invoke(owner, row); Console.WriteLine("PASS support " + row[0]); count++; }
            Console.WriteLine("Pure managed cases: " + count + " passed. Native Unity: NOT RUN."); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
