// Offline source-validation host. No runtime scene, physics, JsonUtility or event coroutine execution occurs here.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using NeonSyndicate;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    public static class QuestTestData
    {
        public static string ResourcesDir;
        public static List<KeyValuePair<string, T>> Load<T>(string folder, List<string> errors, QuestJsonTrace trace) where T : class, new()
        {
            List<KeyValuePair<string, T>> files = new List<KeyValuePair<string, T>>();
            foreach (string file in Directory.GetFiles(Path.Combine(ResourcesDir, folder), "*.json").OrderBy(x => x, StringComparer.Ordinal))
                files.Add(new KeyValuePair<string, T>(Path.GetFileName(file), QuestJson.Read<T>(File.ReadAllText(file), file, errors, trace)));
            return files;
        }
    }
}

public static class EventValidationProgram
{
    public static int Main(string[] args)
    {
        NeonSyndicate.Tests.QuestTestData.ResourcesDir = args[0];
        Dictionary<string, string[]> catalogs = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(args[1]));
        QuestRefs refs = new QuestRefs();
        refs.Species.UnionWith(catalogs["species"]);
        refs.Bosses.UnionWith(catalogs["bosses"]);
        refs.Materials.UnionWith(catalogs["materials"]);
        refs.Factions.UnionWith(catalogs["factions"]);
        refs.Weathers.UnionWith(Enum.GetNames(typeof(Weather)));
        QuestJsonTrace trace = new QuestJsonTrace();
        List<string> errors = new List<string>();
        List<KeyValuePair<string, EventFile>> all = NeonSyndicate.Tests.QuestTestData.Load<EventFile>("Events", errors, trace);
        HashSet<string> ids = new HashSet<string>();
        foreach (KeyValuePair<string, EventFile> file in all)
            foreach (EventTemplate t in file.Value.Events) if (!ids.Add(t.Id)) errors.Add("Duplicate shipped event ID: " + t.Id);
        List<KeyValuePair<string, EventFile>> selected = all.Where(x => x.Key.StartsWith("living04b_", StringComparison.Ordinal)).ToList();
        QuestReport report = new QuestReport();
        QuestValidator.ValidateEvents(selected, refs, report, trace);
        errors.AddRange(report.Errors);
        List<string> failures = new List<string>();
        int passed = 0;
        foreach (Type suite in new[] { typeof(NeonSyndicate.Tests.TerritoryEventTests), typeof(NeonSyndicate.Tests.WorldEventTests), typeof(NeonSyndicate.Tests.RetainedEventCoverageTests) })
        {
            object instance = Activator.CreateInstance(suite);
            foreach (MethodInfo method in suite.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                TestCaseAttribute[] cases = method.GetCustomAttributes(typeof(TestCaseAttribute), true).Cast<TestCaseAttribute>().ToArray();
                List<object[]> arguments = new List<object[]>();
                if (cases.Length > 0) foreach (TestCaseAttribute item in cases) arguments.Add(item.Arguments);
                else if (method.GetCustomAttributes(typeof(TestAttribute), true).Length > 0) arguments.Add(new object[0]);
                foreach (object[] values in arguments)
                {
                    try { method.Invoke(instance, values); passed++; }
                    catch (Exception ex) { failures.Add(suite.Name + "." + method.Name + ": " + (ex.InnerException ?? ex).Message); }
                }
            }
        }
        Console.WriteLine(JsonSerializer.Serialize(new { events = report.Events, total_shipped_unique_ids = ids.Count, strict_errors = errors, warnings = report.Warnings, nunit_passed = passed, nunit_failures = failures, nativeUnity = "NOT RUN" }));
        return errors.Count + failures.Count == 0 ? 0 : 1;
    }
}
