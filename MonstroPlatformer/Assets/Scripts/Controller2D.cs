using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]

public class Controller2D : RaycastController
{


    //maximum angle that allows the player to climb
    float maxClimbAngle = 80;
    float maxDescendAngle = 75;

    public CollisionInfo collisions;
    [HideInInspector]
    public Vector2 playerInput;

    public override void Start(){
        base.Start ();
        collisions.faceDir = 1;
    }
    public void Move(Vector3 velocity, bool standingOnPlatform){
        Move(velocity,Vector2.zero, standingOnPlatform); 
    }

    //this function is for moving the player (is called by player)
    public void Move(Vector3 velocity, Vector2 input, bool standingOnPlatform = false)
    {     
        //whenever we move we update the rays and reset collisions
        UpdateRaycastOrigins();
        collisions.Reset();

        collisions.velocityOld = velocity;
        playerInput = input;

        if (velocity.x != 0){
            collisions.faceDir = (int)Mathf.Sign(velocity.x);
        }

        //do calculations when necessary velocity
        if (velocity.y < 0){
            DescendSlope(ref velocity);
        }



        HorizontalCollisions(ref velocity);
        if(velocity.y != 0){
            VerticalCollisions(ref velocity);
        }
        //afterwards translate
        transform.Translate (velocity);
        if (standingOnPlatform){
            collisions.below = true;
        }
    }

    //horizontal collisions function
    //does this everytime the horizontal rays hit something.
    void HorizontalCollisions(ref Vector3 velocity)
    {
        //get direction and length using the velocity x
        float directionX =  collisions.faceDir;
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        if(Mathf.Abs(velocity.x) < skinWidth){
            rayLength = 2*skinWidth;
        }

        //do something for each ray
        for (int i = 0; i < horizontalRayCount; i ++){
            //sets which ray origin to look at if the directionX is left or right
            Vector2 rayOrigin = (directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight;

            //sets where each ray starts
            rayOrigin += Vector2.up * (horitalRaySpacing * i);

            //calculate if it hit something (origin,direction, length of the ray, mask (this is the layer))
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.right*directionX,rayLength,collisionMask);

            //this just draws it
            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);
            if(hit)
            {
                //if the distance is 0 use the next ray to determine collisions
                if (hit.distance == 0){
                    continue;
                }
                //the angle of the slope is the same as the normal and the angle the slope is facing
                float slopeAngle = Vector2.Angle(hit.normal,Vector2.up);
                //after calculating the slope angle 
                if (i == 0 && slopeAngle <= maxClimbAngle){

                    if (collisions.descendingSlope){
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityOld;
                    }
                    //ok if we can climb then we call the climb function
                    //but we need a function when we're swapping from one slop to another
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld){
                        //basically resetting x so that it calculates the swap
                        distanceToSlopeStart = hit.distance-skinWidth;
                        velocity.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref velocity, slopeAngle);
                    //then adds back the distance
                    velocity.x += distanceToSlopeStart * directionX;
                }
                //if we're climbing now and we can climb, we need to add velocity y to go up
                if(!collisions.climbingSlope || slopeAngle > maxClimbAngle){

                    //change velocity and ray length if colliding
                    //velocity.x = (hit.distance - skinWidth) * directionX;
                    //rayLength = hit.distance;
                    velocity.x = Mathf.Min(Mathf.Abs(velocity.x), (hit.distance - skinWidth)) * directionX;
                    rayLength = Mathf.Min(Mathf.Abs(velocity.x) + skinWidth, hit.distance);

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
                if (hit.collider.tag == "Through"){
                    if(directionY == 1 || hit.distance == 0){
                        continue;
                    }
                }
                if (collisions.fallingThroughPlatform){
                    continue;
                }
                if (playerInput.y == -1){
                    collisions.fallingThroughPlatform = true;
                    Invoke("ResetFallingThroughPlatform", 0.4f);
                    continue;
                }

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

        if(collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight) + Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit){
                float slopeAngle = Vector2.Angle(hit.normal,Vector2.up);
                if (slopeAngle != collisions.slopeAngle){
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }

        }
    }
    void ResetFallingThroughPlatform(){
        collisions.fallingThroughPlatform = false;
    }
    void ClimbSlope(ref Vector3 velocity, float slopeAngle)
    {
        //basically we calculate how high we go using sin and the angle
        //the if statement is if we're climbing a slope then we're grounded
        //the cos statement is us calculating our speed climbing
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

    void DescendSlope(ref Vector3 velocity) {
        float directionX = Mathf.Sign(velocity.x);
        //if we move left we want bottom right, right = left
        Vector2 rayOrigin = (directionX == -1)? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);
        if (hit){
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle){
                //if the x axis of the normal of hit is = to directionX then we're moving down the slope
                if (Mathf.Sign(hit.normal.x) == directionX){
                    //if our distance to the slope is less than how far we have to move in the y axis, then we're close enough
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x)){
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

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;
        public bool climbingSlope, descendingSlope;
        public float slopeAngle, slopeAngleOld;
        public Vector3 velocityOld;
        public bool fallingThroughPlatform;

        public int faceDir;
        public void Reset(){
            above = below = false;
            left = right = false;
            climbingSlope = false;
            descendingSlope = false;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }


}
