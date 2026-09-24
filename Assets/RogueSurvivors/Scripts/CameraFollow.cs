using UnityEngine;
namespace RogueSurvivors
{
    public sealed class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }
        public Transform target;
        public Transform grid;
        public bool arena;
        float shake;
        Vector3 smoothPosition;
        void OnEnable() => Instance = this;
        void Awake() { Instance = this; smoothPosition = transform.position; }
        public void Shake(float strength) => shake = Mathf.Max(shake, strength);
        void LateUpdate()
        {
            var cameraComponent = GetComponent<Camera>();
            if (arena) cameraComponent.orthographicSize = 10;
            Vector3 desired = target ? target.position : Vector3.zero;
            if (arena) {
                float horizontal = Mathf.Max(0, BossArena.HalfWidth - cameraComponent.orthographicSize * cameraComponent.aspect);
                float vertical = BossArena.HalfHeight - cameraComponent.orthographicSize;
                desired.x = Mathf.Clamp(desired.x, -horizontal, horizontal); desired.y = Mathf.Clamp(desired.y, -vertical, vertical);
            }
            desired.z = -10;
            smoothPosition = Vector3.Lerp(smoothPosition, desired, 1 - Mathf.Exp(-8 * Time.deltaTime));
            transform.position = smoothPosition + (Vector3)(Random.insideUnitCircle * shake);
            shake = Mathf.MoveTowards(shake, 0, Time.deltaTime);
            if (grid) grid.position = new Vector3(Mathf.Floor(smoothPosition.x), Mathf.Floor(smoothPosition.y), 0);
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
