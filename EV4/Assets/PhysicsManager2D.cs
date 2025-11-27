using System.Collections.Generic;
using UnityEngine;

namespace PUCV.PhysicEngine2D
{
    public class PhysicsManager2D : MonoBehaviour
    {
        private static PhysicsManager2D _instance;

        // Todos los colliders registrados en el motor
        private readonly List<CustomCollider2D> _colliders = new List<CustomCollider2D>();

        // Colisiones detectadas en el frame actual
        private List<InternalCollisionInfo> _currentCollisionList = new List<InternalCollisionInfo>();

        [Header("Gravedad global")]
        public Vector2 globalGravity = new Vector2(0f, -9.81f);

        // Áreas de flotabilidad (lagos)
        private static readonly List<BuoyancyArea2D> _buoyancyAreas = new List<BuoyancyArea2D>();

        private void Awake()
        {
            if (_instance != null)
            {
                DestroyImmediate(this);
                return;
            }

            // Singleton
            _instance = this;
            DontDestroyOnLoad(_instance);
        }

        // ================== Registro de colliders ==================

        public static void RegisterCollider(CustomCollider2D customCollider2D)
        {
            if (_instance == null || customCollider2D == null) return;

            if (!_instance._colliders.Contains(customCollider2D))
                _instance._colliders.Add(customCollider2D);
        }

        public static void UnregisterCollider(CustomCollider2D customCollider2D)
        {
            if (_instance == null || customCollider2D == null) return;

            _instance._colliders.Remove(customCollider2D);
        }

        // ================== Registro de áreas de flotabilidad ==================

        public static void RegisterBuoyancyArea(BuoyancyArea2D area)
        {
            if (area == null) return;
            if (!_buoyancyAreas.Contains(area))
                _buoyancyAreas.Add(area);
        }

        public static void UnregisterBuoyancyArea(BuoyancyArea2D area)
        {
            if (area == null) return;
            _buoyancyAreas.Remove(area);
        }

        // ================== Ciclo de simulación ==================

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            StepApplyGlobalForces(deltaTime);                  // Gravedad + flotabilidad
            StepCalculateCollisions(deltaTime);                // Detección SAT
            StepApplyMTVAndReflectionToRigidbodies(deltaTime); // Corrección de penetración + rebote
            StepApplyMovementToRigidbodies(deltaTime);         // Integración de posición
            StepInformCollisions(deltaTime);                   // Eventos de colisión
        }

        // ---------- 1) Fuerzas globales ----------

        private void StepApplyGlobalForces(float deltaTime)
        {
            if (_colliders == null || _colliders.Count == 0) return;

            foreach (CustomCollider2D collider in _colliders)
            {
                if (collider == null) continue;

                CustomRigidbody2D rb = collider.rigidBody;
                if (rb == null) continue;
                if (rb.isKinematic) continue;   // No aplicar fuerzas a kinematic

                if (rb.useGravity)
                    ApplyGravity(rb, deltaTime);

                ApplyBuoyancyIfAny(rb, deltaTime);
            }
        }

        private void ApplyGravity(CustomRigidbody2D rb, float dt)
        {
            rb.velocity += globalGravity * dt;
        }

        private void ApplyBuoyancyIfAny(CustomRigidbody2D rb, float dt)
        {
            if (_buoyancyAreas == null || _buoyancyAreas.Count == 0) return;

            Vector2 pos = rb.GetWorldPosition();

            foreach (var area in _buoyancyAreas)
            {
                if (area == null) continue;
                if (!area.IsInside(pos)) continue;

                Vector2 force = area.ComputeBuoyancyForce(rb);
                rb.velocity += force * dt; // masa = 1
            }
        }

        // ---------- 2) Detección de colisiones ----------

        private void StepCalculateCollisions(float deltaTime)
        {
            // Usa SAT para detectar colisiones entre todos los colliders
            var currCollisionList = SAT2DMath.DetectCollisions(_colliders);

            // Marcar si la colisión ya existía en el frame anterior
            foreach (var currCollisionInfo in currCollisionList)
            {
                currCollisionInfo.wasCollidedLastFrame = false;

                foreach (var prevCollisionInfo in _currentCollisionList)
                {
                    bool sameOrder =
                        prevCollisionInfo.bodyACollider == currCollisionInfo.bodyACollider &&
                        prevCollisionInfo.bodyBCollider == currCollisionInfo.bodyBCollider;

                    bool swappedOrder =
                        prevCollisionInfo.bodyACollider == currCollisionInfo.bodyBCollider &&
                        prevCollisionInfo.bodyBCollider == currCollisionInfo.bodyACollider;

                    if (sameOrder || swappedOrder)
                    {
                        currCollisionInfo.wasCollidedLastFrame = true;
                        break;
                    }
                }
            }

            _currentCollisionList = currCollisionList;
        }

        // ---------- 3) Aplicar MTV y reflexión ----------

