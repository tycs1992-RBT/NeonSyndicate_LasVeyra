// Invoke only the twelve pure geometry/frame NUnit cases. This is not the Unity Test Runner.
using System;
using System.Reflection;

public static class SupportedDistrictNunitHarnessWR02J
{
    public static int Main(string[] args)
    {
        int passed = 0, failed = 0;
        try
        {
            Assembly assembly = Assembly.LoadFrom(args[0]);
            Type type = assembly.GetType("NeonSyndicate.Tests.SupportedDistrictWR02JTests", true);
            object fixture = Activator.CreateInstance(type);
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                foreach (object attribute in method.GetCustomAttributes(false))
                {
                    string name = attribute.GetType().FullName;
                    if (name != "NUnit.Framework.TestAttribute" && name != "NUnit.Framework.TestCaseAttribute") continue;
                    object[] row = name == "NUnit.Framework.TestCaseAttribute" ?
                        (object[])attribute.GetType().GetProperty("Arguments").GetValue(attribute) : null;
                    try { method.Invoke(fixture, row); passed++; Console.WriteLine("PASS " + method.Name + " " + (row == null ? "" : string.Join(",", row))); }
                    catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + method.Name + ": " + (error.InnerException ?? error)); }
                }
            Console.WriteLine("Pure NUnit cases: " + passed + " passed, " + failed + " failed. Unity EditMode/PlayMode NOT RUN.");
            return passed == 12 && failed == 0 ? 0 : 1;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
