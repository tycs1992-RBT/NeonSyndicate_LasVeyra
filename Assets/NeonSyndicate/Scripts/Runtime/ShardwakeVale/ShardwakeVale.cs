using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonSyndicate
{
    /// <summary>Scene-owned starting zone. All gameplay locations are editable scene references.</summary>
    [DisallowMultipleComponent]
    public sealed partial class ShardwakeVale : MonoBehaviour
    {
        public Transform spawn, dante, bridge, refuge, landmark, pickup;
        public Transform[] sockets;
        public GameObject forgeHeart, forgeFire;
        public Camera view;

        public static bool Owns(Scene scene)
        {
            if(!scene.IsValid() || !scene.isLoaded)return false;
            foreach(GameObject root in scene.GetRootGameObjects())
                if(root.GetComponent<ShardwakeVale>()!=null)return true;
            return false;
        }
    }
}
