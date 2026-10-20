using System;
using System.IO;
namespace NeonSyndicate
{
    /// <summary>Pure evidence and import policy. An authored checklist never implies native acceptance.</summary>
    public static class CinematicAcceptance04OF
    {
        public static void ResetStatics() { }
        public static string Label(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return "capture";
            System.Text.StringBuilder result=new System.Text.StringBuilder();
            foreach(char c in value.Trim())
            {if(result.Length>=64)break;result.Append(char.IsLetterOrDigit(c) || c=='-' || c=='_'?c:'_');}
            return result.ToString();
        }
        public static bool SafeEvidenceFile(string relative)
        {
            if(string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains(":") || relative.Contains(".."))return false;
            string name=Path.GetFileName(relative).ToLowerInvariant();
            if(name.Contains("save") || name.EndsWith(".bak"))return false;
            string extension=Path.GetExtension(name);
            return extension==".png" || extension==".json" || extension==".xml" || extension==".csv" || extension==".txt" || extension==".log" || extension==".md";
        }
        public static bool RelativeBonePath(string path)
        {
            if(string.IsNullOrWhiteSpace(path) || path.StartsWith("/") || path.EndsWith("/") || path.Contains("\\") || path.Contains(":"))return false;
            foreach(string part in path.Split('/'))if(part=="" || part=="." || part=="..")return false;
            return true;
        }
        public static string ProfileWarning(int revision)
        {return revision==6?"Revision 6: Living Frontier 04O-G. Isolated frontier bank; older worlds have not migrated.":revision==5?"Revision 5: existing Unbound preview. No save migration.":"WRONG PREVIEW for 04OF regional acceptance. Select Title > Unbound 04O-E explicitly; older banks stay protected.";}
        public static string[] Checklist()
        {
            return new[]{"Malik front / side / ordinary attack / grip", "Dante service and Archive return", "Castellan phase 1 / 2 / 3 and broken parts", "Castellan retreat / retry / one victory", "Pending reward with full pack and favorite protection", "Permanent blade equip / paid Forge level / coat", "Temporary treatment replacement and expiration", "Save / reload before claim and after equip", "Sovereign tell / recovery / broken parts / reward", "Forest / raised walk / cave / safe return", "Earned companion art: correct / missed / interrupted", "Pause / death / travel / camera restoration", "Keyboard and controller / gun / dodge / flight / vehicle", "Day / night / Low / High / original bank campaign", "Native profiler traversal and active fight", "Windows development build and launch", "04OG ward frontal / flank / heavy break / shared turns", "04OG elevated gallery / witness passage / return", "04OG ravine upper / lower / cavern junction / vein", "04OG calm wildlife forage / watch / return / storm shelter", "04OG Dante field commissions / once-only materials / bank 6", "04OH original calibration import / audit, not hero art acceptance", "04OH actual candidate license / hash / safe fallback", "04OH one animation authority / weapons / flight / vehicle", "04OH matched route art / day / night / hardware context"};
        }
    }
}
