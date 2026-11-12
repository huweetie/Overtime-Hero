using System;
using UnityEngine;

namespace OfficeRumble
{
    public enum GameState
    {
        Ready,
        InBattle,
        Victory,
        Defeat
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("경기 시간 설정")]
        [SerializeField] private float matchDuration = 180f; // 3분 경기
        [SerializeField] private float feverTimeThreshold = 60f; // 종료 1분 전

        public GameState CurrentState { get; private set; } = GameState.Ready;
        public float RemainingTime { get; private set; }

        public event Action<GameState> OnGameStateChanged;
        public event Action<float> OnTimerTick;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            RemainingTime = matchDuration;
        }

        private void Start()
        {
            StartBattle();
        }

        public void StartBattle()
        {
            CurrentState = GameState.InBattle;
            RemainingTime = matchDuration;
            OnGameStateChanged?.Invoke(CurrentState);
        }

        private void Update()
        {
            if (CurrentState != GameState.InBattle) return;

            RemainingTime -= Time.deltaTime;
            OnTimerTick?.Invoke(RemainingTime);

            if (RemainingTime <= feverTimeThreshold)
            {
                ManaManager.Instance?.SetFeverTime(true);
            }

            if (RemainingTime <= 0)
            {
                EvaluateTimeOutWinner();
            }
        }

        public void HandleTowerDestroyed(bool isPlayerTower)
        {
            if (CurrentState != GameState.InBattle) return;

            if (isPlayerTower)
            {
                CurrentState = GameState.Defeat;
                Debug.Log("[GameManager] 패배! 야근을 피하지 못했습니다...");
            }
            else
            {
                CurrentState = GameState.Victory;
                Debug.Log("[GameManager] 승리! 칼퇴 성공!");
            }

            OnGameStateChanged?.Invoke(CurrentState);
        }

        private void EvaluateTimeOutWinner()
        {
            // 타워 체력 비교 판정
            Tower[] towers = FindObjectsOfType<Tower>();
            float playerHp = 0f;
            float enemyHp = 0f;

            foreach (var t in towers)
            {
                if (t.isPlayerTower) playerHp = t.CurrentHealth;
                else enemyHp = t.CurrentHealth;
            }

            if (playerHp > enemyHp) CurrentState = GameState.Victory;
            else if (playerHp < enemyHp) CurrentState = GameState.Defeat;
            else CurrentState = GameState.Victory; // 무승부 시 판정승

            OnGameStateChanged?.Invoke(CurrentState);
        }
    }
}
