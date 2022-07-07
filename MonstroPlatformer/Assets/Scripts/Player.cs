using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]

public class Player : MonoBehaviour
{
    //edit max height and time to make it 
    public float maxJumpHeight = 8;
    public float minJumpHeight = 2;
    public float timeToJumpApex = 0.4f;
    float accelerationTimeAirborne = .2f;
    float accelerationTimeGrounded = .1f;

    float moveSpeed= 6;

    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;

    //maxJumpHeight = (gravity * timetojumpapex^2)/2
    //gravity = (2*maxJumpHeight)/timetojumpapex^2
    float gravity;
    float maxJumpVelocity;
    float minJumpVelocity;
    Vector3 velocity;
    float velocityXSmoothing;

    public float wallSlideSpeedMax = 3;
    public float wallStickTime = 0.25f;
    float timeToWallUnstick;

    Vector2 directionalInput;

    bool wallSliding;
    int wallDirX;

    public Animator animator;


    // Start is called before the first frame update
    Controller2D controller;

    void Start()
    {
        //initalize and calculate gravity and jump velocity
        controller = GetComponent<Controller2D>();

        gravity = -(2 * maxJumpHeight)/Mathf.Pow(timeToJumpApex,2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2*Mathf.Abs(gravity) * minJumpHeight);
        //print ("Gravity: " + gravity + " Jump Velocity: " + maxJumpVelocity);
    }

     // Update is called once per frame
    void Update()
    {
        //Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));

        CalculateVelocity();
        HandleWallSliding();

        //if something is above or below; we dont have any y
        controller.Move(velocity * Time.deltaTime, directionalInput);

        if (controller.collisions.above || controller.collisions.below){
            if(controller.collisions.slidingDownMaxSlope){
                velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
            } else{
                velocity.y = 0;
            }
        }
        animator.SetFloat("Speed", velocity.x);
        if(wallSliding){
            animator.SetBool("WallSliding", true);
            animator.SetFloat("Speed", (Input.GetAxisRaw("Horizontal")));
        }
        else{
            animator.SetBool("WallSliding", false);
        }
        if(!controller.collisions.below){
            animator.SetBool("InAir", true);
        }
        else{
            animator.SetBool("InAir", false);
        }
        

        
    }


    public void SetDirectionalInput(Vector2 input){
        directionalInput = input;
    }
    public void OnJumpInputDown(){
        if (wallSliding){
            if (wallDirX == directionalInput.x){
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;  

            }
            else if(directionalInput.x == 0){
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
            }
            else{
                velocity.x = -wallDirX * wallLeap.x;
                velocity.y = wallLeap.y;
            }
        }
        if(controller.collisions.below){
            if (controller.collisions.slidingDownMaxSlope){
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x)){
                    //not jumping against max slope
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                }
            }else{
            velocity.y = maxJumpVelocity;
            }
        }
    }
    public void OnJumpInputUp(){
        if (velocity.y > minJumpVelocity){
            velocity.y = minJumpVelocity;
        }
    }

    void HandleWallSliding(){
        wallDirX = (controller.collisions.left) ? -1:1;
        wallSliding = false;

        //print(controller.collisions.slopeAngleOld);
        //print(Vector2.Angle(controller.collisions.slopeNormal,Vector2.up));
        if (Vector2.Angle(controller.collisions.slopeNormal,Vector2.up) >= 85){
        if((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;

            if(velocity.y < -wallSlideSpeedMax){
                velocity.y = -wallSlideSpeedMax;
            }
            if(timeToWallUnstick > 0){
                velocityXSmoothing = 0;
                velocity.x = 0;
                if(directionalInput.x != wallDirX && directionalInput.x != 0){
                    timeToWallUnstick -= Time.deltaTime;
                }
                else{
                    timeToWallUnstick = wallStickTime;
                }
            }
        }
        else{
            timeToWallUnstick = wallStickTime;
        }
        }
    }

    void CalculateVelocity(){
         //calculate the velocity and gravity using if it's grounded and gravity calculations
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below?accelerationTimeGrounded:accelerationTimeAirborne));
        velocity.y += gravity * Time.deltaTime;
    }
}
