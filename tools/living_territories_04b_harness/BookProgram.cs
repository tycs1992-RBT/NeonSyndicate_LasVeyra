using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using NUnit.Framework;
using NeonSyndicate.Tests;
public static class BookProgram
{
    public static int Main()
    {
        List<object> results = new List<object>(); int failures = 0;
        Type type = typeof(TerritoryBookTests);
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            List<object[]> cases = new List<object[]>();
            foreach (TestCaseAttribute attribute in method.GetCustomAttributes(typeof(TestCaseAttribute), true)) cases.Add(attribute.Arguments);
            if (method.GetCustomAttributes(typeof(TestAttribute), true).Length > 0) cases.Add(new object[0]);
            foreach (object[] arguments in cases)
            {
                string error = null;
                try { method.Invoke(Activator.CreateInstance(type), arguments); }
                catch (TargetInvocationException exception) { error = exception.InnerException.ToString(); failures++; }
                results.Add(new { name = type.Name + "." + method.Name, arguments, passed = error == null, error });
            }
        }
        Console.Write(JsonSerializer.Serialize(new { scope = "Actual unchanged engine-free TerritoryBook, TerritoryData, QuestJson and NUnit tests executed on .NET 8. No Unity object, JsonUtility, encounter actor, input or physics execution.", tests = results, failures,
            unityEditMode = "NOT RUN", unityJsonUtility = "NOT RUN" }, new JsonSerializerOptions { WriteIndented=true }));
        return failures == 0 ? 0 : 1;
    }
}
