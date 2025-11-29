using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemCallback : MonoBehaviour
{
    private void OnParticleSystemStopped() => SC_GameLogic.Instance.DestroyEffectSpawner.Despawn(transform);
}
