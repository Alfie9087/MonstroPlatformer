using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]

public class Controller2D : RaycastController
{
    //maximum angle that allows the player to climb
    public float maxSlopeAngle = 70;

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
        //print("Before anything: " + moveAmount.y);
        //do calculations when necessary moveAmount
        if (moveAmount.y < 0){
            DescendSlope(ref moveAmount);
        }
        //print("Before descend: " + moveAmount.y);
        if (moveAmount.x != 0){
            collisions.faceDir = (int)Mathf.Sign(moveAmount.x);
        }

        //print("Before horizontal: " + moveAmount.y);
        HorizontalCollisions(ref moveAmount);

        //print("Before vertical: " + moveAmount.y);
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
                collisions.slopeNormal = hit.normal;
                //after calculating the slope angle 
                if (i == 0 && slopeAngle <= maxSlopeAngle){

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
                    ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
                    //then adds back the distance
                    moveAmount.x += distanceToSlopeStart * directionX;
                }
                //if we're climbing now and we can climb, we need to add moveAmount y to go up
                if(!collisions.climbingSlope || slopeAngle > maxSlopeAngle){

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
    void OnSpike(ref Vector2 moveAmount){
        float directionY =  Mathf.Sign(moveAmount.y);
        Vector3 size = new Vector3((collider.bounds.max.x-collider.bounds.min.x),(collider.bounds.max.y-collider.bounds.min.y),(collider.bounds.max.z-collider.bounds.min.z));
        RaycastHit2D shapeCasting = Physics2D.BoxCast(collider.bounds.center, size,0f,Vector2.down,0.01f,collisionMask);
        if(Mathf.Sign(moveAmount.y) == -1){
        if (playerInput.y == -1){
            collisions.fallingThroughPlatform = true;
            Invoke("ResetFallingThroughPlatform", 0.4f);
            }
        if (shapeCasting && (!(collisions.fallingThroughPlatform))){
            
            moveAmount.y = (shapeCasting.distance -0.01f) * directionY;
            //update collisions struct if colliding vertically
            collisions.below = true;
            collisions.below = directionY == -1;
            collisions.above = directionY == 1;
        }
        }
    }



    //ref = uses actual variable instead of copying it. Basically like pointers
    void VerticalCollisions(ref Vector2 moveAmount)
    {
        //get direction and length using the moveAmount y
        float directionY =  Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth + 0.1f;
        //print(rayLength);
        
        for (int i = 0; i < verticalRayCount; i ++){
            
            Vector2 rayOrigin = (directionY == -1)?raycastOrigins.bottomLeft:raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.up*directionY,rayLength,collisionMask);
            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);
            
            if(hit)
            {
                if (!collisions.firsthit){
                    if ((collisions.climbingSlope) || (collisions.descendingSlope)){
                        
                        collisions.shapeCast = false;
                    }
                    if (Mathf.Sign(collisions.shapeCastAngleChecker) != Mathf.Sign(hit.normal.x)){
                        collisions.shapeCast = true;
                    } 

                    

                } 
                if(collisions.firsthit){
                    collisions.shapeCastAngleChecker = hit.normal.x;
                    collisions.firsthit = false;
                    if (i != 0 && i != (verticalRayCount - 1)){
                        collisions.shapeCast = true;
                    }
                    // else if (i == 0 && moveAmount.x != 0){
                    //     if (!(collisions.climbingSlope) && !(collisions.descendingSlope)){
                    //         if (Mathf.Sign(Vector2.Angle(hit.normal,Vector2.up)) != Mathf.Sign(moveAmount.x) && moveAmount.x != 0){
                    //             collisions.shapeCast = true;
                    //         }
                    //     }
                    // }
                }
                
                if(collisions.shapeCast){
                    if (collisions.descendingSlope || collisions.climbingSlope || collisions.slidingDownMaxSlope|| collisions.fallingThroughPlatform){
                        collisions.shapeCast = false;
                        
                    }
                    if ((i == 0) && hit.distance == 0 &&(Mathf.Sign(moveAmount.x) == 1)){
                        if (Mathf.Sign(collisions.shapeCastAngleChecker) == Mathf.Sign(moveAmount.x)){
                            collisions.shapeCast = false;
                    
                        }
                    }

                }

                if (hit.collider.tag == "Through"){
                    if(directionY == 1 || hit.distance == 0){
                        continue;
                    }
                    if (collisions.fallingThroughPlatform){
                        continue;
                    }
                    if (playerInput.y == -1){
                        collisions.fallingThroughPlatform = true;
                        Invoke("ResetFallingThroughPlatform", 0.4f);
                        continue;
                    }
                
                }
                if(collisions.shapeCast){
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


        collisions.firsthit = true;

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
                    collisions.slopeNormal = hit.normal;
                }
            }

        }
        
