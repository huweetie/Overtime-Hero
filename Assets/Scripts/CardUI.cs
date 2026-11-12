using UnityEngine;
using UnityEngine.UI;

namespace OfficeRumble
{
    public class CardUI : MonoBehaviour
    {
        [Header("카드 데이터 및 UI 컴포넌트")]
        public CardData cardData;
        public Image iconImage;
        public Text costText;
        public Text nameText;
        public Button spawnButton;

        [Header("소환 스폰 위치")]
        public Transform playerSpawnPoint;

        private void Start()
        {
            if (cardData != null)
            {
                if (nameText != null) nameText.text = cardData.cardName;
                if (costText != null) costText.text = cardData.caffeineCost.ToString();
                if (iconImage != null && cardData.cardIcon != null) iconImage.sprite = cardData.cardIcon;
            }

            if (spawnButton != null)
            {
                spawnButton.onClick.AddListener(OnCardClicked);
            }
        }

        public void OnCardClicked()
        {
            if (cardData == null) return;

            if (ManaManager.Instance != null && ManaManager.Instance.TrySpendCaffeine(cardData.caffeineCost))
            {
                SpawnUnit();
            }
            else
            {
                Debug.Log("[CardUI] 카페인이 부족합니다!");
            }
        }

        private void SpawnUnit()
        {
            if (cardData.unitPrefab == null) return;

            Vector3 spawnPos = (playerSpawnPoint != null) ? playerSpawnPoint.position : Vector3.zero;

            for (int i = 0; i < cardData.spawnCount; i++)
            {
                Vector3 offset = new Vector3((i - (cardData.spawnCount - 1) * 0.5f) * 0.8f, 0, 0);
                GameObject unitObj = Instantiate(cardData.unitPrefab, spawnPos + offset, Quaternion.identity);
                UnitBase unit = unitObj.GetComponent<UnitBase>();
                if (unit != null)
                {
                    unit.isPlayerTeam = true;
                    unit.data = cardData;
                }
            }

            Debug.Log($"[CardUI] {cardData.cardName} {cardData.spawnCount}명 소환 완료!");
        }
    }
}
