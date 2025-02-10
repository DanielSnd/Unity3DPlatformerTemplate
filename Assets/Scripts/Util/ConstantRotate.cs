using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantRotate : MonoBehaviour
{
    Rigidbody rb;
    bool hasRb = false;
    public Vector3 RotationToAdd = new Vector3(0.0f, 1.0f, 0.0f);
    void Awake() {
        hasRb = TryGetComponent(out rb);
    }
    
    void FixedUpdate() {
        if (hasRb) {
            rb.MoveRotation(Quaternion.Euler(rb.transform.rotation.eulerAngles + (RotationToAdd  * Time.deltaTime)));
        } else {
            transform.Rotate(RotationToAdd * Time.deltaTime);
        }
    }
}
