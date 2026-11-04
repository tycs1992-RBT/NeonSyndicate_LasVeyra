"""The only box-collider exception in the WR02B continuation is the paired freight model."""

ROOT = 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/'
VIEW = ROOT+'ForgeFreightView.cs'
OWNER = ROOT+'TrafficManager.ForgeFreight.cs'
BOX = 'AddComponent<BoxCollider>'


def freight_boxes(view, owner):
    contacts = [
        'view.Box(ForgeFreightModel.Body,new Vector3(0,1.53f,-.03f),new Vector3(1.48f,1.40f,2.75f));',
        'view.Box(ForgeFreightModel.Head,new Vector3(0,.15f,.40f),new Vector3(.85f,.68f,1.12f));',
        'view.Box(ForgeFreightModel.Wagon,new Vector3(0,.97f,0),new Vector3(1.72f,.85f,2.68f));',
        'view.Box(ForgeFreightModel.Wagon,new Vector3(0,.86f,1.87f),new Vector3(.22f,.16f,1.46f));',
        'contact.transform.SetParent(view._joints[i],false)',
        'contact.AddComponent<BoxCollider>().size=new Vector3(.15f,.084f,.233f)',
        '_joints[joint].gameObject.AddComponent<BoxCollider>();collider.center=centre;collider.size=size;',
        'root.SetActive(false);root.transform.SetParent(parent,false)',
        'ForgeFreightEnvelope.Animal(frame)', 'ForgeFreightEnvelope.Wagon(frame)',
    ]
    transaction = [
        '_shell.gameObject.SetActive(active);gameObject.SetActive(active)',
        'if(_freightTeam!=null)return;',
        'VehicleDb.Find("Mistral Solace")', 'if(!prepared.Ground(ref start))',
        'if(!prepared.EnvelopeFree(start,van))', 'prepared.Show(_freightRun,start,0,0)',
        '_freightLease.Select(true,true,false,van.SetFreightSourceActive,prepared.gameObject.SetActive)',
        '_freightLease.Select(false,true,!clear,',
        '_freightTeam.VanFree(at,dir,_freightVan)', 'FantasyPreviewV1 && WorldRewriteSelectedWR01',
        'van.HasPlayer || van.StolenByPlayer || van.MissionCritical || van.IsPolice',
        'ClearFreightPost();RetireFreight();',
    ]
    return (view.count(BOX) == 2 and view.count('view.Box(') == 4 and all(s in view for s in contacts)
            and all(s in owner for s in transaction)
            and owner.index('prepared.Show(_freightRun,start,0,0)') < owner.index('_freightLease.Select(true,true,false'))


def verify(root, paths):
    view, owner = [(root/p).read_text() for p in (VIEW, OWNER)]
    if any(BOX in (root/path).read_text() for path in paths if path != VIEW and '/Scripts/' in path):
        return False
    return (freight_boxes(view, owner) and not freight_boxes(view+'\nnode.AddComponent<BoxCollider>();', owner)
            and not freight_boxes(view.replace('view.Box(ForgeFreightModel.Head','view.Box(ForgeFreightModel.Wagon'), owner)
            and not freight_boxes(view, owner.replace('_shell.gameObject.SetActive(active);',''))
            and not freight_boxes(view, owner.replace('_freightLease.Select(false,true,!clear,','_freightLease.Select(false,true,false,')))
