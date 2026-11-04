using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class VisibleRealityV0
    {
        private string _structureProbe = "Aim the game camera at a reported structure, then inspect. The report distinguishes renderer bounds candidates from exact physics hits.";

        private void DrawStructureProbe(Rect row, ref float y)
        {
            if(UIKit.Button(new Rect(row.x,y,Mathf.Min(540,row.width),36),"Inspect structure at camera centre",false,16)) InspectStructure();
            y+=42;
            Line(row,ref y,_structureProbe,Color.white,380);
        }

        private void InspectStructure()
        {
            Camera camera=Game.Cam!=null?Game.Cam.Cam:Camera.main;
            if(camera==null){_structureProbe="No ordinary game camera is available.";return;}
            Ray ray=camera.ViewportPointToRay(new Vector3(.5f,.5f,0));
            List<KeyValuePair<float,Renderer>> candidates=new List<KeyValuePair<float,Renderer>>();
            foreach(Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if(!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff ||
                    (camera.cullingMask&(1<<renderer.gameObject.layer))==0 || renderer.bounds.size.magnitude<5)continue;
                float distance;if(renderer.bounds.IntersectRay(ray,out distance) && distance<600)
                    candidates.Add(new KeyValuePair<float,Renderer>(distance,renderer));
            }
            candidates.Sort((a,b)=>a.Key.CompareTo(b.Key));
            RaycastHit[] hits=Physics.RaycastAll(ray,600,Layers.WorldMask,QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            StringBuilder report=new StringBuilder();
            report.AppendLine("Camera "+ray.origin.ToString("F2")+" forward "+ray.direction.ToString("F3"));
            report.AppendLine("Renderer BOUNDS candidates, not triangle selection or proof of ownership:");
            for(int i=0;i<Math.Min(6,candidates.Count);i++)
            {
                Renderer r=candidates[i].Value;
                report.AppendLine((i+1)+". "+StructurePath(r.transform)+"; centre "+r.bounds.center.ToString("F1")+" size "+r.bounds.size.ToString("F1"));
                MeshFilter filter=r.GetComponent<MeshFilter>();
                if(filter!=null && filter.sharedMesh!=null) report.AppendLine("mesh "+filter.sharedMesh.name+"; vertices "+filter.sharedMesh.vertexCount+"; CPU readable "+filter.sharedMesh.isReadable);
                // Named sibling pairing is exact for this sprint, and remains explicitly unknown for old merged batches.
                Transform owner=r.transform;
                while(owner!=null && !owner.name.StartsWith("SF01 sculpted crystal architecture / ",StringComparison.Ordinal))owner=owner.parent;
                if(owner!=null && owner.parent!=null)
                {
                    string id=owner.name.Substring("SF01 sculpted crystal architecture / ".Length);
                    Transform solid=HeartPresentationFR01.FindSciencePhysicalOwner(owner.parent,id,true);
                    report.AppendLine("Paired physical owner: "+(solid!=null?StructurePath(solid)+" active "+solid.gameObject.activeInHierarchy:"MISSING"));
                }
                else report.AppendLine("Physical association unproven for this renderer; use the ray hits and source record below.");
            }
            report.AppendLine("Exact collider ray hits (nearest first; not automatically associated with a renderer bounds candidate):");
            for(int i=0;i<Math.Min(4,hits.Length);i++)
                report.AppendLine(StructurePath(hits[i].collider.transform)+"; "+hits[i].distance.ToString("F1")+"m; triangle "+hits[i].triangleIndex+"; point "+hits[i].point.ToString("F2"));
            if(hits.Length==0)report.AppendLine("No world collider intersected this camera ray.");
            _structureProbe=report.ToString();Log.Info("Structure inspection\n"+_structureProbe);
        }

        private static string StructurePath(Transform node)
        {
            string path=node.name;while(node.parent!=null){node=node.parent;path=node.name+"/"+path;}return path;
        }
    }
}
