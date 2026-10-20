#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace NeonSyndicate.EditorTools
{
    /// <summary>Owner-operated native evidence. Reuses QA capture authority; never loads or exports a player save.</summary>
    public sealed class CinematicAcceptanceWindow04OF : EditorWindow
    {
        [SerializeField] private string _folder="",_label="malik_front",_message="No acceptance run recorded.";
        [SerializeField] private bool[] _checked;
        [SerializeField] private AcceptanceTestRun04OF _tests;
        private Vector2 _scroll;
        private string _pending="",_beforeOutput="";
        private double _deadline;
        private readonly List<string> _console=new List<string>();
        [MenuItem("Neon Syndicate/QA/04OF Cinematic Acceptance")]
        public static void Open(){GetWindow<CinematicAcceptanceWindow04OF>("04OF Acceptance");}
        private void OnEnable()
        {
            if(_checked==null || _checked.Length!=CinematicAcceptance04OF.Checklist().Length)_checked=new bool[CinematicAcceptance04OF.Checklist().Length];
            Application.logMessageReceived+=OnLog;EditorApplication.update+=Tick;
        }
        private void OnDisable(){Application.logMessageReceived-=OnLog;EditorApplication.update-=Tick;}
        private void OnLog(string message,string trace,LogType type)
        {
            if(type!=LogType.Error && type!=LogType.Exception && type!=LogType.Assert)return;
            if(_console.Count>=200)_console.RemoveAt(0);
            _console.Add(DateTime.UtcNow.ToString("o")+" "+type+" "+message+"\n"+trace);
        }
        private void OnGUI()
        {
            _scroll=EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("04OF Cinematic Acceptance",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Native evidence is recorded only when you run these tools in your Editor. Checkboxes are manual observations, not automated test passes. No save file is copied or uploaded.",MessageType.Info);
            EditorGUILayout.LabelField("Unity",Application.unityVersion);
            EditorGUILayout.LabelField("Scene",EditorSceneManager.GetActiveScene().path);
            EditorGUILayout.LabelField("Renderer / preset",GoldenManifest.CurrentRenderPath()+" / "+Settings.Graphics);
            EditorGUILayout.LabelField("Runtime",Game.Layout==null?"No running world":Game.Layout.Revision04O+" / seed "+Game.Layout.Seed+" / bank "+SaveSystem.ActiveBankRevision04OB);
            if(Game.Layout!=null)EditorGUILayout.HelpBox(CinematicAcceptance04OF.ProfileWarning((int)Game.Layout.Revision04O),Game.Layout.Unbound04OE?MessageType.Info:MessageType.Warning);
            if(GUILayout.Button("Start a new local evidence folder"))Try(NewFolder);
            EditorGUILayout.SelectableLabel(_folder,EditorStyles.textField,GUILayout.Height(22));
            _label=EditorGUILayout.TextField("Actual shot label",_label);
            using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying || _pending!=""))
            {
                if(GUILayout.Button("Capture current Game view (combat, menu or flight)"))Try(()=>
                {
                    EnsureFolder();_beforeOutput=QaTools.LastOutput;
                    if(!QaTools.StartPlaceMomentShot()){_message=QaTools.LastMessage;return;}
                    _pending=CinematicAcceptance04OF.Label(_label);_deadline=EditorApplication.timeSinceStartup+15;
                    _message="Capture requested. Resume the Editor if paused at a breakpoint.";
                });
                if(GUILayout.Button("Capture staged matched day / night Golden Views"))_message=QaTools.StartGoldenShots()?"Camera tour launched. Not a combat playtest.":QaTools.LastMessage;
                if(GUILayout.Button("Record existing native performance flythrough"))_message=QaTools.StartPerfCapture()?"Profiler route launched. Import its output when finished.":QaTools.LastMessage;
            }
            if(QaTools.Busy && GUILayout.Button("Cancel QA camera tour and restore control"))QaTools.Cancel();
            if(GUILayout.Button("Copy last completed QA output into this evidence folder"))Try(()=>
            {if(!QaTools.LastSucceeded)throw new InvalidOperationException("Last QA job did not complete successfully.");EnsureFolder();CopyEvidence(QaTools.LastOutput,"qa_"+DateTime.UtcNow.ToString("HHmmssfff"));});
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tests (Unity Test Framework)",EditorStyles.boldLabel);
            if(GUILayout.Button("Run native EditMode suite"))Try(()=>LaunchTests("EditMode"));
            if(GUILayout.Button("Run native PlayMode suite"))Try(()=>LaunchTests("PlayMode"));
            if(_tests!=null){EditorGUILayout.HelpBox(_tests.Status,MessageType.None);if(_tests.Active && GUILayout.Button("Request test cancellation"))_tests.Cancel();}
            if(GUILayout.Button("Open Test Runner / inspect or cancel active run"))EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
            if(GUILayout.Button("Attach real Test Runner XML result"))Try(AttachResults);
            EditorGUILayout.HelpBox("Actual callbacks write status and raw XML. Launching is not passing. If interrupted by an Editor reload, inspect Test Runner and attach its exported XML. PlayMode may recompile; the run folder persists.",MessageType.Info);
            if(GUILayout.Button("Open imported visual wizard"))NeonSyndicate.Editor.ImportedVisualWizard04OF.Open();
            if(GUILayout.Button("Audit supplied model assets"))NeonSyndicate.Editor.ImportedModelAudit04OD.Audit();
            EditorGUILayout.Space();EditorGUILayout.LabelField("Manual observations, initially NOT RUN",EditorStyles.boldLabel);
            string[] checklist=CinematicAcceptance04OF.Checklist();
            for(int i=0;i<checklist.Length;i++)_checked[i]=EditorGUILayout.ToggleLeft(checklist[i],_checked[i]);
            if(GUILayout.Button("Write report and create shareable ZIP"))Try(Export);
            if(_folder!="" && GUILayout.Button("Open evidence folder for review"))EditorUtility.RevealInFinder(_folder);
            EditorGUILayout.HelpBox(_message,MessageType.None);
            EditorGUILayout.EndScrollView();
        }
        private void NewFolder()
        {
            _folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Logs","Acceptance04OF",DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"));
            Directory.CreateDirectory(_folder);Array.Clear(_checked,0,_checked.Length);_console.Clear();WriteContext();
            _message="New evidence folder created. All native acceptance observations start NOT RUN.";
        }
        private void EnsureFolder(){if(_folder=="" || !Directory.Exists(_folder))NewFolder();}
        private void Tick()
        {
            if(_pending=="")return;
            if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup>_deadline)
            {_pending="";_message="Capture cancelled or timed out. No screenshot was marked passed.";Repaint();return;}
            if(QaTools.LastOutput==_beforeOutput || !QaTools.LastSucceeded)return;
            string label=_pending;_pending="";
            Try(()=>{CopyEvidence(QaTools.LastOutput,"shots/"+label+"_"+DateTime.UtcNow.ToString("HHmmssfff"));WriteContext();_message="Actual frame copied. Inspect image before accepting visual quality.";});Repaint();
        }
        private void CopyEvidence(string source,string relative)
        {
            if(File.Exists(source))
            {
                string name=Path.GetFileName(source);if(!CinematicAcceptance04OF.SafeEvidenceFile(name))throw new IOException("Unsupported evidence file.");
                string folder=Path.Combine(_folder,relative);Directory.CreateDirectory(folder);File.Copy(source,Path.Combine(folder,name),false);
                string summary=Path.ChangeExtension(source,".txt");if(File.Exists(summary) && summary!=source)File.Copy(summary,Path.Combine(folder,Path.GetFileName(summary)),false);return;
            }
            if(!Directory.Exists(source))throw new IOException("QA output folder is unavailable.");
            foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories))
            {
                string rel=file.Substring(source.TrimEnd(Path.DirectorySeparatorChar).Length+1);
                if(!CinematicAcceptance04OF.SafeEvidenceFile(rel))continue;
                string destination=Path.Combine(_folder,relative,rel);Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(file,destination,false);
            }
            _message="Copied completed QA evidence. No save files included.";
        }
        private void WriteContext()
        {
            StringBuilder text=new StringBuilder();
            text.AppendLine("Recorded UTC: "+DateTime.UtcNow.ToString("o"));text.AppendLine("Unity: "+Application.unityVersion);
            text.AppendLine("Branch: "+Git("branch --show-current"));text.AppendLine("HEAD: "+Git("rev-parse HEAD"));
            text.AppendLine("Scene: "+EditorSceneManager.GetActiveScene().path);text.AppendLine("Renderer: "+GoldenManifest.CurrentRenderPath());
            text.AppendLine("Preset: "+Settings.Graphics+" / GPU: "+SystemInfo.graphicsDeviceName);
            text.AppendLine("World: "+(Game.Layout==null?"NOT RUN":Game.Layout.Revision04O+" / seed "+Game.Layout.Seed+" / bank "+SaveSystem.ActiveBankRevision04OB));
            text.AppendLine("QA: "+QaTools.Status);text.AppendLine("Captures record actual views; boss phases, success, saves and build require manual evidence.");
            File.WriteAllText(Path.Combine(_folder,"context_"+DateTime.UtcNow.ToString("HHmmssfff")+".txt"),text.ToString());
        }
        private string Git(string arguments)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo info=new System.Diagnostics.ProcessStartInfo("git",arguments){WorkingDirectory=Directory.GetParent(Application.dataPath).FullName,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
                using(System.Diagnostics.Process process=System.Diagnostics.Process.Start(info))
                {if(!process.WaitForExit(1500)){process.Kill();return "UNAVAILABLE (timeout)";}return process.ExitCode==0?process.StandardOutput.ReadToEnd().Trim():"UNAVAILABLE";}
            }
            catch(Exception){return "UNAVAILABLE (git not installed)";}
        }
        private void LaunchTests(string mode)
        {
            EnsureFolder();
            if(EditorApplication.isPlaying || EditorApplication.isCompiling)throw new InvalidOperationException("Stop Play Mode and wait for compilation before launching tests.");
            if(_tests!=null && _tests.Active)throw new InvalidOperationException("The previous test run is active. Inspect or cancel it first.");
            _tests=CreateInstance<AcceptanceTestRun04OF>();_tests.Launch(_folder,mode);_message=_tests.Status;
        }
        private void AttachResults()
        {
            string path=EditorUtility.OpenFilePanel("Actual Unity Test Runner result","","xml");if(path=="")return;
            System.Xml.XmlDocument doc=new System.Xml.XmlDocument();doc.XmlResolver=null;
            using(System.Xml.XmlReader reader=System.Xml.XmlReader.Create(path,new System.Xml.XmlReaderSettings{DtdProcessing=System.Xml.DtdProcessing.Prohibit,XmlResolver=null}))doc.Load(reader);
            System.Xml.XmlElement root=doc.DocumentElement;
            if(root==null || root.Name!="test-run")throw new InvalidDataException("Expected raw NUnit test-run XML exported by Unity Test Runner.");
            EnsureFolder();string result=root.GetAttribute("result");
            File.Copy(path,Path.Combine(_folder,"native_tests_"+DateTime.UtcNow.ToString("HHmmssfff")+".xml"));
            _message="ATTACHED actual XML: "+result+" / total "+root.GetAttribute("total")+" / failed "+root.GetAttribute("failed")+". Inspect skipped/cancelled details in raw XML.";
            File.AppendAllText(Path.Combine(_folder,"test_results.txt"),_message+"\n");
        }
        private void Export()
        {
            EnsureFolder();WriteContext();
            StringBuilder report=new StringBuilder("# Owner observations\n\nManual observations only. Raw XML establishes automated test disposition. Unchecked = NOT RUN.\n\n");
            string[] checklist=CinematicAcceptance04OF.Checklist();
            for(int i=0;i<checklist.Length;i++)report.AppendLine("- ["+(_checked[i]?"x":" ")+"] "+checklist[i]);
            File.WriteAllText(Path.Combine(_folder,"manual_checklist.md"),report.ToString());
            File.WriteAllLines(Path.Combine(_folder,"console_errors.log"),_console);
            File.WriteAllText(Path.Combine(_folder,"READ_BEFORE_SHARING.txt"),"Review screenshots and logs for private information before sharing. This bundle excludes player saves. No upload was performed. No screenshot proves an unobserved save/reward/camera test.\n");
            string zip=_folder+".zip";if(File.Exists(zip))File.Delete(zip);
            using(ZipArchive archive=ZipFile.Open(zip,ZipArchiveMode.Create))
                foreach(string file in Directory.GetFiles(_folder,"*",SearchOption.AllDirectories))
                {
                    string relative=file.Substring(_folder.TrimEnd(Path.DirectorySeparatorChar).Length+1);
                    if(CinematicAcceptance04OF.SafeEvidenceFile(relative))archive.CreateEntryFromFile(file,relative,System.IO.Compression.CompressionLevel.Optimal);
                }
            _message="ZIP created beside this folder. Review private information before sharing it. Nothing was uploaded.";
        }
        private void Try(Action action){try{action();}catch(Exception e){_message=e.GetBaseException().Message;Debug.LogWarning("04OF acceptance: "+_message);}}
    }
}
#endif
