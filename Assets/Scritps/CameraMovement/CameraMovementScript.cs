using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovementScript : MonoBehaviour
{
    float speed = 0.06f;
    float zoomSpeed = 10f;
    float rotationSpeed = 1f;

    float maxHeight = 40f;
    float minHeight = 2f;

    Vector3 mPos1;
    Vector3 mPos2;
    
    void Update()
    {
        
        getCameraRotation();

        if(Input.GetKey(KeyCode.LeftShift))
        {
            speed = 0.06f;
            zoomSpeed = 20f;
        }
        else
        {
            speed = 0.35f;
            zoomSpeed = 10f;
        }

        float hSpeed = transform.position.y * Input.GetAxis("Horizontal") * Time.deltaTime * 5f;
        float vSpeed = transform.position.y * Input.GetAxis("Vertical") * Time.deltaTime * 5f;
        float scrollSpeed = -zoomSpeed * Input.GetAxis("Mouse ScrollWheel");

        if(transform.position.y >= maxHeight && scrollSpeed > 0)
        {
            scrollSpeed = 0;
        }
        if(transform.position.y <= minHeight && scrollSpeed < 0)
        {
            scrollSpeed = 0;
        }

        Vector3 verticalMove = new Vector3(0, scrollSpeed, 0);
        Vector3 lateralMove = hSpeed * transform.right;
        Vector3 forwardMove = transform.forward;

        forwardMove.y = 0;
        forwardMove.Normalize();
        forwardMove *= vSpeed;

        Vector3 move = (verticalMove + lateralMove + forwardMove);

        transform.position += move;
    }

    
    void getCameraRotation()
    {
        if(Input.GetMouseButtonDown(2))
        {
            mPos1 = Input.mousePosition;
        }
        if(Input.GetMouseButton(2))
        {
            mPos2 = Input.mousePosition;
            float dx = (mPos1 - mPos2).x * rotationSpeed;
            float dy = (mPos1 - mPos2).y * rotationSpeed;

            transform.rotation *= Quaternion.Euler(new Vector3(0, -dx, 0));// y rotation
            transform.GetChild(0).transform.rotation *= Quaternion.Euler(new Vector3(dy, 0, 0)); 

            mPos1 = mPos2;
        }
    }
}
