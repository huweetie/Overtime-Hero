using UnityEngine;

namespace OfficeRumble
{
    public class UnitBase : MonoBehaviour
    {
        [Header("소속 및 타겟팅")]
        public bool isPlayerTeam = true;
        public CardData data;

        protected float currentHealth;
        protected float attackTimer = 0f;
        protected GameObject currentTarget = null;
        protected bool isDead = false;

        protected virtual void Start()
        {
            if (data != null)
            {
                currentHealth = data.maxHealth;
            }
            else
            {
                currentHealth = 100f;
            }
        }

        protected virtual void Update()
        {
            if (isDead) return;

            attackTimer += Time.deltaTime;

            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                FindNearestTarget();
            }

            if (currentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
                float requiredRange = (data != null) ? data.attackRange : 1.5f;

                if (distance <= requiredRange)
                {
                    // 사거리 내 진입: 공격
                    float interval = (data != null) ? data.attackInterval : 1.0f;
                    if (attackTimer >= interval)
                    {
                        PerformAttack(currentTarget);
                        attackTimer = 0f;
                    }
                }
                else
                {
                    // 타겟 방향으로 전진
                    MoveTowards(currentTarget.transform.position);
                }
            }
            else
            {
                // 적이 없으면 적 사옥/타워 방향으로 기본 직진
                Vector3 defaultDirection = isPlayerTeam ? Vector3.forward : Vector3.back;
                float speed = (data != null) ? data.moveSpeed : 3.0f;
                transform.position += defaultDirection * (speed * Time.deltaTime);
            }
        }

        protected virtual void MoveTowards(Vector3 destination)
        {
            float speed = (data != null) ? data.moveSpeed : 3.0f;
            Vector3 direction = (destination - transform.position).normalized;
            direction.y = 0; // 평면 이동
            transform.position += direction * (speed * Time.deltaTime);
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
            }
        }

        protected virtual void PerformAttack(GameObject target)
        {
            float damage = (data != null) ? data.attackDamage : 20f;
            
            var targetUnit = target.GetComponent<UnitBase>();
            if (targetUnit != null)
            {
                targetUnit.TakeDamage(damage);
                return;
            }

            var targetTower = target.GetComponent<Tower>();
            if (targetTower != null)
            {
                targetTower.TakeDamage(damage);
            }
        }

        public virtual void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            isDead = true;
            Destroy(gameObject, 0.2f);
        }

        protected virtual void FindNearestTarget()
        {
            // 전장 내 유닛 및 타워 탐색
            UnitBase[] allUnits = FindObjectsOfType<UnitBase>();
            float closestDist = float.MaxValue;
            GameObject bestTarget = null;

            foreach (var u in allUnits)
            {
                if (u.isPlayerTeam != this.isPlayerTeam && !u.isDead)
                {
                    float d = Vector3.Distance(transform.position, u.transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        bestTarget = u.gameObject;
                    }
                }
            }

            // 유닛이 없으면 적 타워 탐색
            if (bestTarget == null)
            {
                Tower[] allTowers = FindObjectsOfType<Tower>();
                foreach (var t in allTowers)
                {
                    if (t.isPlayerTower != this.isPlayerTeam && !t.IsDestroyed)
                    {
                        float d = Vector3.Distance(transform.position, t.transform.position);
                        if (d < closestDist)
                        {
                            closestDist = d;
                            bestTarget = t.gameObject;
                        }
                    }
                }
            }

            currentTarget = bestTarget;
        }

        protected virtual bool IsTargetValid(GameObject target)
        {
            if (target == null) return false;
            var u = target.GetComponent<UnitBase>();
            if (u != null && u.isDead) return false;
            var t = target.GetComponent<Tower>();
            if (t != null && t.IsDestroyed) return false;
            return true;
        }
    }
}
