using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]

public class Player : MonoBehaviour
{
    float gravity= -10;
    Vector3 velocity;


    // Start is called before the first frame update
    Controller2D controller;

    void Start()
    {
        controller = GetComponent<Controller2D>();
    }

    // Update is called once per frame
    void Update()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
