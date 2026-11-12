using UnityEngine;

namespace OfficeRumble
{
    public enum CardType
    {
        Unit,
        Spell
    }

    public enum UnitTargetType
    {
        Ground,
        Tower,
        All
    }

    [CreateAssetMenu(fileName = "NewCardData", menuName = "OfficeRumble/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("기본 정보")]
        public string cardName = "인턴 사원";
        [TextArea(2, 4)]
        public string description = "열정 가득한 신입사원. 빠른 속도로 전진합니다.";
        public CardType cardType = CardType.Unit;
        public Sprite cardIcon;
        public int caffeineCost = 2;

        [Header("유닛 능력치 (Unit인 경우)")]
        public GameObject unitPrefab;
        public float maxHealth = 120f;
        public float attackDamage = 25f;
        public float attackRange = 1.5f;
        public float attackInterval = 1.0f;
        public float moveSpeed = 3.5f;
        public UnitTargetType targetPriority = UnitTargetType.All;
        public int spawnCount = 1; // 인턴의 경우 3명 동시 소환

        [Header("마법 효과 (Spell인 경우)")]
        public float spellRadius = 4f;
        public float spellPower = 50f;
        public float spellDuration = 4f;
    }
}
