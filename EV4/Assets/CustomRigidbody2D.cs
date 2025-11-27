using UnityEngine;

namespace PUCV.PhysicEngine2D
{
    public class CustomRigidbody2D : MonoBehaviour
    {
        [Header("Estado")]
        
        public bool isKinematic = false;
        public bool useGravity = true;

        [Header("Masa")]
        
        [Min(0f)]
        public float mass = 1f;
        public float inverseMass
        {
            get
            {
                if (isKinematic || mass <= 0f)
                    return 0f;           // infinito (no se acelera con fuerzas)
                return 1f / mass;
            }
        }

        [Header("Movimiento")]
        public Vector2 velocity;

        private CustomCollider2D _customCollider;

        private void Awake()
        {
            // Cachear collider y registrar referencia cruzada
            _customCollider = GetComponent<CustomCollider2D>();
            if (_customCollider != null)
            {
                _customCollider.AddRigidbodyReference(this);
            }
        }

        public Vector2 GetWorldPosition()
        {
            return transform.position;
        }

        public void SetWoldPosition(Vector2 newPos)
        {
            transform.position = newPos;
        }

        public CustomCollider2D GetCollider()
        {
            return _customCollider;
        }
    }
}

