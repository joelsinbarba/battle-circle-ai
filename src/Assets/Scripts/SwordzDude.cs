using UnityEngine;
using System.Collections;

public class DudeState {
    public bool dodging = false;
    public bool dodgingRecovery = false;
    public bool blocking = false;
    public bool attacking = false;
    public bool attackingRecovery = false;
    public bool stunned = false;
    public bool running = false;
    public bool unbalanced = false;

    public void Reset() {
        dodging = false;
        dodgingRecovery = false;
        blocking = false;
        attacking = false;
        attackingRecovery = false;
        stunned = false;
        running = false;
        unbalanced = false;
    }
}

[AddComponentMenu("DudeWorld/Swordz Dude")]
public class SwordzDude : MonoBehaviour {

    public GameObject baseAbilities;
    public float maxStamina = 30.0f;
    public Transform animated;
    public Transform weaponHand;
    public Transform offHand;
    public float followUpWindow = 0.2f;
    public bool telegraphAttacks = false;
    public bool cannotBeStunned = false;
    public float stunResistance = 0.0f;
    public GameObject telegraphDecal;
    public float telegraphRate = 0.6f;
    public DudeState status = new DudeState();

    private Dude dude;
    private DudeController controller;
    private DudeAttack attackChain;
    private float followUpTimer = 0.0f;
    private float attackCooldown = 0.0f;
    private float attackDelay = 0.0f;
    private float stamina;

    private float attackCancelWindow = 0.25f;
    private const float attackAnimationRatio = 1.0f;
    private const float defaultStun = 2.0f;

    private float blockStartTime = 0.0f;
    private Transform blockDecal;
    private Transform chargeDecal;
    private bool disabled = false;
    private bool cancelable = false;
    private Transform head;
    private Transform hotspot;
    private Transform mount;
    private Transform activeTelegraph = null;
    private GameObject circlePrefab;

    private Color originalTelegraphColor;

    public int swingNumber { get; private set; }

    public bool followUpQueued { get; private set; }

    void Awake() {    
        dude = GetComponent<Dude>();
        attackChain = GetComponent<DudeAttack>();
        mount = weaponHand;
        hotspot = transform.Find("weapon_hotspot");

        swingNumber = 0;
        followUpQueued = false;
    }

    void Start() {
        controller = GetComponent<DudeController>();
        blockDecal = transform.Find("blockDecal");
        chargeDecal = transform.Find("chargeDecal");

        circlePrefab = Resources.Load("Effects/effect_telegraphCircle") as GameObject;
        
        if (blockDecal != null) blockDecal.gameObject.SetActive(false);
        if (chargeDecal != null) chargeDecal.gameObject.SetActive(false);
        
        stamina = maxStamina;
        head = animated.Find("torso/head");

        if (telegraphAttacks && telegraphDecal != null) {
            var clone = Instantiate(telegraphDecal, hotspot.position, hotspot.rotation) as GameObject;
            clone.transform.parent = transform;
            activeTelegraph = clone.transform;

            originalTelegraphColor = activeTelegraph.GetComponentInChildren<MeshRenderer>().sharedMaterial.GetColor("_TintColor");
            activeTelegraph.gameObject.SetActive(false);
        }
    }

    void FixedUpdate() {
        if (status.dodging && !status.dodgingRecovery) {
            //dude.RawMovement(transform.forward * dodgeSpeed, false);
        }

        if (followUpTimer > 0.0f) followUpTimer -= Time.fixedDeltaTime;
        if (attackCooldown > 0.0f) attackCooldown -= Time.fixedDeltaTime;

        float staminaRecovery = 1.0f;
        if (stamina < maxStamina) {
            stamina += Time.fixedDeltaTime * staminaRecovery;
            if (stamina > maxStamina) stamina = maxStamina;
        }
    }

    public void OnShot(DamageEvent d) {
        if (dude != null && d.team == dude.team) return;

        stamina -= d.knockback;
        if (stamina <= 0) {
            //StartCoroutine("OnStun", 3.0f);
        }
    }

    public void OnBlock() {
        if (status.dodging) dude.velocity = Vector3.zero;

        status.blocking = true;
        blockStartTime = Time.time;
        blockDecal.gameObject.SetActive(true);
        dude.blocking = true;
    }

