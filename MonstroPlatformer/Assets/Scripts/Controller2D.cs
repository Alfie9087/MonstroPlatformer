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
    public void Move(Vector2 moveAmount, bool standingOnPlatform){
        Move(moveAmount,Vector2.zero, standingOnPlatform); 
    }

    //this function is for moving the player (is called by player)
    public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
    {     
        //whenever we move we update the rays and reset collisions
        UpdateRaycastOrigins();
        collisions.Reset();

        collisions.moveAmountOld = moveAmount;
        playerInput = input;

        if (moveAmount.x != 0){
            collisions.faceDir = (int)Mathf.Sign(moveAmount.x);
        }

        //do calculations when necessary moveAmount
        if (moveAmount.y < 0){
            DescendSlope(ref moveAmount);
        }



        HorizontalCollisions(ref moveAmount);
        if(moveAmount.y != 0){
            VerticalCollisions(ref moveAmount);
        }
        //afterwards translate
        transform.Translate (moveAmount);
        if (standingOnPlatform){
            collisions.below = true;
        }
    }

    //horizontal collisions function
    //does this everytime the horizontal rays hit something.
    void HorizontalCollisions(ref Vector2 moveAmount)
    {
        //get direction and length using the moveAmount x
        float directionX =  collisions.faceDir;
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

        if(Mathf.Abs(moveAmount.x) < skinWidth){
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
            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);
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
                        moveAmount = collisions.moveAmountOld;
                    }
                    //ok if we can climb then we call the climb function
                    //but we need a function when we're swapping from one slop to another
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld){
                        //basically resetting x so that it calculates the swap
                        distanceToSlopeStart = hit.distance-skinWidth;
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref moveAmount, slopeAngle);
                    //then adds back the distance
                    moveAmount.x += distanceToSlopeStart * directionX;
                }
                //if we're climbing now and we can climb, we need to add moveAmount y to go up
                if(!collisions.climbingSlope || slopeAngle > maxClimbAngle){

                    //change moveAmount and ray length if colliding
                    //moveAmount.x = (hit.distance - skinWidth) * directionX;
                    //rayLength = hit.distance;
                    moveAmount.x = Mathf.Min(Mathf.Abs(moveAmount.x), (hit.distance - skinWidth)) * directionX;
                    rayLength = Mathf.Min(Mathf.Abs(moveAmount.x) + skinWidth, hit.distance);

                    if (collisions.climbingSlope){
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                    }

                    //update collisions struct if colliding left or right
                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }

            }
        }
    }



    //ref = uses actual variable instead of copying it. Basically like pointers
    void VerticalCollisions(ref Vector2 moveAmount)
    {
        //get direction and length using the moveAmount y
        float directionY =  Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i ++){
            Vector2 rayOrigin = (directionY == -1)?raycastOrigins.bottomLeft:raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.up*directionY,rayLength,collisionMask);
            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);

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

                //change moveAmount and ray length if colliding
                moveAmount.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                if (collisions.climbingSlope){
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);

                }

                //update collisions struct if colliding vertically
                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }

        if(collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(moveAmount.x);
            rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit){
                float slopeAngle = Vector2.Angle(hit.normal,Vector2.up);
                if (slopeAngle != collisions.slopeAngle){
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }

        }
    }
    void ResetFallingThroughPlatform(){
        collisions.fallingThroughPlatform = false;
    }
    void ClimbSlope(ref Vector2 moveAmount, float slopeAngle)
    {
        //basically we calculate how high we go using sin and the angle
        //the if statement is if we're climbing a slope then we're grounded
        //the cos statement is us calculating our speed climbing
        float moveDistance = Mathf.Abs(moveAmount.x);
        float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        if (moveAmount.y <= climbmoveAmountY){
            moveAmount.y = climbmoveAmountY;
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }

    

    }

    void DescendSlope(ref Vector2 moveAmount) {
        float directionX = Mathf.Sign(moveAmount.x);
        //if we move left we want bottom right, right = left
        Vector2 rayOrigin = (directionX == -1)? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);
        if (hit){
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle){
                //if the x axis of the normal of hit is = to directionX then we're moving down the slope
                if (Mathf.Sign(hit.normal.x) == directionX){
                    //if our distance to the slope is less than how far we have to move in the y axis, then we're close enough
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x)){
                        float moveDistance = Mathf.Abs(moveAmount.x);
                        float descendmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                        moveAmount.y -= descendmoveAmountY;

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
        public Vector2 moveAmountOld;
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
