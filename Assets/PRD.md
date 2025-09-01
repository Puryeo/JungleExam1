
[PRD] Bouncy Cube Slayer – Phase 1 Control Implementation & Game‑Feel Verification
=================================================================================

Document : Input-System-Based Player Controls & Game‑Feel Verification Specs
Version  : 1.2 (Revision for Unity 6.1 prototyping)
Date     : 2025‑08‑02
Author   : Gemini
Goal     : Using **Unity 6.1 + Input System** implement the core **Auto‑Bounce/Slam** control loop, then confirm – via quantitative & qualitative checkpoints – that the target *game feel* is achieved, all within a **12 h** prototype window (first **4–5 h** for controls + feedback).

---------------------------------------------------------------------
1. Overview
---------------------------------------------------------------------

1.1 Feature Goal
• Under the **Auto‑Bounce** constraint the player performs all actions with **WASD + Spacebar**.  
• Emphasise the **power & reward** of the **Slam** action (strong impact → Enhanced Bounce).

1.2 Control Philosophy
┌───────────────┬───────────────────────────────────────────────────────────────┐
│ Event‑Driven  │ Use `PlayerInput` (Invoke Unity Events) for low‑latency       │
│ Input         │ reactions & clean code.                                       │
├───────────────┼───────────────────────────────────────────────────────────────┤
│ Minimal Input │ Only two inputs drive every core mechanic.                    │
│ / Max Depth   │                                                               │
├───────────────┼───────────────────────────────────────────────────────────────┤
│ Clear Role    │ *Normal Bounce* → mobility • *Slam* → offense / flow reset    │
│ Split         │                                                               │
└───────────────┴───────────────────────────────────────────────────────────────┘

---------------------------------------------------------------------
2. Input System Setup & Implementation Specs
---------------------------------------------------------------------

2.1 Action Map
┌────────┬─────┬───────────┬─────────────────────────────────────────┐
│ Action │Type │ Value     │ Bindings                                │
├────────┼─────┼───────────┼─────────────────────────────────────────┤
│ Move   │Value│ Vector2   │ WASD , **Left Stick**                   │
│ Slam   │Button│ bool     │ Spacebar , **South Button**             │
└────────┴─────┴───────────┴─────────────────────────────────────────┘

2.2 Script Skeleton (PlayerController.cs)

// Prototype Only – Delete or replace in production
using UnityEngine;
using UnityEngine.InputSystem;

public class forgamedesign03_PlayerController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField, Tooltip("Rigidbody attached to the player")]
    private Rigidbody rb;

    #region Movement — Vibe Tweaks
    [SerializeField, Tooltip("Horizontal move force per input step")]
    private float moveForce = 20f;
    #endregion

    #region Slam — Vibe Tweaks
    [SerializeField, Tooltip("Downward slam impulse")]
    private float slamForce = 25f;
    private bool canSlam = true;
    #endregion

    /* Input Events ------------------------------------------------- */
    public void OnMove(InputValue v)
    {
        Vector2 dir = v.Get<Vector2>();
        rb.AddForce(new Vector3(dir.x, 0, dir.y) * moveForce,
                    ForceMode.Acceleration);
    }

    public void OnSlam(InputValue _)
    {
        if (!canSlam) return;
        canSlam = false;
        rb.AddForce(Vector3.down * slamForce, ForceMode.Impulse);
        // trigger slam VFX/SFX here
    }

    /* Collision ---------------------------------------------------- */
    private void OnCollisionEnter(Collision c)
    {
        if (!c.collider.CompareTag("Bouncable")) return;
        // determine bounce type by tag / contact point
        Bounce(BounceType.Normal);   // placeholder
        canSlam = true;
    }

    private void Bounce(BounceType type)
    {
        float coeff = GameFeel.BounceCoefficients[type];
        rb.velocity = new Vector3(
            rb.velocity.x,
            Mathf.Sqrt(2 * Physics.gravity.magnitude * coeff),
            rb.velocity.z);
    }

    private enum BounceType { Normal, Enhanced, Strategic }
}

