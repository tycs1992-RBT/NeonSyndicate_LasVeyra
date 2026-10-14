using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Loads the shipped quest and event data with System.IO and the engine-free QuestJson reader (no Resources, no
    /// JsonUtility), so the data tests run in the editor and offline alike. The folder is found from the working
    /// directory (Unity runs tests from the project folder) or, failing that, from this source file's location.
    /// </summary>
    internal static class QuestTestData
    {
        private static QuestRefs _refs;

        /// <summary>Assets/NeonSyndicate/Resources, or null when it cannot be found.</summary>
        public static string ResourcesDir
        {
            get { return Find(); }
        }

        private static string Find([CallerFilePath] string here = "")
        {
            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "NeonSyndicate", "Resources");
            if (Directory.Exists(Path.Combine(cwd, "Quests"))) return cwd;
            if (!string.IsNullOrEmpty(here))
            {
                string dir = Path.GetDirectoryName(here);
                for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
                {
                    string r = Path.Combine(dir, "Resources");
                    if (Directory.Exists(Path.Combine(r, "Quests"))) return r;
                    dir = Path.GetDirectoryName(dir);
                }
            }
            return null;
        }

        /// <summary>Everything quest data may refer to, built from the default city.</summary>
        public static QuestRefs Refs
        {
            get
            {
                if (_refs == null) _refs = QuestRefs.Build(TestData.City);
                return _refs;
            }
        }

        /// <summary>Reads every *.json in a Resources subfolder (sorted by name). Reader errors go into errors.</summary>
        public static List<KeyValuePair<string, T>> Load<T>(string folder, List<string> errors, QuestJsonTrace trace) where T : class, new()
        {
            List<KeyValuePair<string, T>> list = new List<KeyValuePair<string, T>>();
            string root = ResourcesDir;
            if (root == null) return list;
            string dir = Path.Combine(root, folder);
            if (!Directory.Exists(dir)) return list;
            string[] files = Directory.GetFiles(dir, "*.json");
            System.Array.Sort(files, System.StringComparer.Ordinal);
            foreach (string path in files)
            {
                string name = Path.GetFileName(path);
                T data = QuestJson.Read<T>(File.ReadAllText(path), name, errors, trace);
                list.Add(new KeyValuePair<string, T>(name, data));
            }
            return list;
        }

        /// <summary>A quest file holding one quest (and a giver for its arc), read from JSON text like the shipped files.</summary>
        public static QuestReport ValidateQuestJson(string questsJson, List<string> readerErrors)
        {
            QuestJsonTrace trace = new QuestJsonTrace();
            QuestFile f = QuestJson.Read<QuestFile>(questsJson, "test.json", readerErrors, trace);
            QuestReport rep = new QuestReport();
            List<KeyValuePair<string, QuestFile>> files = new List<KeyValuePair<string, QuestFile>> { new KeyValuePair<string, QuestFile>("test.json", f) };
            QuestValidator.ValidateQuests(files, Refs, rep, trace);
            return rep;
        }

        public static QuestReport ValidateEventJson(string eventsJson, List<string> readerErrors)
        {
            QuestJsonTrace trace = new QuestJsonTrace();
            EventFile f = QuestJson.Read<EventFile>(eventsJson, "test.json", readerErrors, trace);
            QuestReport rep = new QuestReport();
            List<KeyValuePair<string, EventFile>> files = new List<KeyValuePair<string, EventFile>> { new KeyValuePair<string, EventFile>("test.json", f) };
            QuestValidator.ValidateEvents(files, Refs, rep, trace);
            return rep;
        }

        /// <summary>True when any message contains the text.</summary>
        public static bool Mentions(IEnumerable<string> messages, string text)
        {
            foreach (string m in messages) if (m.Contains(text)) return true;
            return false;
        }
    }
}
