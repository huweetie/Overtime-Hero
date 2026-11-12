using System;
using UnityEngine;

namespace OfficeRumble
{
    public class ManaManager : MonoBehaviour
    {
        public static ManaManager Instance { get; private set; }

        [Header("카페인(마나) 설정")]
        [SerializeField] private float maxCaffeine = 10f;
        [SerializeField] private float startingCaffeine = 4f;
        [SerializeField] private float chargeRatePerSecond = 1.0f;

        public float CurrentCaffeine { get; private set; }
        public float MaxCaffeine => maxCaffeine;

        public event Action<float, float> OnCaffeineChanged; // (current, max)

        private bool isFeverTime = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            CurrentCaffeine = startingCaffeine;
        }

        private void Update()
        {
            float rate = chargeRatePerSecond * (isFeverTime ? 2.0f : 1.0f);
            if (CurrentCaffeine < maxCaffeine)
            {
                CurrentCaffeine = Mathf.Min(maxCaffeine, CurrentCaffeine + rate * Time.deltaTime);
                OnCaffeineChanged?.Invoke(CurrentCaffeine, maxCaffeine);
            }
        }

        public bool CanAfford(int cost)
        {
            return CurrentCaffeine >= cost;
        }

        public bool TrySpendCaffeine(int cost)
        {
            if (CanAfford(cost))
            {
                CurrentCaffeine -= cost;
                OnCaffeineChanged?.Invoke(CurrentCaffeine, maxCaffeine);
                return true;
            }
            return false;
        }

        public void AddCaffeine(float amount)
        {
            CurrentCaffeine = Mathf.Min(maxCaffeine, CurrentCaffeine + amount);
            OnCaffeineChanged?.Invoke(CurrentCaffeine, maxCaffeine);
        }

        public void SetFeverTime(bool active)
        {
            isFeverTime = active;
        }
    }
}
