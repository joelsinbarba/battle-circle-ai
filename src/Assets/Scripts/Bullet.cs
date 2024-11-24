using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("DudeWorld/Bullet")]
public class Bullet : MonoBehaviour {
	
	public int damage = 100;
	public float speed = 500.0f;
	public float range = 9999.0f;
	public float knockbackForce = 0.0f;
	public GameObject boom;
	public DamageEvent data;
	
	private float life = 9999.0f;
	public GameObject owner;
	public int team = 0;
	public bool environmental = false;
	public bool shakesCamera = false;
	public bool penetrates = false;
	public string[] effects;
	
	private float distanceTravelled = 0.0f;

	private Rigidbody rb;
	
	void Start() {
		if (owner == null)
			owner = gameObject;

		rb = GetComponent<Rigidbody>();

		// Hack to handle speed and range calculation
		if (speed == 0.0f) {
			life = range;
		} else if (!environmental) {
			// Apply force to bullet
			rb.AddForce(transform.forward * speed, ForceMode.VelocityChange);
			life = (range / speed);
		}
		
		/*if (shakesCamera && Camera.main != null) {
			// Access camera shake effect directly instead of using SendMessage
			Camera.main.GetComponent<CameraShake>()?.Shake(0.1f);
		}*/
	}
	
	void FixedUpdate() {
		if (!environmental) {
			life -= Time.fixedDeltaTime;
			if (life <= 0.0f) {
				OnKill();
			}
		}
	}
	
	void OnCollisionEnter(Collision c) {
		OnTriggerEnter(c.collider);
	}
	
	void OnTriggerEnter(Collider other) {	
		if (other.isTrigger) return;

		if (this.data == null)
			this.data = new DamageEvent(this.owner, damage, knockbackForce, 0, team);

		if (this.data.owner == null)
			this.data.owner = gameObject;

		// Apply effects
		if (effects.Length > 0) {
			foreach (string effect in effects) {
				string[] bits = effect.Split('=');
				if (bits.Length == 2) {
					this.data.effects[bits[0]] = float.Parse(bits[1]);
				}
			}
		}

		this.data.bullet = gameObject;
		this.data.victim = other.gameObject;

		if (speed > 0.0f)
			this.data.ranged = true;

		other.gameObject.SendMessage("OnShot", this.data, SendMessageOptions.DontRequireReceiver);

		if (!penetrates)
			OnKill();
	}
	
	void OnKill() {
		if (environmental) return;

		if (boom != null) {
			Instantiate(boom, transform.position, transform.rotation);
		}
		
		// Detach trail if present
		var trail = GetComponentInChildren<TrailRenderer>();
		if (trail != null) {
			trail.transform.parent = null;
		}

		// Handle ParticleSystems (replaces deprecated ParticleEmitter)
		var particleSystems = GetComponentsInChildren<ParticleSystem>();
		foreach (var particleSystem in particleSystems) {
			particleSystem.Stop();
			particleSystem.transform.parent = null; // Detach
		}
		
		Destroy(gameObject);
	}
}
