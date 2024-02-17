using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowObject : MonoBehaviour
{
    [SerializeField] public GameObject ThrowableRef;
    [SerializeField] public Transform firePoint;
    public float throwForce = 10f;
    public float throwUpwardForce = 10f;
    private Collider closestTarget;
    public bool isThrowing = false;
    bool wait = false;

    private void Update()
    {
        closestTarget = GetComponent<FieldOfView>().closestTarget;

        if (closestTarget && !wait)
        {
            isThrowing = true;
            ThrowObj();
            StartCoroutine(Delay(5f));
        }
        isThrowing = false;
    }
    void ThrowObj()
    {
        GameObject throwable = Instantiate(ThrowableRef, firePoint.transform.forward + transform.position, ThrowableRef.transform.rotation);

        //addforce
        Vector3 addForce = firePoint.transform.forward * throwForce + firePoint.transform.up * throwUpwardForce;

        throwable.GetComponent<Rigidbody>().AddForce(addForce, ForceMode.Impulse);

        //ignore collision between this game object and throwable object
        Physics.IgnoreCollision(throwable.GetComponent<Collider>(), gameObject.GetComponent<Collider>(), true);

        Destroy(throwable, 3f);
    }

    IEnumerator Delay(float ammount)
    {
        wait = true;
        yield return new WaitForSeconds(ammount);
        wait = false;
    }
}
