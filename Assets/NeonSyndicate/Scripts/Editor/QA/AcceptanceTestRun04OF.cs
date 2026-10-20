#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace NeonSyndicate.EditorTools
{
    /// <summary>Real Unity Test Framework callbacks. Launching, cancellation and completion have separate receipts.</summary>
    public sealed class AcceptanceTestRun04OF : ScriptableObject, ICallbacks
    {
        [SerializeField] private TestRunnerApi _api;
        [SerializeField] private string _folder,_guid,_mode,_status="NOT RUN";
        [SerializeField] private bool _active;
        public bool Active {get{return _active;}}
        public string Status {get{return _status;}}
        private void OnEnable()
        {if(_api!=null){_api.UnregisterCallbacks(this);_api.RegisterCallbacks(this);}}
        public void Launch(string folder,string mode)
        {
            if(_active)throw new InvalidOperationException("A test run is already active.");
            _folder=Path.Combine(folder,"tests_"+mode+"_"+DateTime.UtcNow.ToString("HHmmssfff"));Directory.CreateDirectory(_folder);_mode=mode;
            _api=CreateInstance<TestRunnerApi>();_api.RegisterCallbacks(this);_active=true;
            Write("LAUNCHED: "+mode+". Completion NOT VERIFIED.");
            try{_guid=_api.Execute(new ExecutionSettings(new Filter{testMode=mode=="EditMode"?TestMode.EditMode:TestMode.PlayMode}));File.WriteAllText(Path.Combine(_folder,"run-id.txt"),_guid);}
            catch(Exception e){_active=false;Write("UNAVAILABLE: "+e.GetBaseException().Message);throw;}
        }
        public void Cancel()
        {
            if(!_active)return;
            Write(TestRunnerApi.CancelTestRun(_guid)?"CANCELLATION REQUESTED. Await actual result.":"Cancellation unavailable. Inspect Test Runner; completion NOT VERIFIED.");
        }
        public void RunStarted(ITestAdaptor testsToRun){if(_active)Write("RUNNING: "+_mode);}
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            if(!_active)return;_active=false;
            try
            {
                TestRunnerApi.SaveResultToFile(result,Path.Combine(_folder,"nunit-results.xml"));
                int executed=result.PassCount+result.FailCount+result.InconclusiveCount;
                Write((executed==0?"NO EXECUTED CASES / ":"")+result.ResultState+" / passed "+result.PassCount+" / failed "+result.FailCount+" / skipped "+result.SkipCount+" / inconclusive "+result.InconclusiveCount);
            }
            catch(Exception e){Write("RESULT CALLBACK RECEIVED, EXPORT FAILED: "+e.Message+". Export manually from Test Runner.");}
            if(_api!=null)_api.UnregisterCallbacks(this);
        }
        private void Write(string value){_status=value;File.AppendAllText(Path.Combine(_folder,"status.txt"),DateTime.UtcNow.ToString("o")+" "+value+"\n");}
    }
}
#endif
