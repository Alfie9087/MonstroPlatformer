using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent (typeof (BoxCollider2D))]

public class RaycastController : MonoBehaviour
{
    //mask what collides with raycast
    public LayerMask collisionMask;

    //how deep the rays overlap with the player model
    public const float skinWidth = 0.1f;

    //distance of each ray
    [HideInInspector]
    public float horitalRaySpacing;
    [HideInInspector]
    public float verticalRaySpacing;

    //object get components
    [HideInInspector]
    public BoxCollider2D collider;
    public RaycastOrigins raycastOrigins;
    
    //how many rays we'll use
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;
    
    //public virtual void means it will call even if we use another start in controller 2d
    public virtual void Start()
    {
        //initialize everything
        collider = GetComponent<BoxCollider2D>();
        CalculateRaySpacing ();
    }

    public void UpdateRaycastOrigins()
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

    public void CalculateRaySpacing()
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
    public struct RaycastOrigins{
        public Vector2 topLeft, topRight; 
        public Vector2 bottomLeft, bottomRight;
    }
}
