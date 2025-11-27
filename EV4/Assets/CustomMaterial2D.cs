using UnityEngine;
using PUCV.PhysicEngine2D;

public class CustomMaterial2D : MonoBehaviour
{
    [Range(0f, 1.2f)]
    public float bounciness = 0.5f;   // 1 = perfectamente elástico, <1 = inelástico

    [Range(0f, 1f)]
    public float friction = 0.2f;

    public static float GetCombinedBounciness(PUCV.PhysicEngine2D.CustomCollider2D a, PUCV.PhysicEngine2D.CustomCollider2D b)
    {
        var ma = a.GetComponent<CustomMaterial2D>();
        var mb = b.GetComponent<CustomMaterial2D>();

        float ba = ma ? ma.bounciness : 0.3f;
        float bb = mb ? mb.bounciness : 0.3f;

        // Puedes usar promedio o max
        return (ba + bb) * 0.5f;
    }
}

