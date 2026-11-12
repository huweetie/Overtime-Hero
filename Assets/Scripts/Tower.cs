using System;
using UnityEngine;

namespace OfficeRumble
{
    public class Tower : MonoBehaviour
    {
        [Header("타워 소속 및 체력")]
        public bool isPlayerTower = true; // true면 칼퇴 엘리베이터, false면 야근 본사
        public string towerName = "칼퇴 엘리베이터";
        [SerializeField] private float maxHealth = 1500f;

        public float CurrentHealth { get; private set; }
        public bool IsDestroyed => CurrentHealth <= 0;

        public event Action<float, float> OnHealthChanged;
        public event Action<bool> OnTowerDestroyed; // true면 플레이어 타워 파괴

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDestroyed) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0)
            {
                OnTowerDestroyed?.Invoke(isPlayerTower);
                GameManager.Instance?.HandleTowerDestroyed(isPlayerTower);
            }
        }
    }
}
