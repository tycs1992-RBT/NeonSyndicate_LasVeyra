using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class ValeCommission
    {
        public string id;
        public List<string> objectives, outbound, homecoming;

        public static ValeCommission Read(string json)
        {
            List<string> errors=new List<string>();
            ValeCommission data=QuestJson.Read<ValeCommission>(json,"HearthCommission.json",errors,null);
            if(errors.Count>0 || data==null || data.id!="shardwake.hearth-seed" ||
                data.objectives?.Count!=Enum.GetValues(typeof(ValeStep)).Length || data.outbound?.Count!=3 || data.homecoming?.Count!=2)
                throw new FormatException("Invalid hearth commission. "+string.Join(" ",errors));
            return data;
        }
    }

    public sealed partial class ShardwakeVale
    {
        private GUIStyle _bodyStyle,_titleStyle,_tinyStyle;
        public TextAsset commission;
        private ValeCommission _brief;
        private string Dialogue()
        {
            return _page<_brief.outbound.Count?_brief.outbound[_page]:_brief.homecoming[_page-_brief.outbound.Count];
        }
        private string TaskText()
        {
            string objective=_brief.objectives[(int)_run.Profile.step];
            if(_run.Profile.step!=ValeStep.GroundCrown)return objective;
            int count=0;for(int i=0;i<3;i++)if(_run.Spent(i))count++;
            return objective+"    "+count+" / 3";
        }
        private Vector3 Objective()
        {
            switch(_run.Profile.step)
            {
                case ValeStep.CrossBridge:return bridge.TransformPoint(new Vector3(0,2,2));
                case ValeStep.GroundCrown:
                    int seen=-1;float best=float.MaxValue;
                    for(int i=0;i<3;i++){float d=Flat(_body.transform.position,sockets[i].position);if(!_run.Spent(i)&&d<best){best=d;seen=i;}}
                    return seen<0?landmark.position:GemPoint(seen);
                case ValeStep.TakeSeed:return pickup.position+Vector3.up;
                case ValeStep.FitHearth:return forgeHeart.transform.position+Vector3.up;
                default:return dante.position+Vector3.up*1.9f;
            }
        }
        private void OnGUI()
        {
            if(_run==null || _body==null)return;
            if(_bodyStyle==null)
            {
                _bodyStyle=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true};
                _titleStyle=new GUIStyle(_bodyStyle){fontSize=24,fontStyle=FontStyle.Bold};
                _tinyStyle=new GUIStyle(_bodyStyle){fontSize=14};
            }
            float scale=Mathf.Clamp(Mathf.Min(Screen.width/1040f,Screen.height/590f),.55f,1.8f);
            Matrix4x4 old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float w=Screen.width/scale,h=Screen.height/scale;
            try
            {
                if(_flash>0)Panel(new Rect(0,0,w,h),new Color(.38f,.83f,.95f,_flash*.18f));
                Panel(new Rect(18,16,434,94),new Color(.075f,.135f,.15f,.88f));
                GUI.Label(new Rect(32,21,399,24),"SHARDWAKE VALE     /     PILOT PROFILE",_tinyStyle);
                GUI.Label(new Rect(32,47,405,56),TaskText(),_bodyStyle);
                if(_paused){Panel(new Rect(w/2-170,h/2-45,340,90),new Color(.07f,.12f,.13f,.93f));GUI.Label(new Rect(w/2-150,h/2-18,295,48),"Paused    ·    Esc / Start to resume",_bodyStyle);return;}
                if(_page>=0)
                {
                    Panel(new Rect(28,h-144,w-56,120),new Color(.08f,.12f,.13f,.96f));
                    GUI.Label(new Rect(46,h-135,260,32),"Dante Rook",_titleStyle);
                    GUI.Label(new Rect(46,h-99,w-116,54),Dialogue(),_bodyStyle);
                    GUI.Label(new Rect(w-223,h-54,180,24),"E / X    Continue",_tinyStyle);return;
                }
                Vector3 dest=Objective(),screen=view.WorldToViewportPoint(dest);float dist=Flat(_body.transform.position,dest);
                if(_run.Profile.step!=ValeStep.HearthLit && dist>3.0f)
                {
                    float sx=screen.z>=0?screen.x:1-screen.x;
                    float x=Mathf.Clamp(sx*w,118,w-118),y=Mathf.Clamp((1-screen.y)*h-25,116,h-135);
                    Rect chip=new Rect(x-44,y,88,23);Panel(chip,new Color(.13f,.18f,.19f,.78f));
                    GUI.Label(new Rect(chip.x+6,chip.y,85,25),(screen.z<0?"Turn  ":"◆  ")+Mathf.RoundToInt(dist)+" m",_tinyStyle);
                }
                string cue="";
                if(Near(dante,3.1f))cue=_run.Profile.step==ValeStep.HearthLit?"E / X   Talk      R / Y   Replay commission":"E / X   Speak with Dante";
                if(_run.Profile.step==ValeStep.FitHearth && Near(forgeHeart.transform,2.9f))cue="E / X   Fit the hearth seed";
                if(_run.Profile.step==ValeStep.TakeSeed && Near(pickup,2.8f))cue="E / X   Take the hearth seed";
                if(_run.Profile.step==ValeStep.GroundCrown && Flat(_body.transform.position,landmark.position)<28)
                    cue=_run.Exposed?"Amber faces open   ·   Tab / R3 target    B / RT Sunbolt":"Wave charging   ·   Move clear or jump";
                if(_tipTimer>0)cue=_tip;
                if(cue.Length>0){Panel(new Rect(w*.16f,h-97,w*.68f,47),new Color(.09f,.14f,.15f,.9f));GUI.Label(new Rect(w*.16f+12,h-92,w*.68f-24,43),cue,_bodyStyle);}
                string controls=_run.Profile.step==ValeStep.GroundCrown?"SUNBOLT   "+Mathf.RoundToInt(_run.Energy)+" / 100      ·      Amber after the wave      ·      Esc / Start pause":
                    "Move  WASD / LS     ·     Look  mouse / RS     ·     Interact  E / X     ·     Jump  Space / A";
                Panel(new Rect(18,h-41,w-36,27),new Color(.09f,.14f,.15f,.80f));GUI.Label(new Rect(30,h-40,w-60,26),controls,_tinyStyle);
            }
            finally{GUI.matrix=old;}
        }
        private static void Panel(Rect r,Color c){Color previous=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=previous;}
    }
}