        if(collisions.shapeCast){
            Vector3 size = new Vector3((collider.bounds.max.x-collider.bounds.min.x),(collider.bounds.max.y-collider.bounds.min.y),(collider.bounds.max.z-collider.bounds.min.z));
            RaycastHit2D shapeCasting = Physics2D.BoxCast(collider.bounds.center, size,0f,Vector2.down,0.02f,collisionMask);
            if(Mathf.Sign(moveAmount.y) == -1 && !((collisions.descendingSlope || collisions.climbingSlope || collisions.slidingDownMaxSlope|| collisions.fallingThroughPlatform))){
                if (playerInput.y == -1){
                collisions.fallingThroughPlatform = true;
                Invoke("ResetFallingThroughPlatform", 0.5f);
                }
                if (shapeCasting && (!(collisions.fallingThroughPlatform))){
                
                moveAmount.y = (shapeCasting.distance -0.02f) * directionY;
                //update collisions struct if colliding vertically
                collisions.below = true;
                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
                }
                else{collisions.shapeCast = false;}
            }
            else{
                collisions.shapeCast = false;
            }
            if (!shapeCasting){
                collisions.shapeCast = false;
            }


        }
        // if (collisions.shapeCast){
        //     print("yes");
        // }
        // else {
        //     print("no");
        // }

    }

    void TurnOffShapeCasting(){
        collisions.shapeCast = false;
    }


    void ResetFallingThroughPlatform(){
        collisions.fallingThroughPlatform = false;
    }
    void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
    {

        //basically we calculate how high we go using sin and the angle
        //the if statement is if we're climbing a slope then we're grounded
        //the cos statement is us calculating our speed climbing
        float moveDistance = Mathf.Abs(moveAmount.x);
        float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        if (moveAmount.y <= climbmoveAmountY){
            if (Mathf.Sign(moveAmount.x) == Mathf.Sign(collisions.faceDir))
            {
                moveAmount.y = climbmoveAmountY;
                moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                collisions.below = true;
                collisions.climbingSlope = true;
                collisions.slopeAngle = slopeAngle;
                collisions.slopeNormal = slopeNormal;
                
            }
            else{
                moveAmount.x = moveAmount.y = 0;
            }
        }
        //print("awawa" + moveAmount.y);
        
    

    }

    void DescendSlope(ref Vector2 moveAmount) {

        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y)+ skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y)+ skinWidth, collisionMask);

        if (maxSlopeHitLeft ^ maxSlopeHitRight){
            SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
            SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
        }


        if (!collisions.slidingDownMaxSlope)
        {
            float directionX = Mathf.Sign(moveAmount.x);

            //if we move left we want bottom right, right = left
            Vector2 rayOrigin = (directionX == -1)? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);
            if (hit){
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle){
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
                            collisions.slopeNormal = hit.normal;


                        }
                    } 
                }
            }
        }
    }

    void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount){
        if (hit){
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle > maxSlopeAngle){
                moveAmount.x = hit.normal.x * (Mathf.Abs(moveAmount.y) -hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

                collisions.slopeAngle = slopeAngle;
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal;
            }

        }
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;


        public bool climbingSlope, descendingSlope;
        public bool slidingDownMaxSlope;
        public bool shapeCast;
        
        public bool shapeCastChecker;
        public bool firsthit;
        
        public float shapeCastAngleChecker;

        public float slopeAngle, slopeAngleOld;
        public Vector2 moveAmountOld;
        public Vector2 slopeNormal;
        public bool fallingThroughPlatform;

        public int faceDir;
        public void Reset(){
            above = below = false;
            left = right = false;
            firsthit = true;
            shapeCastAngleChecker = 0;
            climbingSlope = false;
            descendingSlope = false;
            slidingDownMaxSlope = false;
            slopeNormal = Vector2.zero;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }


}
