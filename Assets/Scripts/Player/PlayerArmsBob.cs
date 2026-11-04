using UnityEngine;

namespace Espionage.Player
{
    /// <summary>
    /// A gentle walking sway for the first-person arms. Purely visual: reads how fast the player body moves
    /// and nudges this object, so it has no effect on gameplay.
    /// </summary>
    public sealed class PlayerArmsBob : MonoBehaviour
    {
        [SerializeField] float stepRate = 7f;
        [SerializeField] float verticalAmount = 0.012f;
        [SerializeField] float sideAmount = 0.008f;
        [SerializeField] float smoothing = 10f;

        CharacterController body;
        Vector3 restPosition;
        float phase;

        void Awake()
        {
            body = GetComponentInParent<CharacterController>();
            restPosition = transform.localPosition;
        }

        void Update()
        {
            Vector3 velocity = body != null ? body.velocity : Vector3.zero;
            velocity.y = 0f;
            float speed = velocity.magnitude;
            bool walking = body != null && body.isGrounded && speed > 0.1f;

            Vector3 target = restPosition;
            if (walking)
            {
                phase += Time.deltaTime * stepRate * Mathf.Clamp(speed / 4f, 0.6f, 1.7f);
                float strength = Mathf.Clamp(speed / 4f, 0.6f, 1.6f);
                target += new Vector3(
                    Mathf.Sin(phase * 0.5f) * sideAmount * strength,
                    -Mathf.Abs(Mathf.Sin(phase)) * verticalAmount * strength,
                    0f);
            }

            transform.localPosition = Vector3.Lerp(transform.localPosition, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }
    }
}
