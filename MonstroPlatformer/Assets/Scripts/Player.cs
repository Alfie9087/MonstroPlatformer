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

    //jumpheight = (gravity * timetojumpapex^2)/2
    //gravity = (2*jumpheight)/timetojumpapex^2
    float gravity;
    float jumpVelocity;
    Vector3 velocity;
    float velocityXSmoothing;


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

        if (controller.collisions.above || controller.collisions.below){
            velocity.y = 0;
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
        
        //jumping
        if (Input.GetKeyDown(KeyCode.Space) && controller.collisions.below)
        {
            velocity.y = jumpVelocity;
        }

       
        //calculate the velocity and gravity using if it's grounded and gravity calculations
        float targetVelocityX = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below?accelerationTimeGrounded:accelerationTimeAirborne));
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
