#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate.EditorTools
{
    /// <summary>
    /// Editor-only, repeatable generation of original faceted meshes and a visual-only
    /// crystal-gothic proof block. No runtime hooks, physics or package dependencies.
    /// </summary>
    public static class CrystalGothicKitBuilder
    {
        private const string Root = "Assets/NeonSyndicate/Art/CrystalGothicProof";
        private const string MeshDir = Root + "/Meshes";
        private const string MatDir = Root + "/Materials";
        private const string PrefabDir = Root + "/Prefabs";

        [MenuItem("Neon Syndicate/Art/Build Crystal Gothic Proof Kit")]
        public static void Build()
        {
            EnsureFolder("Assets/NeonSyndicate", "Art");
            EnsureFolder("Assets/NeonSyndicate/Art", "CrystalGothicProof");
            EnsureFolder(Root, "Meshes");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Prefabs");

            Material stone = MakeMaterial("CG_DarkMineral", new Color(.065f,.078f,.13f), 0.18f, false);
            Material trim = MakeMaterial("CG_GoldInlay", new Color(.52f,.32f,.12f), .65f, false);
            Material violet = MakeMaterial("CG_Amethyst", new Color(.24f,.065f,.50f), .68f, true);
            Material cyan = MakeMaterial("CG_CyanCrystal", new Color(.04f,.50f,.65f), .6f, true);
            Material foliage = MakeMaterial("CG_MysticFoliage", new Color(.06f,.20f,.16f), .16f, false);

            Mesh column = SaveMesh("CG_FacetedButtress", Rings(8,
                new[] {new Vector2(0f,.58f),new Vector2(.5f,.54f),new Vector2(4.8f,.38f),new Vector2(6.0f,.23f)}));
            Mesh shard = SaveMesh("CG_AmethystShard", Rings(6,
                new[] {new Vector2(0f,.15f),new Vector2(.35f,.48f),new Vector2(2.2f,.38f),new Vector2(4.7f,0f)}));
            Mesh beam = SaveMesh("CG_StoneBeam", BoxMesh());
            Mesh trunk = SaveMesh("CG_OrganicTrunk", Rings(7,
                new[] {new Vector2(0f,.27f),new Vector2(1.6f,.19f),new Vector2(4f,.08f)}));

            GameObject root = new GameObject("CG | Crystal Gothic Proof Block");
            root.transform.position = Vector3.zero;
            try
            {
                // A single 20m facade; visual-only and intentionally offset-free.
                // Place next to an existing block, never over a vehicle route.
                Add(root.transform,"Mineral pier L",column,stone,new Vector3(-8f,0,0),Vector3.one);
                Add(root.transform,"Mineral pier R",column,stone,new Vector3(8f,0,0),Vector3.one);
                Add(root.transform,"Mineral pier C",column,stone,new Vector3(0f,0,0),new Vector3(.75f,1.3f,.75f));
                Add(root.transform,"Lintel",beam,stone,new Vector3(0f,6.1f,0f),new Vector3(17.3f,.5f,1.4f));
                Add(root.transform,"Gilt lintel",beam,trim,new Vector3(0f,6.5f,0f),new Vector3(17.3f,.09f,1.5f));
                Add(root.transform,"Crown plinth",beam,stone,new Vector3(0f,8.5f,0f),new Vector3(12f,1f,2.0f));
                Add(root.transform,"Amethyst crown",shard,violet,new Vector3(0f,8.8f,0f),new Vector3(2.5f,2.2f,2.5f));
                Add(root.transform,"Crown flank L",shard,cyan,new Vector3(-5f,7.3f,0f),new Vector3(1.2f,1.4f,1.2f));
                Add(root.transform,"Crown flank R",shard,cyan,new Vector3(5f,7.3f,0f),new Vector3(1.2f,1.4f,1.2f));
                // Pointed arch silhouettes: diagonal custom-mesh struts.
                for (int a=0;a<2;a++)
                {
                    float center=a==0?-4f:4f;
                    Add(root.transform,"Arch L "+a,beam,trim,new Vector3(center-1.8f,4.65f,-.7f),
                        new Vector3(.35f,3.9f,.5f),Quaternion.Euler(0,0,-31f));
                    Add(root.transform,"Arch R "+a,beam,trim,new Vector3(center+1.8f,4.65f,-.7f),
                        new Vector3(.35f,3.9f,.5f),Quaternion.Euler(0,0,31f));
                }
                // Distinctive crystalline flora, not sphere or cube foliage.
                for (int i=0;i<4;i++)
                {
                    float x = -10.5f+i*7f;
                    Add(root.transform,"Bark "+i,trunk,stone,new Vector3(x,0f,2.6f),Vector3.one);
                    for (int j=0;j<3;j++)
                        Add(root.transform,"Canopy "+i+"."+j,shard,j==1?cyan:foliage,
                            new Vector3(x+(j-1)*.7f,3.2f,2.6f),new Vector3(1.1f,1.1f,1.1f),
                            Quaternion.Euler((j-1)*12f,j*48f,(j-1)*24f));
                }
                // No colliders, triggers, AI, lights or full-screen effects.
                PrefabUtility.SaveAsPrefabAsset(root,PrefabDir+"/CG_ProofBlock.prefab");
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Crystal Gothic] Generated reusable mesh/material assets and CG_ProofBlock.prefab. " +
                "Use the placement menu to preview in your scene.");
        }

        [MenuItem("Neon Syndicate/Art/Place Crystal Gothic Proof Block In Scene")]
        public static void Place()
        {
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir+"/CG_ProofBlock.prefab");
            if (prefab==null) { Build(); prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir+"/CG_ProofBlock.prefab"); }
            if (prefab==null) { Debug.LogError("Crystal Gothic proof prefab missing."); return; }
            GameObject instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            // Place at SceneView pivot so designers can position beside a real facade.
            SceneView view=SceneView.lastActiveSceneView;
            instance.transform.position=view!=null?view.pivot:Vector3.zero;
            Undo.RegisterCreatedObjectUndo(instance,"Place Crystal Gothic proof block");
            Selection.activeGameObject=instance;
            Debug.Log("[Crystal Gothic] Visual-only proof placed. Move it alongside an existing building, " +
                "inspect from street level, and save the scene if approved.");
        }

        private static void EnsureFolder(string parent,string name)
        {
            if(!AssetDatabase.IsValidFolder(parent+"/"+name)) AssetDatabase.CreateFolder(parent,name);
        }

        private static Material MakeMaterial(string name,Color color,float smooth,bool emissive)
        {
            string path=MatDir+"/"+name+".mat";
            Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                Shader shader=Shader.Find("Universal Render Pipeline/Lit");
                if(shader==null) shader=Shader.Find("Standard");
                if(shader==null) throw new System.InvalidOperationException("No compatible Lit shader found.");
                material=new Material(shader);
                AssetDatabase.CreateAsset(material,path);
            }
            if(material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
            if(material.HasProperty("_Color")) material.SetColor("_Color",color);
            if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",smooth);
            if(emissive)
            {
                material.EnableKeyword("_EMISSION");
                if(material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor",color*1.8f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh SaveMesh(string name,Mesh mesh)
        {
            string path=MeshDir+"/"+name+".asset";
            Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing!=null) AssetDatabase.DeleteAsset(path);
            mesh.name=name;
            AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }

        private static GameObject Add(Transform parent,string name,Mesh mesh,Material mat,
            Vector3 position,Vector3 scale,Quaternion? rotation=null)
        {
            GameObject obj=new GameObject(name);
            obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;
            obj.transform.localRotation=rotation??Quaternion.identity;
            obj.transform.localScale=scale;
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            MeshRenderer renderer=obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=mat;
            renderer.shadowCastingMode=ShadowCastingMode.On;
            return obj;
        }

        private static Mesh Rings(int sides,Vector2[] profile)
        {
            var verts=new List<Vector3>();
            var uvs=new List<Vector2>();
            var triangles=new List<int>();
            for(int i=0;i<profile.Length;i++)
                for(int s=0;s<sides;s++)
                {
                    float a=2f*Mathf.PI*s/sides;
                    verts.Add(new Vector3(Mathf.Cos(a)*profile[i].y,profile[i].x,Mathf.Sin(a)*profile[i].y));
                    uvs.Add(new Vector2(s/(float)sides,profile[i].x/5f));
                }
            for(int i=0;i<profile.Length-1;i++)
                for(int s=0;s<sides;s++)
                {
                    int a=i*sides+s,b=i*sides+(s+1)%sides,c=(i+1)*sides+s,d=(i+1)*sides+(s+1)%sides;
                    triangles.Add(a);triangles.Add(c);triangles.Add(b);
                    triangles.Add(b);triangles.Add(c);triangles.Add(d);
                }
            for(int s=1;s<sides-1;s++)
            {
                triangles.Add(0);triangles.Add(s);triangles.Add(s+1);
                int top=(profile.Length-1)*sides;
                triangles.Add(top);triangles.Add(top+s+1);triangles.Add(top+s);
            }
            Mesh m=new Mesh();
            m.SetVertices(verts);m.SetUVs(0,uvs);m.SetTriangles(triangles,0);
            m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        private static Mesh BoxMesh()
        {
            // Explicit vertex geometry, not GameObject.CreatePrimitive.
            Vector3[] p={new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),
                new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),
                new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),
                new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
            int[] t={0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,
                3,7,6,3,6,2,1,2,6,1,6,5,0,4,7,0,7,3};
            Mesh m=new Mesh { vertices=p,triangles=t };
            m.RecalculateNormals();m.RecalculateBounds();return m;
        }
    }
}
#endif
