using UnityEngine;
using PUCV.PhysicEngine2D;
using UnityEngine.InputSystem;

public class SlingshotController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform anchorPoint;             // Centro de la resortera
    public GameObject projectilePrefab;       // Prefab con CustomRigidbody2D + CustomCollider2D
    public PhysicsManager2D physicsManager;   // Para leer gravedad

    [Header("Ajustes de disparo")]
    public float maxPullDistance = 3f;
    public float launchPower = 10f;

    [Header("Debug")]
    public int trajectorySegments = 30;

    private CustomRigidbody2D _currentRb;
    private Vector2 _currentLaunchVelocity;
    private bool _isDragging;

    private void Start()
    {
        SpawnNewProjectile();
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            StartDrag();
        else if (Mouse.current.leftButton.isPressed && _isDragging)
            UpdateDrag();
        else if (Mouse.current.leftButton.wasReleasedThisFrame && _isDragging)
            Release();
    }
    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(mousePos);
        world.z = 0f;
        return world;
    }
    private void StartDrag()
    {
        _isDragging = true;
    }

    private void UpdateDrag()
    {
        Vector3 mouseWorld = GetMouseWorldPosition();
        mouseWorld.z = 0f;

        Vector2 anchorPos = anchorPoint.position;
        Vector2 pullDir = (Vector2)mouseWorld - anchorPos;

        // Limitar distancia de estiramiento
        if (pullDir.magnitude > maxPullDistance)
            pullDir = pullDir.normalized * maxPullDistance;

        // Coloca el proyectil en el punto arrastrado
        if (_currentRb != null)
        {
            _currentRb.SetWoldPosition(anchorPos + pullDir);
            _currentRb.velocity = Vector2.zero;
        }

        // La velocidad de lanzamiento apunta en la direcci�n opuesta
        _currentLaunchVelocity = -pullDir * launchPower;
    }

    private void Release()
    {
        _isDragging = false;
        if (_currentRb != null)
        {
            _currentRb.velocity = _currentLaunchVelocity;
            _currentRb = null;

            
            Invoke(nameof(SpawnNewProjectile), 1.5f);
        }
    }

    private void SpawnNewProjectile()
    {
        if (projectilePrefab == null || anchorPoint == null) return;

        GameObject go = Instantiate(projectilePrefab, anchorPoint.position, Quaternion.identity);
        _currentRb = go.GetComponent<CustomRigidbody2D>();
        if (_currentRb == null)
            _currentRb = go.AddComponent<CustomRigidbody2D>();

        _currentRb.velocity = Vector2.zero;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || _currentRb == null) return;
        if (physicsManager == null) return;

        // Dibujar la trayectoria usando la gravedad del motor
        Vector2 g = physicsManager.globalGravity;

        Gizmos.color = Color.yellow;

        Vector2 startPos = _currentRb.GetWorldPosition();
        Vector2 v0 = _currentLaunchVelocity;

        float dt = 0.1f;
        Vector3 prev = startPos;

        for (int i = 1; i <= trajectorySegments; i++)
        {
            float t = i * dt;
            // x(t) = x0 + v0*t + 0.5*g*t^2
            Vector2 p = startPos + v0 * t + 0.5f * g * (t * t);

            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}

