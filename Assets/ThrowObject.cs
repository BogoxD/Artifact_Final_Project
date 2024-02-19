using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowObject : MonoBehaviour
{
    [SerializeField] public GameObject ThrowableRef;
    [SerializeField] public Transform firePoint;
    public float projectileSpeed = 5f;
    public float throwForce = 12f;
    public float throwUpwardForce = 10f;
    private Collider closestTarget;
    bool wait = false;

    private void Update()
    {
        closestTarget = GetComponent<FieldOfView>().closestTarget;

        if (closestTarget && !wait)
        {
            ThrowObj();
            StartCoroutine(Delay(5f));
        }
    }
    void ThrowObj()
    {
        GameObject throwable = Instantiate(ThrowableRef, firePoint.transform.forward + transform.position, ThrowableRef.transform.rotation);
        if (InterceptionDirection(closestTarget.transform.position, transform.position,
            closestTarget.GetComponent<Rigidbody>().velocity, projectileSpeed, out var direction))
        {
            Vector3 addForce = firePoint.transform.forward * throwForce + firePoint.transform.up * throwUpwardForce;
            throwable.GetComponent<Rigidbody>().velocity = direction * projectileSpeed;
            throwable.GetComponent<Rigidbody>().AddForce(addForce, ForceMode.Impulse);
            Vector3.Lerp(throwable.transform.position, closestTarget.transform.position, 5f);

        }

        Destroy(throwable, 5f);
    }
    IEnumerator Delay(float ammount)
    {
        wait = true;
        yield return new WaitForSeconds(ammount);
        wait = false;
    }

    public bool InterceptionDirection(Vector3 a, Vector3 b, Vector3 vA, float sB, out Vector3 result)
    {
        var aToB = b - a;
        var dC = aToB.magnitude;
        var alpha = Vector3.Angle(aToB, vA) * Mathf.Deg2Rad;
        var sA = vA.magnitude;
        var r = sA / sB;

        if (Math.SolveQuadratic(1 - r * r, 2 * r * dC * Mathf.Cos(alpha), -(dC * dC), out var root1, out var root2) == 0)
        {
            result = Vector3.zero;
            return false;
        }

        var dA = Mathf.Max(root1, root2);
        var t = dA / sB;
        var c = a + vA * t;

        result = (c - b).normalized;
        return true;


    }

}
public class Math
{
    public static int SolveQuadratic(float a, float b, float c, out float root1, out float root2)
    {
        var discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
        {
            root1 = Mathf.Infinity;
            root2 = -root1;

            return 0;
        }

        root1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        root2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

        return discriminant > 0 ? 2 : 1;
    }

}
