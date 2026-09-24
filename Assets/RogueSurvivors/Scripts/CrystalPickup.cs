using UnityEngine;
namespace RogueSurvivors
{
    // 反応を起こしたプレイヤー専用。保持も地面のアイテムも一個まで。
    public sealed class CrystalPickup : MonoBehaviour
    {
        PlayerHealth owner; float life=18; LineRenderer gem; static Material material;
        public static CrystalPickup Spawn(Vector2 point,PlayerHealth player)
        {
            if(!player || player.HasBarrier) return null;
            foreach(var old in FindObjectsByType<CrystalPickup>(FindObjectsSortMode.None)) if(old.owner==player) return old;
            var pickup=new GameObject("結晶：拾うと一撃無効").AddComponent<CrystalPickup>(); pickup.owner=player; pickup.transform.position=point;
            if(!material) material=new Material(Shader.Find("Sprites/Default"));
            pickup.gem=pickup.gameObject.AddComponent<LineRenderer>(); pickup.gem.sharedMaterial=material; pickup.gem.useWorldSpace=false;
            pickup.gem.sortingLayerName="Items"; pickup.gem.loop=true; pickup.gem.startWidth=pickup.gem.endWidth=.13f;
            pickup.gem.startColor=pickup.gem.endColor=new Color(1,.85f,.25f); pickup.gem.positionCount=4;
            pickup.gem.SetPositions(new[]{new Vector3(0,.48f),new Vector3(.3f,0),new Vector3(0,-.48f),new Vector3(-.3f,0)});
            CombatFx.Ring(point,.55f,new Color(1,.8f,.25f),.8f,1);
            return pickup;
        }
        void Update()
        {
            life-=Time.deltaTime; if(life<=0 || !owner || owner.HasBarrier) { Destroy(gameObject); return; }
            transform.rotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time*3)*12);
            if(!owner.IsLocal || !owner.Alive) return;
            float distance=Vector2.Distance(transform.position,owner.transform.position);
            if(distance<owner.GetComponent<PlayerStats>().PickupRadius) transform.position=Vector3.MoveTowards(transform.position,owner.transform.position,8*Time.deltaTime);
            if(distance<.65f) { owner.GrantBarrier(); Destroy(gameObject); }
        }
    }
}
