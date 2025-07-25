using UnityEngine;

namespace Portfolio.InputSystem
{
    [CreateAssetMenu(fileName = "SpeedBoostSettings", menuName = "Player/Speed Boost Settings")]
    public class SpeedBoostSettings : ScriptableObject
    {
        [Header("Boost Configuration")]
        [Tooltip("Duration of the boost effect in seconds")]
        public float boostDuration = 2f;

        [Tooltip("Distance the boost will cover")]
        public float boostDistance = 10f;

        [Tooltip("Speed multiplier during boost")]
        [Range(1f, 5f)]
        public float speedMultiplier = 2f;

        [Header("Cooldown Settings")]
        [Tooltip("Cooldown time before boost can be used again")]
        public float cooldownTime = 5f;

        [Tooltip("Pause duration before boost starts")]
        public float preBoostPauseDuration = 0.2f;

        [Tooltip("Pause duration after boost ends")]
        public float postBoostPauseDuration = 0.15f;

        [Header("Input Block Settings")]
        [Tooltip("How long to block input after boost activation")]
        public float inputBlockDuration = 0.5f;

        [Header("Animation Settings")]
        [Tooltip("Animation curve for boost speed over time")]
        public AnimationCurve boostCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Audio Settings")]
        [Tooltip("Audio clip to play when boost is activated")]
        public AudioClip boostActivationSound;

        [Tooltip("Audio clip to play during boost")]
        public AudioClip boostLoopSound;

        [Header("Visual Effects")]
        [Tooltip("Particle effect to play during boost")]
        public GameObject boostParticleEffect;

        [Tooltip("Screen shake intensity during boost")]
        [Range(0f, 1f)]
        public float screenShakeIntensity = 0.3f;

        [Header("Debug")]
        [Tooltip("Enable debug logging for boost system")]
        public bool enableDebugLogging = false;
    }

    [System.Serializable]
    public struct BoostData
    {
        public Vector3 direction;
        public float duration;
        public float distance;
        public float speedMultiplier;
        public bool inputBlocked;
        public float preBoostPause;
        public float postBoostPause;

        public BoostData(Vector3 boostDirection, float boostDuration, float boostDistance, float multiplier, bool blockInput = true, float prePause = 0.2f, float postPause = 0.15f)
        {
            direction = boostDirection;
            duration = boostDuration;
            distance = boostDistance;
            speedMultiplier = multiplier;
            inputBlocked = blockInput;
            preBoostPause = prePause;
            postBoostPause = postPause;
        }

        public static BoostData CreateFromSettings(SpeedBoostSettings settings, Vector3 direction)
        {
            return new BoostData(
                direction,
                settings.boostDuration,
                settings.boostDistance,
                settings.speedMultiplier,
                true,
                settings.preBoostPauseDuration,
                settings.postBoostPauseDuration
            );
        }
    }
}