using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerController2D))]
public class Player : MonoBehaviour {

    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float maxJumpHeight = 4f;
    [SerializeField] private float minJumpHeight = 1f;
    [SerializeField] private float timeToJumpApex = 0.4f;
    [SerializeField] private float maxFallingSpeed;
    [SerializeField] private float fastFallGravityModifier = 1.5f;
    [SerializeField] private float accelerationTimeAirborne = 0.2f;
    [SerializeField] private float accelerationGrounded = 0.1f;
    [SerializeField] private float accelerationFlying = 0.05f;
    [SerializeField] private float wallSlideSpeedMax = 3f;
    [SerializeField] private Vector2 wallJumpClimb;
    [SerializeField] private Vector2 wallJumpOff;
    [SerializeField] private Vector2 wallLeap;
    [SerializeField] private float wallStickTime = 0.25f;

    [SerializeField] private LayerMask mask;

    [SerializeField] public Vector2 velocity;

    float maxJumpVelocity;
    float minJumpVelocity;
    float gravity;
    float velocitySmoothingX;
    float velocitySmoothingY;
    float timeToWallUnstick;
    bool canDoubleJump = false;

	[SerializeField] private float maxFlyTime = 1.5f;
	[SerializeField] private float maxFlySpeed = 10f;
	[SerializeField] private float flyStrength = 6f;
	[SerializeField] private float flySinStrength = 4f;
	float timeFlyInitiated = 0f;
	bool flying;

    PlayerController2D controller;

	[SerializeField]
	Transform graphicsObject;
	[SerializeField]
	Animator anim;
	[SerializeField]
	ParticleSystem landDust;
	public float dustTol = 0.15f;

    void Start() {
        transform.parent.transform.position = new Vector3(0f, 0f, 0f);
        controller = GetComponent<PlayerController2D>();

        //suvats give:
        gravity = -(2 * maxJumpHeight) / (timeToJumpApex * timeToJumpApex);
        maxJumpVelocity = -gravity * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * -gravity * minJumpHeight);
    }

    void Update() {

		anim.SetBool ("Land", false);
		anim.SetBool ("Fall", false);

		Vector2 input = PlayerInput.GetDirectionalInput ();

		int wallDirectionX = (controller.collisions.left) ? -1 : 1;
		float targetVelocityX;

		targetVelocityX = input.x * moveSpeed;
		velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocitySmoothingX, (controller.collisions.below) ? accelerationGrounded : accelerationTimeAirborne);

		if (PlayerInput.GetFlyDown()) {
			flying = true;
			timeFlyInitiated = Time.time;
			anim.SetTrigger ("Flying");
		}
		if (flying && (PlayerInput.GetFlyUp () || Time.time - timeFlyInitiated > maxFlyTime)) {
			flying = false;
			anim.SetTrigger ("FlyingRelease");
		}

		if (flying) {
			velocity.y = Mathf.Lerp(velocity.y, flyStrength  + flySinStrength * Mathf.Sin ((Time.time - timeFlyInitiated) * 4f * Mathf.PI), 4f * (Time.time - timeFlyInitiated));
			if (velocity.y > maxFlySpeed) {
				velocity.y = maxFlySpeed;
			}
		}
			

		bool wallSliding = false;
		if (controller.collisions.left || controller.collisions.right && !controller.collisions.below && velocity.y < 0) {
			wallSliding = true;

			if (velocity.y < -wallSlideSpeedMax) {
				velocity.y = -wallSlideSpeedMax;
			}

			if (timeToWallUnstick > 0) {
				velocitySmoothingX = 0f;
				velocitySmoothingY = 0f;
				velocity.x = 0f;

				if (input.x != wallDirectionX && input.x != 0f) {
					timeToWallUnstick -= Time.deltaTime;
				} else {
					timeToWallUnstick = wallStickTime;
				}
			} else {
				timeToWallUnstick = wallStickTime;
			}
		}


		if (controller.collisions.above && velocity.y > 0) {
			velocity.y = 0f;
		}

		if (PlayerInput.GetJumpDown ()) {
			if (wallSliding) {
				if (wallDirectionX == input.x) {
					velocity.x = -wallDirectionX * wallJumpClimb.x;
					velocity.y = wallJumpClimb.y;
				} else if (velocity.x == 0f) {
					velocity.x = -wallDirectionX * wallJumpOff.x;
					velocity.y = wallJumpOff.y;		
				} else {
					velocity.x = -wallDirectionX * wallLeap.x;
					velocity.y = wallLeap.y;
				}
				TriggerJump ();
				canDoubleJump = true;
			}

			if (controller.collisions.below) {
				velocity.y = maxJumpVelocity;
				canDoubleJump = true;
				TriggerJump ();
			} else if (canDoubleJump && !wallSliding) {
				velocity.y = (maxJumpVelocity + minJumpVelocity) / 2f;
				canDoubleJump = false;
				TriggerJump ();
			}
		}
		if (PlayerInput.GetJumpUp ()) {
			if (velocity.y > minJumpVelocity) {
				velocity.y = minJumpVelocity;
			}
		}
			

		if (controller.collisions.below) {
			TriggerLand ();
		} else if (!flying)  {
			TriggerFall ();
		}

		velocity.y += gravity * Time.deltaTime * ((velocity.y < 0f) ? fastFallGravityModifier : 1f);
		if (velocity.y < -maxFallingSpeed) {
			velocity.y = -maxFallingSpeed;
		}
        
		controller.Move (velocity * Time.deltaTime);

		anim.SetFloat ("VelocityX", (Mathf.Abs (velocity.x) < 0) ? 0 : (Mathf.Abs (velocity.x) > 1 ? 1 : Mathf.Abs (velocity.x)));
		anim.SetFloat ("VelocityY", velocity.y);

	}

	void TriggerJump() {
		anim.SetTrigger ("Jump");
	}

	void TriggerFall() {
		anim.SetBool ("Fall", true);
	}

	void TriggerLand() {
		anim.SetBool ("Land", true);
	}

	public void TriggerLandDust() {
		landDust.Play ();
	}
}