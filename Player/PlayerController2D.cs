using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController2D : MonoBehaviour {

    const float SKINWIDTH = 0.015f;

    public CollisionInfo collisions;
    [SerializeField] private LayerMask collisionMask;

    [SerializeField] private int horizontalRayCount = 4;
    [SerializeField] private int verticalRayCount = 3;

    [SerializeField] private float maxClimbAngle = 80f;
    [SerializeField] private float maxDescendAngle = 75f;

	[SerializeField] private bool debug = false;

    float horizontalRaySpacing;
    float verticalRaySpacing;

    public new BoxCollider2D collider;
    public Vector2 lastVelocity;

    RaycastOrigins raycastOrigins;
	Player player;

    void Start() {
        collider = GetComponent<BoxCollider2D>();
		player = GetComponent<Player> ();
        CalculateRaySpacing();
        collisions.faceDirection = 1;
    }

	void Update() {
		if (debug) {
			for (int i = 0; i < verticalRayCount; i++) {
				Debug.DrawRay (raycastOrigins.bottomLeft + Vector2.right * verticalRaySpacing * i, Vector2.up * -2, Color.red);
			}

			for (int i = 0; i < horizontalRayCount; i++) {
				Debug.DrawRay (raycastOrigins.bottomRight + Vector2.up * horizontalRaySpacing * i, Vector2.right * 2, Color.red);
			}
		}
	}

    public void Move(Vector2 velocity) {
        //Reset raycast and collisions for his frame
        UpdateRaycastOrigins();
        collisions.Reset();

        collisions.velocityOld = velocity;

        //Find direction of player
        if (velocity.x != 0f) {
            collisions.faceDirection = (int)Mathf.Sign(velocity.x);
        }

        if(velocity.y < 0) {
            HandleDescendingSlope(ref velocity);
        }

        //Handle collisions
        HorizontalCollisions(ref velocity);
        if (velocity.y != 0f) {
            VerticalCollisions(ref velocity);
        }

        lastVelocity = velocity;

        transform.Translate(velocity);
    }

    void HorizontalCollisions(ref Vector2 velocity) {

        float directionX = (float)collisions.faceDirection;
        float rayLength = Mathf.Abs(velocity.x) + SKINWIDTH;

        //Still check for collisions when not moving (i.e. can tell we are touching the wall even if we're not pushing against it)
        if(Mathf.Abs(velocity.x) < SKINWIDTH) {
            rayLength = 2 * SKINWIDTH;
        }

        //Raycast at equally spaced points along the left or right side of the player (depending on which direction the player is moving)
        for (int i = 0; i < horizontalRayCount; i++) {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * horizontalRaySpacing * i;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, directionX * Vector2.right, rayLength, collisionMask);

            if (hit) {

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                //Handles slope movement on first iteration of for loop only, i.e. the corner closest to the floor
                if (i == 0 && slopeAngle <= maxClimbAngle) {
                    if (collisions.descendingSlope) {
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityOld;
                    }

                    float distanceToSlopeStart = 0f;
                    if (slopeAngle != collisions.slopeAngleOld) {
                        distanceToSlopeStart = hit.distance - SKINWIDTH;

                        //Carry on all the way to slope's start before ascending it
                        velocity.x -= distanceToSlopeStart * directionX;
                    }

                    //Slope climbing handled
                    HandleClimbingSlope(ref velocity, slopeAngle);

                    //Return to correct x velocity
                    velocity.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle) {
                    //Collide with walls and slopes too vertical
                    velocity.x = (hit.distance - SKINWIDTH) * directionX;
                    rayLength = hit.distance;

                    if(collisions.climbingSlope) {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }

                    collisions.left = (directionX == -1);
                    collisions.right = (directionX == 1);
                }
            }
        }
    }

    void VerticalCollisions(ref Vector2 velocity) {

        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + SKINWIDTH;

        //Raycast at equally spaced points along the top or bottom side of the player (depending on which direction the player is moving)
        for (int i = 0; i < verticalRayCount; i++) {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, directionY * Vector2.up, rayLength, collisionMask);

            if (hit) {
                velocity.y = (hit.distance - SKINWIDTH) * directionY;
                rayLength = hit.distance;
				if (velocity.y < -player.dustTol && !collisions.descendingSlope) {
					player.TriggerLandDust ();
				}

                if (collisions.climbingSlope) {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }

                collisions.below = (directionY == -1);
                collisions.above = (directionY == 1);
            }
        }

        if(collisions.climbingSlope) {
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + SKINWIDTH;
            Vector2 rayOrigin = ((directionX == -1)?raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, directionX * Vector2.right, rayLength, collisionMask);

            if (hit) {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle) {
                    velocity.x = (hit.distance - SKINWIDTH) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }

    }

    void HandleClimbingSlope(ref Vector2 velocity, float slopeAngle) {
        float moveDistance = Mathf.Abs(velocity.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        if (velocity.y <= climbVelocityY) {
            velocity.y = climbVelocityY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void HandleDescendingSlope (ref Vector2 velocity) {
        float directoinX = Mathf.Sign(velocity.x);
        Vector2 rayOrigin = (directoinX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if (hit) {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle) {
                if(Mathf.Sign(hit.normal.x) == directoinX) {
                    if(hit.distance - SKINWIDTH <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x)) {
                        float moveDistance = Mathf.Abs(velocity.x);
                        float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
                        velocity.y -= descendVelocityY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }


    void UpdateRaycastOrigins() {
        Bounds bounds = collider.bounds;
        bounds.Expand(SKINWIDTH * -2f);

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    void CalculateRaySpacing() {
        Bounds bounds = collider.bounds;
        bounds.Expand(SKINWIDTH * -2f);

        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);


    }


    struct RaycastOrigins {
        public Vector2 topLeft;
        public Vector2 topRight;
        public Vector2 bottomLeft;
        public Vector2 bottomRight;
    }

    public struct CollisionInfo {
        public bool above;
        public bool below;
        public bool left;
        public bool right;

        public bool climbingSlope;
        public bool descendingSlope;
        public float slopeAngle, slopeAngleOld;

        public Vector2 velocityOld;
        public int faceDirection;

        public void Reset() {
           descendingSlope = climbingSlope = above = below = left = right = false;
           slopeAngleOld = slopeAngle;
           slopeAngle = 0;
        }
    }
}
