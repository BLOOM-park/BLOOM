using UnityEngine;

public class ObjectGrabbable : MonoBehaviour
{
    // [Header("Item Data")]
    // public ItemData itemData; // assign in inspector - Removed as ItemData was deleted

    private Rigidbody objectRigidbody;
    private Transform objectGrabPointTransform;

    private void Awake()
    {
        objectRigidbody = GetComponent<Rigidbody>();
    }

    public void Grab(Transform grabPoint)
    {
        objectGrabPointTransform = grabPoint;
        objectRigidbody.useGravity = false;
    }

    public void Drop()
    {
        objectGrabPointTransform = null;
        objectRigidbody.useGravity = true;
    }

    private void FixedUpdate()
    {
        if (objectGrabPointTransform != null)
        {
            float lerpSpeed = 10f;
            Vector3 newPosition = Vector3.Lerp(transform.position, objectGrabPointTransform.position, Time.fixedDeltaTime * lerpSpeed);
            objectRigidbody.MovePosition(newPosition);
        }
    }
}
