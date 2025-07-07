using UnityEngine;

public class LandingParticleManager : MonoBehaviour
{
    [Header("Particle Systems")]
    public ParticleSystem dustCloud;

    public ParticleSystem rockDebris;
    public ParticleSystem energyBurst;

    public void TriggerLandingParticles()
    {
        // Dust cloud
        var dustMain = dustCloud.main;
        dustMain.startLifetime = 3f;
        dustMain.startSpeed = 15f;
        dustMain.startSize = 2f;
        dustCloud.Play();

        // Rock debris
        var rockMain = rockDebris.main;
        rockMain.startLifetime = 2f;
        rockMain.startSpeed = 8f;
        rockDebris.Play();

        // Energy burst
        var energyMain = energyBurst.main;
        energyMain.startLifetime = 1f;
        energyMain.startSpeed = 20f;
        energyBurst.Play();
    }
}