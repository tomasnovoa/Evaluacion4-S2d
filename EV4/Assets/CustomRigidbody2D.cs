using UnityEngine;

namespace PUCV.PhysicEngine2D
{
    public class CustomRigidbody2D : MonoBehaviour
    {
        [Header("Estado")]
        [Tooltip("Si es true, este cuerpo NO es afectado por gravedad ni movimiento automático.")]
        public bool isKinematic = false;

        [Tooltip("Si es true y no es kinematic, se le aplica gravedad global.")]
        public bool useGravity = true;

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

