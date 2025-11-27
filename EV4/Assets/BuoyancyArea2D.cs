using UnityEngine;
using PUCV.PhysicEngine2D;

public class BuoyancyArea2D : MonoBehaviour
{
    [Header("Propiedades del fluido")]
    public float fluidDensity = 1f;          // Ajusta cuánto empuja
    public float damping = 0.5f;             // Resistencia del fluido
    public float surfaceYOffset = 0f;        // Permite mover la "superficie" dentro del sprite

    private Bounds _bounds;

    private void Awake()
    {
        RecalculateBounds();

    }

    private void RecalculateBounds()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            _bounds = sr.bounds;
        else
            _bounds = new Bounds(transform.position, new Vector3(5f, 2f, 0f)); // fallback
    }

    public bool IsInside(Vector2 worldPos)
    {
        return _bounds.Contains(worldPos);
    }

    public float GetSurfaceY()
    {
        return _bounds.max.y + surfaceYOffset;
    }

    public Vector2 ComputeBuoyancyForce(CustomRigidbody2D rb)
    {
        Vector2 pos = rb.GetWorldPosition();
        float surfaceY = GetSurfaceY();

        // Si está totalmente fuera por arriba, no hay empuje
        if (pos.y >= surfaceY) return Vector2.zero;

        // Profundidad sumergida aprox
        float depth = surfaceY - pos.y;   // >0 si está bajo la superficie
        depth = Mathf.Clamp(depth, 0f, _bounds.size.y);

        // Empuje proporcional a profundidad
        float buoyantForce = fluidDensity * depth;
        Vector2 up = new Vector2(0f, 1f);

        // Resistencia (drag lineal simple)
        Vector2 drag = -rb.velocity * damping;

        return up * buoyantForce + drag;
    }
}