        private void StepApplyMTVAndReflectionToRigidbodies(float deltaTime)
        {
            foreach (var currCollisionInfo in _currentCollisionList)
            {
                var rbA = currCollisionInfo.bodyARigidbody;
                var rbB = currCollisionInfo.bodyBRigidbody;

                // Mover según MTV (solo cuerpos no kinematic)
                if (currCollisionInfo.hasMTV)
                {
                    if (rbA != null && !rbA.isKinematic)
                    {
                        Vector2 posA = rbA.GetWorldPosition();
                        posA += currCollisionInfo.mtvA;
                        rbA.SetWoldPosition(posA);
                    }

                    if (rbB != null && !rbB.isKinematic)
                    {
                        Vector2 posB = rbB.GetWorldPosition();
                        posB += currCollisionInfo.mtvB;
                        rbB.SetWoldPosition(posB);
                    }
                }

                // Rebote elástico / inelástico usando CustomMaterial2D
                float e = CustomMaterial2D.GetCombinedBounciness(
                    currCollisionInfo.bodyACollider,
                    currCollisionInfo.bodyBCollider
                );

                // Normal de A hacia B (ya normalizada)
                Vector2 nAB = currCollisionInfo.contactNormalBA;

                if (rbA != null && !rbA.isKinematic)
                {
                    Vector2 v = rbA.velocity;
                    float vn = Vector2.Dot(v, nAB);

                    // Solo si se acerca a la superficie
                    if (vn < 0f)
                    {
                        Vector2 vPrime = v - (1f + e) * vn * nAB;
                        rbA.velocity = vPrime;
                    }
                }

                if (rbB != null && !rbB.isKinematic)
                {
                    Vector2 nBA = -nAB;
                    Vector2 v = rbB.velocity;
                    float vn = Vector2.Dot(v, nBA);

                    if (vn < 0f)
                    {
                        Vector2 vPrime = v - (1f + e) * vn * nBA;
                        rbB.velocity = vPrime;
                    }
                }
            }
        }

        // ---------- 4) Movimiento ----------

        private void StepApplyMovementToRigidbodies(float deltaTime)
        {
            if (_colliders == null || _colliders.Count == 0) return;

            foreach (CustomCollider2D collider in _colliders)
            {
                if (collider == null) continue;

                CustomRigidbody2D rigidbody = collider.rigidBody;
                if (rigidbody == null) continue;
                if (rigidbody.isKinematic) continue;   // No mover kinematic

                Vector2 rigidbodyPos = rigidbody.GetWorldPosition();
                rigidbodyPos += rigidbody.velocity * deltaTime;
                rigidbody.SetWoldPosition(rigidbodyPos);
            }
        }

        // ---------- 5) Eventos de colisión ----------

        private void StepInformCollisions(float deltaTime)
        {
            foreach (var currCollisionInfo in _currentCollisionList)
            {
                // Solo disparar evento cuando la colisión empezó este frame
                if (currCollisionInfo.wasCollidedLastFrame) continue;

                CollisionInfo a = currCollisionInfo.GetCollInfoForBodyA();
                CollisionInfo b = currCollisionInfo.GetCollInfoForBodyB();

                currCollisionInfo.bodyACollider.InformOnCollisionEnter2D(a);
                currCollisionInfo.bodyBCollider.InformOnCollisionEnter2D(b);
            }
        }
    }

    // ================== Clases de datos de colisión ==================

    public class InternalCollisionInfo
    {
        public CustomCollider2D bodyACollider;
        public CustomRigidbody2D bodyARigidbody;

        public CustomCollider2D bodyBCollider;
        public CustomRigidbody2D bodyBRigidbody;

        public bool wasCollidedLastFrame;

        // Minimum Translation Vector
        public bool hasMTV;
        public Vector2 mtvA;
        public Vector2 mtvB;

        public Vector2 contactPoint;
        public Vector2 contactNormalAB; // Desde A
        public Vector2 contactNormalBA; // Desde B

        public InternalCollisionInfo(
            CustomCollider2D colA,
            CustomCollider2D colB,
            Vector2 point,
            Vector2 normal
        )
        {
            bodyACollider = colA;
            bodyARigidbody = colA ? colA.rigidBody : null;

            bodyBCollider = colB;
            bodyBRigidbody = colB ? colB.rigidBody : null;

            contactPoint = point;

            // Guardar normal en ambos sentidos
            contactNormalAB = normal;
            contactNormalBA = -normal;
        }

        public CollisionInfo GetCollInfoForBodyA()
        {
            return new CollisionInfo()
            {
                otherCollider = bodyBCollider,
                otherRigidbody = bodyBRigidbody,
                contactPoint = contactPoint,
                contactNormal = contactNormalAB
            };
        }

        public CollisionInfo GetCollInfoForBodyB()
        {
            return new CollisionInfo()
            {
                otherCollider = bodyACollider,
                otherRigidbody = bodyARigidbody,
                contactPoint = contactPoint,
                contactNormal = contactNormalBA
            };
        }
    }

    public class CollisionInfo
    {
        public CustomCollider2D otherCollider;
        public CustomRigidbody2D otherRigidbody;
        public Vector2 contactPoint;
        public Vector2 contactNormal;
    }
}


