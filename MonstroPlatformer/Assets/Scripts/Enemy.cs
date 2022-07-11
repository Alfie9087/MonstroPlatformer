using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : RaycastController
{
    // Start is called before the first frame update
    public LayerMask playerMask;

    public Vector3[] localWaypoints;
    Vector3[] globalWaypoints;

    public float speed;
    public bool cyclic;
    public float waitTime;
    //this clamps or sets minmax value
    [Range(0,2)]
    public float easeAmount;

    int fromWaypointIndex;
    float percentInBetweenWaypoints;
    float nextMoveTime;
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();

        globalWaypoints = new Vector3[localWaypoints.Length];
        for (int i = 0; i < localWaypoints.Length; i++){
            globalWaypoints[i] = localWaypoints[i] + transform.position;
        } 

    }

    // Update is called once per frame
    void Update()
    {
        UpdateRaycastOrigins();

        Vector3 velocity = CalculateEnemyMovement();
        transform.Translate(velocity);
        PlayerContact(velocity);
       
    }

    Vector3 CalculateEnemyMovement(){
        if (Time.time < nextMoveTime){
            return Vector3.zero;
        }

        fromWaypointIndex %= globalWaypoints.Length;
        int toWayPointIndex = (fromWaypointIndex +1) % globalWaypoints.Length;
        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWayPointIndex]);
        percentInBetweenWaypoints += Time.deltaTime * speed/distanceBetweenWaypoints;
        percentInBetweenWaypoints = Mathf.Clamp01(percentInBetweenWaypoints);
        float easedPercentBetweenWaypoints = Ease(percentInBetweenWaypoints);

        Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWayPointIndex], easedPercentBetweenWaypoints);

        // if percent > 1;
        if (percentInBetweenWaypoints >= 1){
            percentInBetweenWaypoints = 0;
            fromWaypointIndex ++;
            if (!cyclic){
                if (fromWaypointIndex >= globalWaypoints.Length - 1){
                    fromWaypointIndex = 0; 
                    System.Array.Reverse(globalWaypoints);
                }
            }
            nextMoveTime = Time.time + waitTime;
        }


        return newPos - transform.position;

    }


    public GameObject other;

    void PlayerContact(Vector3 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        //vertically moving platform
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;


         for (int i = 0; i < verticalRayCount; i ++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (verticalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.up,rayLength,playerMask);

                if (hit && hit.distance != 0)
                {
                    Destroy(gameObject);
                }
            }

        rayLength = Mathf.Abs(velocity.x) + skinWidth;

            for (int i = 0; i < horizontalRayCount; i ++)
            {
                Vector2 rayOrigin = (directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight;
                rayOrigin += Vector2.up * (horitalRaySpacing * i);

                RaycastHit2D kill = Physics2D.Raycast(rayOrigin,Vector2.right * directionX,rayLength,playerMask);

                if (kill && kill.distance != 0)
                {
                    Destroy(other);
                }
            }
        

           
        }

    float Ease(float x){
        float a = easeAmount + 1;
        return Mathf.Pow(x,a) / (Mathf.Pow(x,a) + Mathf.Pow(1-x,a));
    }

    struct EnemyMovement{
        public Transform transform;
        public Vector3 velocity;

        public EnemyMovement(Transform _transform,Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform){
            transform = _transform;
            velocity = _velocity;
        }
    }

    void OnDrawGizmos(){
        if (localWaypoints !=  null){
            Gizmos.color = Color.red;
            float size = 0.3f;

            for (int i = 0; i < localWaypoints.Length; i++){
                Vector3 globalWaypointPos = (Application.isPlaying)?globalWaypoints[i]:localWaypoints[i] + transform.position;
                Gizmos.DrawLine(globalWaypointPos - Vector3.up * size, globalWaypointPos + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPos - Vector3.left * size, globalWaypointPos + Vector3.left * size);
            }
        }
    }




     
}
