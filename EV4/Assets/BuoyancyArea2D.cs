using UnityEngine;
using PUCV.PhysicEngine2D;

public class BuoyancyArea2D : MonoBehaviour
{
    [Header("Propiedades del fluido")]
    [Tooltip("Escala de la fuerza de empuje. Valores altos = más flotabilidad.")]
    public float fluidDensity = 10f;

    [Tooltip("Resistencia del fluido sobre la velocidad.")]
    public float damping = 1f;

    [Tooltip("Offset vertical de la superficie dentro del sprite.")]
    public float surfaceYOffset = 0f;

    private Bounds _bounds;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        RecalculateBounds();
    }

    private void OnEnable()
    {
        // Registrar esta área en el PhysicsManager2D
        PhysicsManager2D.RegisterBuoyancyArea(this);
    }

    private void OnDisable()
    {
        // Desregistrar al desactivar/destruir el lago
        PhysicsManager2D.UnregisterBuoyancyArea(this);
    }

    private void LateUpdate()
    {
        // Por si se mueve o escala el lago en tiempo de ejecución
        RecalculateBounds();
    }

    private void RecalculateBounds()
    {
        if (_sr != null)
            _bounds = _sr.bounds;
        else
            _bounds = new Bounds(transform.position, new Vector3(5f, 2f, 0f));
    }

    public bool IsInside(Vector2 worldPos)
    {
        bool inside = _bounds.Contains(worldPos);
        // Debug opcional para verificar
        // Debug.Log($"Buoyancy IsInside? {inside} pos={worldPos} bounds={_bounds}");
        return inside;
    }

    public float GetSurfaceY()
    {
        return _bounds.max.y + surfaceYOffset;
    }

    public Vector2 ComputeBuoyancyForce(CustomRigidbody2D rb)
    {
        Vector2 pos = rb.GetWorldPosition();
        float surfaceY = GetSurfaceY();

        // Si está por encima de la superficie, no hay empuje
        if (pos.y >= surfaceY)
            return Vector2.zero;

        // Profundidad sumergida aprox.
        float depth = surfaceY - pos.y; // >0 bajo la superficie
        depth = Mathf.Clamp(depth, 0f, _bounds.size.y);

        // Empuje proporcional a la profundidad normalizada
        float t = depth / Mathf.Max(_bounds.size.y, 0.0001f);
        float buoyantForce = fluidDensity * t;

        Vector2 up = Vector2.up;
        Vector2 drag = -rb.velocity * damping;

        return up * buoyantForce + drag;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Dibujar bounds aproximado del lago
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _bounds = _sr.bounds;

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f);
        Gizmos.DrawCube(_bounds.center, _bounds.size);

        // Superficie
        Gizmos.color = Color.cyan;
        float surfaceY = _bounds.max.y + surfaceYOffset;
        Vector3 left = new Vector3(_bounds.min.x, surfaceY, 0f);
        Vector3 right = new Vector3(_bounds.max.x, surfaceY, 0f);
        Gizmos.DrawLine(left, right);
    }
#endif
}