    public void OnBlockEnd() {
        if (status.dodging || status.running || status.attacking) return;

        status.blocking = false;
        blockStartTime = 0.0f;
        blockDecal.gameObject.SetActive(false);
        dude.blocking = false;
    }

    public bool OnAttack() {
        if (dude.blocking || status.stunned || attackCooldown > 0.0f) return false;
        if (status.dodging) dude.velocity = Vector3.zero;

        int maxSwings = 3;

        if (disabled && !status.stunned && (status.attacking || status.attackingRecovery || status.dodging)) {
            if (swingNumber + 1 < maxSwings) followUpQueued = true;
            else followUpQueued = false;
            return false;
        }

        if (swingNumber <= maxSwings /*&& alwaysFollowUp*/) followUpQueued = true;

        if (followUpTimer <= 0.0f) {
            swingNumber = 0;
            followUpTimer = followUpWindow + 0.001f;
        }

        if (followUpTimer > 0.0f && swingNumber < maxSwings) {
            swingNumber += 1;
            SwingEvent swing = attackChain.comboChain[0];
            StartCoroutine("doSwing", swing);

            if (swingNumber < maxSwings) {
                followUpTimer = swing.rate + followUpWindow;
            } else {
                swingNumber = 0;
                followUpTimer = 0.0f;
                followUpQueued = false;
                attackCooldown = attackDelay;
            }
            return true;
        }

        return false;
    }
    public void OnCancel()
    {
        this.followUpQueued = false;
    }
    IEnumerator doSwing(SwingEvent swingEvent) {
        if (status.dodging) status.dodging = false;

        status.attacking = true;
        gameObject.BroadcastMessage("OnDisable");

        if (telegraphAttacks && swingNumber <= 1) {
            animated.GetComponent<Animation>().Play(swingEvent.animation.name + "_telegraph");

            activeTelegraph.gameObject.SetActive(true);
            yield return new WaitForSeconds(telegraphRate - 0.2f);
            foreach (Transform decalChild in activeTelegraph) {
                decalChild.GetComponent<Renderer>().material.SetColor("_TintColor", Color.white);
            }
            status.unbalanced = true;
            yield return new WaitForSeconds(0.2f);
        }

        status.unbalanced = true;

        if (activeTelegraph != null && swingNumber == 1) {
            activeTelegraph.gameObject.SetActive(false);
            foreach (Transform decalChild in activeTelegraph) {
                decalChild.GetComponent<Renderer>().material.SetColor("_TintColor", originalTelegraphColor);
            }
        }

        dude.AddForce(transform.forward * swingEvent.step * Time.fixedDeltaTime);
        gameObject.SendMessage("OnFire");

        yield return new WaitForEndOfFrame();

        string animName = swingEvent.animation.name;
        animated.GetComponent<Animation>()[animName].speed = attackAnimationRatio / swingEvent.rate;
        animated.GetComponent<Animation>().Play(animName, PlayMode.StopAll);

        yield return new WaitForSeconds(swingEvent.rate * (1.0f - attackCancelWindow));

        if (swingNumber < 3) {
            status.attacking = false;
            status.unbalanced = false;
            status.attackingRecovery = true;
        }

        yield return new WaitForSeconds(swingEvent.rate * attackCancelWindow);
        gameObject.BroadcastMessage("OnEnable");

        status.attacking = false;
        status.unbalanced = false;
        status.attackingRecovery = false;

        ResetAnimation();
        gameObject.SendMessage("OnFollowUp");
    }

    void ResetAnimation() {
        animated.GetComponent<Animation>().Stop();
        animated.GetComponent<Animation>().Play("idle");
    }

    public void OnStun(float stunDuration) {
        if (status.stunned || cannotBeStunned) return;

        stunDuration -= stunResistance;
        if (stunDuration <= 0.0f) stunDuration = 0.5f;

        status.stunned = true;
        StartCoroutine(EndStun(stunDuration));
    }

    IEnumerator EndStun(float stunDuration) {
        yield return new WaitForSeconds(stunDuration);
        status.stunned = false;
        status.Reset();
    }

    public void OnDisable() {
        disabled = true;
    }

    public void OnEnable() {
        disabled = false;
    }

    public bool isDisabled {
        get { return disabled; }
    }
}
