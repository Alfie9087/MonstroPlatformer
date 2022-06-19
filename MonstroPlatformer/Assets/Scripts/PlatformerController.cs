using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerController : RaycastController
{
    // Start is called before the first frame update
    public LayerMask passengersMask;
    public Vector3 move;
    List<PassengerMovement> passengerMovement;
    Dictionary<Transform,Controller2D> passengerDictionary = new Dictionary<Transform,Controller2D>();

    public override void Start()
    {
        base.Start();

    }

    // Update is called once per frame
    void Update()
    {
        UpdateRaycastOrigins();

        Vector3 velocity = move * Time.deltaTime;

        CalculatePassengerMoement(velocity);
        MovePassengers(true);

        transform.Translate(velocity);
        MovePassengers(false);
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
}