2.3 Physics Parameters (YAML)

bounceCoefficients:
  normal:    1.0
  enhanced:  1.5
  strategic: 1.2
slamForce:   25.0
gravityScale: 1.8

2.4 Feedback Parameters (Game‑Feel)

hitStopSec:           0.10
camShake:
  durationSec:        0.20
  amplitude:          0.15
sfx:
  slamHit:            Slam_Big.wav
  bounceEnhanced:     Bounce_Enhanced.wav
particles:
  slamImpactPrefab:   VFX_SlamImpact.prefab

---------------------------------------------------------------------
3. Physics & Interaction Specs
---------------------------------------------------------------------

┌────────────────────┬────────────────────────────────────┬───────────────────────────────────────────┐
│ 이벤트              │ 조건                               │ 결과                                      │
├────────────────────┼────────────────────────────────────┼───────────────────────────────────────────┤
│ Normal Bounce      │ Collide ground (Bouncable)         │ bounceCoefficients.normal                 │
│ Enhanced Bounce    │ Slam hits zombie top → kill        │ bounceCoefficients.enhanced + TimeWarp0.8 │
│ Strategic Bounce   │ Normal hit zombie top (no kill)    │ bounceCoefficients.strategic              │
│ Slam Failure       │ Collide zombie side/bottom         │ Player damage + knock‑back                │
└────────────────────┴────────────────────────────────────┴───────────────────────────────────────────┘

Slam Cool‑down — Player must register ≥1 bounce before canSlam resets.

---------------------------------------------------------------------
4. Verification Goals & Metrics
---------------------------------------------------------------------

4.1 Qualitative Questions
A) Responsiveness  B) Slam Impact  C) Bounce Pleasure  D) Predictability

4.2 Quantitative Criteria
Metric                Target           Measure
Input Latency         ≤50 ms           Profiler Timeline / realtime diff
Hit‑Stop Accuracy     0.10±0.01 s      Time.timeScale log
Enhanced Bounce ΔH    ≥0.8 m           rb.position.y apex
FPS (Editor)          ≥60              Stats overlay

---------------------------------------------------------------------
5. Minimal Test Environment
---------------------------------------------------------------------

scenePath : Assets/Scenes/_AITemp/Test_BouncyCube.unity
prefabs   : fgd03_Player, fgd03_Zombie, BouncablePlatform
camera    : CinemachineVirtualCamera (follow, dist = 8)
hot‑keys  : F1 ResetSlam  F2 ToggleHitStop/CamShake
gitignore : _AITemp/ excluded

---------------------------------------------------------------------
6. Implementation Priorities & Time‑Box
---------------------------------------------------------------------

P1   Input setup                         0‑0.5h
P2‑1 Movement + Normal Bounce           +1.0h
P2‑2 Slam + Enhanced Bounce             +1.0h
P3‑1 HitStop / CamShake / SFX           +1.0h
P3‑2 Tuning to metrics                  +1.0h
Buf  Debug / Playtest                   +0.5h      → Total ≈5h

---------------------------------------------------------------------
7. Risks & Mitigations
---------------------------------------------------------------------

CamShake/VFX delay → placeholder color‑flash
SFX missing → built‑in clip
Input package clash → pin 1.7.0
Inspector clutter → group= param in @AI:VIBE_TWEAK

---------------------------------------------------------------------
8. Cursor AI Integration Blocks
---------------------------------------------------------------------

physicsParams:
  bounce: { normal:1.0, enhanced:1.5, strategic:1.2 }
  slamForce: 25.0
  gravityScale: 1.8

metrics:
  latencyMaxMs: 50
  hitStopSec:   0.10
  bounceDeltaM: 0.8

Cursor AI must follow folder path, file/class prefix forgamedesign03_, and Korean comment/tooltip rules.

---------------------------------------------------------------------
End of PRD v1.2