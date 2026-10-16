// Offline host: exact quest validation and phase graph methods, no native Unity execution.
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
        public static QuestRefs Refs;
        public static List<KeyValuePair<string, T>> Load<T>(string folder, List<string> errors, QuestJsonTrace trace) where T : class, new()
        {
            List<KeyValuePair<string, T>> files = new List<KeyValuePair<string, T>>();
            foreach (string file in Directory.GetFiles(Path.Combine(ResourcesDir, folder), "*.json").OrderBy(x => x, StringComparer.Ordinal))
                files.Add(new KeyValuePair<string, T>(Path.GetFileName(file), QuestJson.Read<T>(File.ReadAllText(file), file, errors, trace)));
            return files;
        }
    }
}

public static class QuestValidationProgram
{
    public static int Main(string[] args)
    {
        NeonSyndicate.Tests.QuestTestData.ResourcesDir = args[0];
        Dictionary<string, string[]> catalogs = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(args[1]));
        QuestRefs refs = new QuestRefs { Layout = new CityLayout() };
        refs.Species.UnionWith(catalogs["species"]); refs.Bosses.UnionWith(catalogs["bosses"]);
        refs.Materials.UnionWith(catalogs["materials"]); refs.Factions.UnionWith(catalogs["factions"]);
        refs.Pois.UnionWith(catalogs["pois"]); refs.Weathers.UnionWith(Enum.GetNames(typeof(Weather)));
        NeonSyndicate.Tests.QuestTestData.Refs = refs;
        QuestJsonTrace trace = new QuestJsonTrace(); List<string> errors = new List<string>();
        List<KeyValuePair<string, QuestFile>> all = NeonSyndicate.Tests.QuestTestData.Load<QuestFile>("Quests", errors, trace);
        HashSet<string> ids = new HashSet<string>();
        foreach (KeyValuePair<string, QuestFile> file in all)
            foreach (QuestDef quest in file.Value.Quests) if (!ids.Add(quest.Id)) errors.Add("Duplicate shipped quest ID: " + quest.Id);
        List<KeyValuePair<string, QuestFile>> selected = all.Where(x => x.Key == "quiet_signal_04c.json").ToList();
        QuestReport report = new QuestReport(); QuestValidator.ValidateQuests(selected, refs, report, trace); errors.AddRange(report.Errors);
        List<string> failures = new List<string>(); int passed = 0;
        foreach (Type suite in new[] { typeof(NeonSyndicate.Tests.QuietSignal04CTests), typeof(NeonSyndicate.Tests.RetainedGuildChainTests) })
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
                catch (Exception exception) { failures.Add(suite.Name + "." + method.Name + ": " + (exception.InnerException ?? exception).Message); }
            }
        }
        }
        Console.Write(JsonSerializer.Serialize(new { quests = report.Quests, phases = report.Phases, steps = report.Steps, total_shipped_unique_ids = ids.Count,
            strict_errors = errors, warnings = report.Warnings, nunit_passed = passed, nunit_failures = failures,
            unityEditor = "NOT RUN", nativeRoomAndQuestExecution = "NOT RUN" }));
        return errors.Count + report.Warnings.Count + failures.Count == 0 ? 0 : 1;
    }
}
