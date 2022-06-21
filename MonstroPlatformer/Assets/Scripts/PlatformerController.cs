using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerController : RaycastController
{
    // Start is called before the first frame update
    public LayerMask passengersMask;

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
    

    List<PassengerMovement> passengerMovement;
    Dictionary<Transform,Controller2D> passengerDictionary = new Dictionary<Transform,Controller2D>();

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

        Vector3 velocity = CalculatePlatformMovement();

        CalculatePassengerMoement(velocity);
        MovePassengers(true);

        transform.Translate(velocity);
        MovePassengers(false);
    }

    Vector3 CalculatePlatformMovement(){
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

    float Ease(float x){
        float a = easeAmount + 1;
        return Mathf.Pow(x,a) / (Mathf.Pow(x,a) + Mathf.Pow(1-x,a));
    }

    void MovePassengers (bool beforeMovePlatform){
        foreach (PassengerMovement passenger in passengerMovement){
            if (!passengerDictionary.ContainsKey(passenger.transform)){
                passengerDictionary.Add(passenger.transform,passenger.transform.GetComponent<Controller2D>());
            }
            if (passenger.moveBeforePlatform == beforeMovePlatform){
                passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);

            }
        }
    }

    //passengers = controller2d that interacts with platform
    void CalculatePassengerMoement(Vector3 velocity)
    {
        HashSet<Transform> movedPassengers = new HashSet<Transform>();
        passengerMovement = new List<PassengerMovement>();
        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);
        //vertically moving platform
        if (velocity.y != 0){
            float rayLength = Mathf.Abs(velocity.y) + skinWidth;

            for (int i = 0; i < verticalRayCount; i ++)
            {
                Vector2 rayOrigin = (directionY == -1)?raycastOrigins.bottomLeft:raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (verticalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.up*directionY,rayLength,passengersMask);

                if (hit)
                {
                    if (!movedPassengers.Contains(hit.transform)){
                        movedPassengers.Add(hit.transform);
                        float pushX = (directionY == 1)?velocity.x:0;
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;
                        
                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX,pushY), directionY == 1, true));
                        //hit.transform.Translate(new Vector3(pushX,pushY));
                    }
                }
            }
        }

        //horizontally moving platform

        if (velocity.x != 0) {
            float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        //do something for each ray
        for (int i = 0; i < horizontalRayCount; i ++){
            //sets which ray origin to look at if the directionX is left or right
            Vector2 rayOrigin = (directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight;

            //sets where each ray starts
            rayOrigin += Vector2.up * (horitalRaySpacing * i);

            //calculate if it hit something (origin,direction, length of the ray, mask (this is the layer))
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.right*directionX,rayLength,passengersMask);
            if (hit)
                {
                    if (!movedPassengers.Contains(hit.transform)){
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        float pushY = -skinWidth;
                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX,pushY), false, true));
                        //hit.transform.Translate(new Vector3(pushX,pushY));
                    }
                }
            }
        }

        //passenger on top of horizontally or downward moving platform
        if (directionY == -1 || velocity.y == 0 && velocity.x != 0){
            float rayLength = skinWidth * 2;

            for (int i = 0; i < verticalRayCount; i ++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.up,rayLength,passengersMask);

                if (hit)
                {
                    if (!movedPassengers.Contains(hit.transform)){
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x;
                        float pushY = velocity.y;
                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX,pushY), true, false));
                        //hit.transform.Translate(new Vector3(pushX,pushY));
                    }
                }
            }
        }
    }

    struct PassengerMovement{
        public Transform transform;
        public Vector3 velocity;
        public bool standingOnPlatform;
        public bool moveBeforePlatform;

        public PassengerMovement(Transform _transform,Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform){
            transform = _transform;
            velocity = _velocity;
            standingOnPlatform = _standingOnPlatform;
            moveBeforePlatform = _moveBeforePlatform;
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
