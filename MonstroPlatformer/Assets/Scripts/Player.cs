using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]

public class Player : MonoBehaviour
{
    //edit max height and time to make it 
    public float jumpHeight = 4;
    public float timeToJumpApex = 0.4f;
    float accelerationTimeAirborne = .2f;
    float accelerationTimeGrounded = .1f;

    float moveSpeed= 6;

    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;

    //jumpheight = (gravity * timetojumpapex^2)/2
    //gravity = (2*jumpheight)/timetojumpapex^2
    float gravity;
    float jumpVelocity;
    Vector3 velocity;
    float velocityXSmoothing;

    public float wallSlideSpeedMax = 3;
    public float wallStickTime = 0.25f;
    float timeToWallUnstick;


    // Start is called before the first frame update
    Controller2D controller;

    void Start()
    {
        //initalize and calculate gravity and jump velocity
        controller = GetComponent<Controller2D>();

        gravity = -(2 * jumpHeight/Mathf.Pow(timeToJumpApex,2));
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        print ("Gravity: " + gravity + " Jump Velocity: " + jumpVelocity);
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
        int wallDirX = (controller.collisions.left) ? -1:1;

        float targetVelocityX = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below?accelerationTimeGrounded:accelerationTimeAirborne));

        bool wallSliding = false;
        if((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;

            if(velocity.y < -wallSlideSpeedMax){
                velocity.y = -wallSlideSpeedMax;
            }
            if(timeToWallUnstick > 0){
                velocityXSmoothing = 0;
                velocity.x = 0;
                if(input.x != wallDirX && input.x != 0){
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
        //if something is above or below; we dont have any y
        if (controller.collisions.above || controller.collisions.below){
            velocity.y = 0;
        }

        
        //jumping
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (wallSliding){
                if (wallDirX == input.x){
                    velocity.x = -wallDirX * wallJumpClimb.x;
                    velocity.y = wallJumpClimb.y;  

                }
                else if(input.x == 0){
                    velocity.x = -wallDirX * wallJumpOff.x;
                    velocity.y = wallJumpOff.y;
                }
                else{
                    velocity.x = -wallDirX * wallLeap.x;
                    velocity.y = wallLeap.y;
                }
            }
            if(controller.collisions.below){
            velocity.y = jumpVelocity;
            }
        }

       
        //calculate the velocity and gravity using if it's grounded and gravity calculations
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
