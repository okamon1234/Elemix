using UnityEngine;
using UnityEngine.InputSystem;
namespace RogueSurvivors
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerStats))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        Rigidbody2D body;
        PlayerStats stats;
        PlayerHealth health;
        Animator animator;
        SpriteRenderer sprite;
        Vector2 input;
        void Awake()
        {
            body = GetComponent<Rigidbody2D>(); stats = GetComponent<PlayerStats>();
            health = GetComponent<PlayerHealth>(); animator = GetComponentInChildren<Animator>();
            sprite = GetComponentInChildren<SpriteRenderer>();
        }
        void Update()
        {
            input = Vector2.zero;
            if (!health.Alive || (GameManager.Instance && !GameManager.Instance.IsPlaying)) return;
            var k = Keyboard.current;
            if (k != null)
                input = new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) -
                    (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) -
                    (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
            if (Gamepad.current != null && Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f)
                input = Gamepad.current.leftStick.ReadValue();
            input = Vector2.ClampMagnitude(input, 1);
            if (animator) animator.SetBool("IsMoving", input.sqrMagnitude > 0.01f);
            if (sprite && Mathf.Abs(input.x) > 0.01f) sprite.flipX = input.x < 0;
        }
        void FixedUpdate() => body.linearVelocity = health.Alive ? input * stats.MoveSpeed : Vector2.zero;
        void OnDisable() { if (body) body.linearVelocity = Vector2.zero; }
    }
}
