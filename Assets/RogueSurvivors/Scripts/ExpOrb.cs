using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class ExpOrb : MonoBehaviour
    {
        public static readonly HashSet<ExpOrb> Active = new HashSet<ExpOrb>();
        public int value = 2;
        PlayerHealth target;
        float speed = 3;
        bool collected;
        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);
        public void Attract(PlayerHealth player) { if (!collected) target = player; }
        void Update()
        {
            if (!target || !target.Alive || collected) return;
            speed += Time.deltaTime * 15;
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
            if ((transform.position - target.transform.position).sqrMagnitude > .25f) return;
            collected = true;
            target.GetComponent<PlayerStats>().AddExperience(value);
            EffectsService.Instance?.Play("Pickup", .12f);
            Destroy(gameObject);
        }
    }
}
