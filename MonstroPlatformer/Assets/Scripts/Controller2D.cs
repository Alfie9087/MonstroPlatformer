using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]

public class Controller2D : MonoBehaviour
{

    public LayerMask collisionMask;

    //how deep the rays overlap with the player model
    const float skinWidth = .015f;

    //how many rays we'll use
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;

    float maxClimbAngle = 80;

    //distance of each ray
    float horitalRaySpacing;
    float verticalRaySpacing;

    //object get components
    BoxCollider2D collider;
    RaycastOrigins raycastOrigins;
    public CollisionInfo collisions;

    void Start()
    {
        collider = GetComponent<BoxCollider2D>();
        CalculateRaySpacing ();
    }

    public void Move(Vector3 velocity)
    {     
        UpdateRaycastOrigins();
        collisions.Reset();

        if(velocity.x != 0){
            HorizontalCollisions(ref velocity);
        }
        if(velocity.y != 0){
            VerticalCollisions(ref velocity);
        }
        transform.Translate (velocity);
    }
    void HorizontalCollisions(ref Vector3 velocity)
    {
        //get direction and length using the velocity y
        float directionX =  Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i ++){
            Vector2 rayOrigin = (directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horitalRaySpacing * i);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.right*directionX,rayLength,collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);
            if(hit)
            {
                //the angle of the slope is the same as the normal and the angle the slope is facing
                float slopeAngle = Vector2.Angle(hit.normal,Vector2.up);

                if (i == 0 && slopeAngle <= maxClimbAngle){
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld){
                        distanceToSlopeStart = hit.distance-skinWidth;
                        velocity.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += distanceToSlopeStart * directionX;
                }

                if(!collisions.climbingSlope || slopeAngle > maxClimbAngle){

                    //change velocity and ray length if colliding
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope){
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }

                    //update collisions struct if colliding left or right
                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }

            }
        }
    }



    //ref = uses actual variable instead of copying it. Basically like pointers
    void VerticalCollisions(ref Vector3 velocity)
    {
        //get direction and length using the velocity y
        float directionY =  Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i ++){
            Vector2 rayOrigin = (directionY == -1)?raycastOrigins.bottomLeft:raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.up*directionY,rayLength,collisionMask);
            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

            if(hit)
            {
                //change velocity and ray length if colliding
                velocity.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                if (collisions.climbingSlope){
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);

                }

                //update collisions struct if colliding vertically
                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }
    }

    void ClimbSlope(ref Vector3 velocity, float slopeAngle)
    {
        float moveDistance = Mathf.Abs(velocity.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        if (velocity.y <= climbVelocityY){
            velocity.y = climbVelocityY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }

    

    }

    void UpdateRaycastOrigins()
    {
        //get bounds from Bounds from collider
        Bounds bounds = collider.bounds;
        bounds.Expand (skinWidth * -2);

        //set raycast origins by getting bounds min and max values
        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    void CalculateRaySpacing()
    {
        //get bounds from Bounds from collider
        Bounds bounds = collider.bounds;
        bounds.Expand (skinWidth * -2);

        //distance is calculated by checking how many rays there are
        //example 2 rays means one in each corner (we need at least 2)
        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        //we calculate the space by dividing the bounds size by the number of rays - 1
        horitalRaySpacing = bounds.size.y / (horizontalRayCount-1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount-1);
    }

    //store the corners of our player
    struct RaycastOrigins{
        public Vector2 topLeft, topRight; 
        public Vector2 bottomLeft, bottomRight;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;
        public bool climbingSlope;
        public float slopeAngle, slopeAngleOld;
        public void Reset(){
            above = below = false;
            left = right = false;
            climbingSlope = false;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }


}
