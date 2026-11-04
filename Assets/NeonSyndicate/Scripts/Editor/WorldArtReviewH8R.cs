#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>Optional existing-world art overlay. Original collision, quest points, banks and renderer stay owned.</summary>
    public sealed class WorldArtReviewH8R : EditorWindow
    {
        private GameObject _preview;
        private CityLayout _layout;
        private CharacterBase _player;
        private readonly List<Material> _materials=new List<Material>();
        private string _report="Native acceptance NOT RUN. This preview adds four visual modules beside existing route anchors. Original collision remains authoritative. The stair module is intentionally not placed in revision 10.";
        [MenuItem("Neon Syndicate/QA/H8R existing-world art preview")]
        public static void Open(){GetWindow<WorldArtReviewH8R>("H8R world art");}
        private void OnEnable(){EditorApplication.playModeStateChanged+=PlayState;AssemblyReloadEvents.beforeAssemblyReload+=Stop;EditorApplication.update+=CheckSession;}
        private void OnDisable(){Stop();EditorApplication.playModeStateChanged-=PlayState;AssemblyReloadEvents.beforeAssemblyReload-=Stop;EditorApplication.update-=CheckSession;}
        private void PlayState(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode || state==PlayModeStateChange.ExitingEditMode)Stop();}
        private void CheckSession(){if(_preview!=null && (Game.Layout!=_layout || Game.Player!=_player || _player==null || _player.IsDead))Stop();}
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Explicit reversible art review inside Gothic Civic H5. No default world, saved coordinate, gameplay reward or original collider is changed. Appearance, camera clearance and navigation require native acceptance.",MessageType.Info);
            using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying || Game.Player==null))if(GUILayout.Button("Show existing-route art overlay"))Begin();
            if(GUILayout.Button("Remove H8R overlay"))Stop();
            EditorGUILayout.TextArea(_report,GUILayout.MinHeight(130));
        }
        private void Begin()
        {
            Stop();
            try
            {
                if(!EditorApplication.isPlaying || Game.Player==null || Game.Player.IsDead || Game.Layout==null || !Game.Layout.GothicCivicH5)throw new InvalidOperationException("Start Title > Gothic Civic H5, revision 10, with living Malik first.");
                LiveWorldArtH8S live = UnityEngine.Object.FindAnyObjectByType<LiveWorldArtH8S>(); if (live != null && live.ShowingArt) throw new InvalidOperationException("Live world art already shows these pieces in the game; hide it first in F2 > Live world, then run this preview.");
                _layout=Game.Layout;_player=Game.Player;_preview=new GameObject("H8R reversible route art, native acceptance pending");_preview.hideFlags=HideFlags.DontSave;
                Place("SanctuaryPortal","Realm:gallery",new Vector3(0,0,6));
                Place("ShardwoodElder","Realm:azure",new Vector3(5,0,3));
                Place("RootMineralBank","Realm:storm-shrine",new Vector3(-5,0,3));
                Place("RootSpringArch","Realm:storm-cavern",new Vector3(0,0,6));
                _report="Four art modules instantiated in the actual revision-10 world. Original floors and collision remain active. No navigation or visual check is auto-passed. Walk the existing Hearth Witness route and record actual visibility, overlap and camera results. Remove or close this window to revert.";
            }
            catch(Exception error){Stop();_report="Preview refused safely: "+error.Message;Log.Warn(_report);}
        }
        private void Place(string module,string anchor,Vector3 offset)
        {
            Vector3 at;if(!_layout.ResolveRealmAnchor04OC(anchor,out at))throw new InvalidOperationException("Existing route anchor unavailable: "+anchor);
            string path="Assets/NeonSyndicate/Art/WorldFoundationH8R/"+module+".fbx";GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(source==null)throw new InvalidOperationException("Wait for actual native model import: "+path);
            foreach(Component component in source.GetComponentsInChildren<Component>(true))if(component==null || !ImportedArt04OH.VisualComponent(component.GetType()))throw new InvalidOperationException("World art must contain visual components only.");
            GameObject visual=Instantiate(source,_preview.transform,false);visual.name=module;visual.transform.position=at+offset;
            foreach(Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    string role=slots[i]!=null?slots[i].name:"MineralStone";Material material=_materials.Find(item=>item.name==role);
                    if(material==null)
                    {
                        Shader shader=Mats.StandardShader;if(shader==null)throw new InvalidOperationException("Original supported Lit shader unavailable.");
                        Color color=role.Contains("Amethyst")?new Color(.15f,.035f,.22f):role.Contains("Azure")?new Color(.025f,.19f,.22f):role.Contains("Bark")?new Color(.065f,.046f,.034f):role.Contains("Leaf")?new Color(.025f,.095f,.059f):role.Contains("WeatheredEdge")?new Color(.16f,.15f,.14f):new Color(.065f,.077f,.091f);
                        material=new Material(shader){name=role};material.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",color);material.SetFloat(RenderPath.IsURP?"_Smoothness":"_Glossiness",.18f);
                        if(role.Contains("Amethyst") || role.Contains("Azure")){material.SetColor("_EmissionColor",color*.12f);material.EnableKeyword("_EMISSION");}
                        _materials.Add(material);
                    }
                    slots[i]=material;
                }
                renderer.sharedMaterials=slots;
            }
        }
        private void Stop()
        {
            if(_preview!=null)DestroyImmediate(_preview);_preview=null;_layout=null;_player=null;
            foreach(Material material in _materials)if(material!=null)DestroyImmediate(material);_materials.Clear();
        }
    }
}
#endif
