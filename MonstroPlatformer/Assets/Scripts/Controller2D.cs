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

    //distance of each ray
    float horitalRaySpacing;
    float verticalRaySpacing;

    //object get components
    BoxCollider2D collider;
    RaycastOrigins raycastOrigins;

    void Start()
    {
        collider = GetComponent<BoxCollider2D>();
        CalculateRaySpacing ();
    }

    public void Move(Vector3 velocity)
    {     
        UpdateRaycastOrigins();

        VerticalCollisions(ref velocity);
        transform.Translate (velocity);
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
            Debug.DrawRay(raycastOrigins.bottomLeft + Vector2.right *verticalRaySpacing * i, Vector2.up * -2, Color.red);

            if(hit)
            {
                velocity.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;
            }
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


}
