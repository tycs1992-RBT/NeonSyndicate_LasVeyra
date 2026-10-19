using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonSyndicate
{
    /// <summary>Reload the existing scene to change topology. Never apply saved coordinates to another revision.</summary>
    public static class CityRebirthBoot04O
    {
        private static bool _pending;
        private static CityRevision04O _revision;
        private static SaveData _save;
        private static bool _free, _skip;
        public static void ResetStatics() { _pending=false;_revision=CityRevision04O.Legacy04N;_save=null;_free=false;_skip=false; }
        public static bool Take(out CityRevision04O revision,out SaveData save,out bool free,out bool skip)
        {
            revision=_revision;save=_save;free=_free;skip=_skip;bool pending=_pending;ResetStatics();return pending;
        }
        public static bool Request(CityRevision04O revision,SaveData save,bool free,bool skip=false)
        {
            if(_pending || !CityRebirthRules04O.Known((int)revision)) return false;
            if(save!=null && save.WorldRevision04O!=(int)revision) return false;
            Scene scene=SceneManager.GetActiveScene();
            if(!scene.IsValid() || string.IsNullOrEmpty(scene.path)) return false;
            GameState previous=Game.State;bool blocked=GInput.GameplayBlocked;
            _pending=true;_revision=revision;_save=save;_free=free;_skip=skip;
            try
            {
                GInput.GameplayBlocked=true;Game.State=GameState.Loading;
                SceneManager.LoadScene(scene.path,LoadSceneMode.Single);
                return true;
            }
            catch(Exception error)
            {
                ResetStatics();Game.State=previous;GInput.GameplayBlocked=blocked;
                if(Game.Hud!=null)Game.Hud.Notify("The city preview could not reload this scene. Your saves are unchanged.",7f);
                Log.Exception("City revision reload",error);return false;
            }
        }
    }
}
