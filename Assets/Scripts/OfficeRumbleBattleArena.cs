using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OfficeRumble
{
    public class OfficeRumbleBattleArena : MonoBehaviour
    {
        public static OfficeRumbleBattleArena Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoBootstrap()
        {
            if (Instance == null && FindObjectOfType<OfficeRumbleBattleArena>() == null)
            {
                GameObject boot = new GameObject("OfficeRumbleBattleArena");
                boot.AddComponent<OfficeRumbleBattleArena>();
                DontDestroyOnLoad(boot);
            }
        }

        // --- Game Balance ---
        public const float LaneLeftX = -3.4f;
        public const float LaneCenterX = 0.0f;
        public const float LaneRightX = 3.4f;
        public const float PlayerBaseZ = -9.5f;
        public const float EnemyBaseZ = 9.5f;

        public int selectedLane = 1; // 0: Left, 1: Center, 2: Right
        public float caffeine = 4.0f;
        public const float MaxCaffeine = 10.0f;
        public float matchTimer = 180.0f; // 3분 경기
        public bool isGameOver = false;
        public bool isVictory = false;

        // Towers
        public TowerData playerMainTower;
        public TowerData playerSubLeft;
        public TowerData playerSubRight;

        public TowerData enemyMainTower;
        public TowerData enemySubLeft;
        public TowerData enemySubRight;

        // Unit lists
        public List<BattleUnit> playerUnits = new List<BattleUnit>();
        public List<BattleUnit> enemyUnits = new List<BattleUnit>();
        public List<FloatingText> floatingTexts = new List<FloatingText>();

        // Spawner timer
        private float enemySpawnTimer = 3.0f;
        private float coffeeCooldown = 0f;

        // Visual Shared Materials
        private Material matFloor;
        private Material matLane;
        private Material matPlayer;
        private Material matEnemy;
        private Material matPlayerTower;
        private Material matEnemyTower;
        private Material matGold;

        // Audio
        private AudioSource audioSource;
        private AudioClip clipSpawn;
        private AudioClip clipHit;
        private AudioClip clipLaser;
        private AudioClip clipWin;
        private AudioClip clipLose;

        public class TowerData
        {
            public string name;
            public GameObject go;
            public float hp;
            public float maxHp;
            public bool isMain;
            public bool isPlayer;
            public Vector3 position;
            public float attackTimer;
            public float attackInterval = 1.2f;
            public float attackDamage = 40f;
            public float attackRange = 5.0f;
        }

        public class BattleUnit
        {
            public string unitName;
            public GameObject go;
            public bool isPlayer;
            public int laneIndex; // 0, 1, 2
            public float hp;
            public float maxHp;
            public float attackDamage;
            public float moveSpeed;
            public float attackRange;
            public float attackInterval;
            public float attackTimer;
            public float flashTimer;
            public Material unitMat;
            public Color baseColor;
        }

        public class FloatingText
        {
            public string text;
            public Vector3 worldPos;
            public Color color;
            public float lifetime = 1.0f;
            public float maxLifetime = 1.0f;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupCameraAndLighting();
            CreateMaterials();
            BuildArena();
            InitAudio();
            ResetGame();
        }

        private void SetupCameraAndLighting()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            cam.transform.position = new Vector3(0f, 18.5f, -12.5f);
            cam.transform.rotation = Quaternion.Euler(54f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.14f, 0.19f, 1f);
            cam.fieldOfView = 52f;

            // Light
            Light light = FindObjectOfType<Light>();
            if (light == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
            }
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.color = new Color(1.0f, 0.96f, 0.88f);
            light.intensity = 1.6f;
        }

        private void CreateMaterials()
        {
            matFloor = CreateSolidMaterial(new Color(0.18f, 0.22f, 0.27f));
            matLane = CreateSolidMaterial(new Color(0.24f, 0.29f, 0.36f));
            matPlayer = CreateSolidMaterial(new Color(0.20f, 0.60f, 0.95f));
            matEnemy = CreateSolidMaterial(new Color(0.90f, 0.25f, 0.25f));
            matPlayerTower = CreateSolidMaterial(new Color(0.85f, 0.75f, 0.50f));
            matEnemyTower = CreateSolidMaterial(new Color(0.40f, 0.12f, 0.15f));
            matGold = CreateSolidMaterial(new Color(0.95f, 0.80f, 0.20f));
        }

        private Material CreateSolidMaterial(Color col)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Standard");

            Material mat = null;
            if (sh != null)
            {
                mat = new Material(sh);
            }
            else
            {
                GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mat = new Material(temp.GetComponent<Renderer>().sharedMaterial);
                DestroyImmediate(temp);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            mat.color = col;
            return mat;
        }

        private void BuildArena()
        {
            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "OfficeFloor";
            floor.transform.position = new Vector3(0f, -0.25f, 0f);
            floor.transform.localScale = new Vector3(11.5f, 0.5f, 25.0f);
            floor.GetComponent<Renderer>().material = matFloor;

            // 3 Lane Strips
            float[] laneX = { LaneLeftX, LaneCenterX, LaneRightX };
            for (int i = 0; i < 3; i++)
            {
                GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = "LaneStrip_" + i;
                strip.transform.position = new Vector3(laneX[i], 0.02f, 0f);
                strip.transform.localScale = new Vector3(2.2f, 0.05f, 23.5f);
                strip.GetComponent<Renderer>().material = matLane;
            }

            // River / Office Partition
            GameObject partition = GameObject.CreatePrimitive(PrimitiveType.Cube);
            partition.name = "CenterPartition";
            partition.transform.position = new Vector3(0f, 0.35f, 0f);
            partition.transform.localScale = new Vector3(11.2f, 0.7f, 0.4f);
            partition.GetComponent<Renderer>().material = CreateSolidMaterial(new Color(0.12f, 0.15f, 0.20f));

            // Bridges across lanes
            for (int i = 0; i < 3; i++)
            {
                GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bridge.name = "Bridge_" + i;
                bridge.transform.position = new Vector3(laneX[i], 0.05f, 0f);
                bridge.transform.localScale = new Vector3(2.0f, 0.1f, 1.4f);
                bridge.GetComponent<Renderer>().material = matGold;
            }
        }

        public void ResetGame()
        {
            // Clear existing units
            foreach (var u in playerUnits) if (u.go != null) Destroy(u.go);
            foreach (var u in enemyUnits) if (u.go != null) Destroy(u.go);
            playerUnits.Clear();
            enemyUnits.Clear();
            floatingTexts.Clear();

            // Destroy old towers
            if (playerMainTower?.go != null) Destroy(playerMainTower.go);
            if (playerSubLeft?.go != null) Destroy(playerSubLeft.go);
            if (playerSubRight?.go != null) Destroy(playerSubRight.go);
            if (enemyMainTower?.go != null) Destroy(enemyMainTower.go);
            if (enemySubLeft?.go != null) Destroy(enemySubLeft.go);
            if (enemySubRight?.go != null) Destroy(enemySubRight.go);

            // Rebuild Towers
            playerMainTower = SpawnTower("6시 칼퇴 엘리베이터", new Vector3(0f, 1.8f, PlayerBaseZ), new Vector3(3.0f, 3.6f, 2.2f), matPlayerTower, 1500f, true, true);
            playerSubLeft = SpawnTower("팀장석 A", new Vector3(LaneLeftX, 1.1f, PlayerBaseZ + 2.0f), new Vector3(1.8f, 2.2f, 1.8f), matPlayer, 600f, false, true);
            playerSubRight = SpawnTower("수석석 B", new Vector3(LaneRightX, 1.1f, PlayerBaseZ + 2.0f), new Vector3(1.8f, 2.2f, 1.8f), matPlayer, 600f, false, true);

            enemyMainTower = SpawnTower("대표이사 야근본사", new Vector3(0f, 1.8f, EnemyBaseZ), new Vector3(3.0f, 3.6f, 2.2f), matEnemyTower, 1500f, true, false);
            enemySubLeft = SpawnTower("감사실 A", new Vector3(LaneLeftX, 1.1f, EnemyBaseZ - 2.0f), new Vector3(1.8f, 2.2f, 1.8f), matEnemy, 600f, false, false);
            enemySubRight = SpawnTower("전략실 B", new Vector3(LaneRightX, 1.1f, EnemyBaseZ - 2.0f), new Vector3(1.8f, 2.2f, 1.8f), matEnemy, 600f, false, false);

            caffeine = 4.0f;
            matchTimer = 180.0f;
            isGameOver = false;
            isVictory = false;
            enemySpawnTimer = 3.0f;
            coffeeCooldown = 0f;
        }

        private TowerData SpawnTower(string name, Vector3 pos, Vector3 scale, Material mat, float hp, bool isMain, bool isPlayer)
        {
            GameObject tObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tObj.name = name;
            tObj.transform.position = pos;
            tObj.transform.localScale = scale;
            tObj.GetComponent<Renderer>().material = mat;

            // Floating banner box on top
            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "Banner";
            banner.transform.SetParent(tObj.transform);
            banner.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            banner.transform.localScale = new Vector3(0.9f, 0.25f, 0.9f);
            banner.GetComponent<Renderer>().material = isPlayer ? matPlayer : matEnemy;

            TowerData data = new TowerData
            {
                name = name,
                go = tObj,
                hp = hp,
                maxHp = hp,
                isMain = isMain,
                isPlayer = isPlayer,
                position = pos
            };
            return data;
        }

        private void Update()
        {
            if (isGameOver) return;

            // Timer & Fever
            matchTimer -= Time.deltaTime;
            bool isFever = matchTimer <= 60f;
            float caffeineRate = isFever ? 2.0f : 1.0f;

            if (caffeine < MaxCaffeine)
            {
                caffeine = Mathf.Min(MaxCaffeine, caffeine + caffeineRate * Time.deltaTime);
            }

            if (coffeeCooldown > 0f) coffeeCooldown -= Time.deltaTime;

            if (matchTimer <= 0f)
            {
                EvaluateTimeoutWinner();
                return;
            }

            // Enemy AI Spawner
            enemySpawnTimer -= Time.deltaTime;
            float spawnInterval = isFever ? UnityEngine.Random.Range(2.2f, 3.8f) : UnityEngine.Random.Range(3.8f, 6.0f);
            if (enemySpawnTimer <= 0f)
            {
                enemySpawnTimer = spawnInterval;
                SpawnEnemyAI();
            }

            // Tower Attacks
            UpdateTowerAttack(playerMainTower, enemyUnits);
            UpdateTowerAttack(playerSubLeft, enemyUnits);
            UpdateTowerAttack(playerSubRight, enemyUnits);

            UpdateTowerAttack(enemyMainTower, playerUnits);
            UpdateTowerAttack(enemySubLeft, playerUnits);
            UpdateTowerAttack(enemySubRight, playerUnits);

            // Unit Movements & Combat
            UpdateUnits(playerUnits, enemyUnits, true);
            UpdateUnits(enemyUnits, playerUnits, false);

            // Floating Texts
            for (int i = floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = floatingTexts[i];
                ft.lifetime -= Time.deltaTime;
                ft.worldPos += Vector3.up * 1.5f * Time.deltaTime;
                if (ft.lifetime <= 0) floatingTexts.RemoveAt(i);
            }

            // Check End Conditions
            if (playerMainTower.hp <= 0)
            {
                EndGame(false);
            }
            else if (enemyMainTower.hp <= 0)
            {
                EndGame(true);
            }
        }

        private void UpdateTowerAttack(TowerData tower, List<BattleUnit> targetUnits)
        {
            if (tower == null || tower.hp <= 0 || tower.go == null) return;

            tower.attackTimer += Time.deltaTime;
            if (tower.attackTimer < tower.attackInterval) return;

            // Find closest target in range
            BattleUnit closest = null;
            float minDist = tower.attackRange;

            foreach (var u in targetUnits)
            {
                if (u.hp <= 0 || u.go == null) continue;
                float d = Vector3.Distance(tower.position, u.go.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = u;
                }
            }

            if (closest != null)
            {
                tower.attackTimer = 0f;
                ApplyUnitDamage(closest, tower.attackDamage);
                PlaySfx(clipHit);
            }
        }

        private void UpdateUnits(List<BattleUnit> units, List<BattleUnit> enemies, bool isPlayer)
        {
            float targetZ = isPlayer ? EnemyBaseZ : PlayerBaseZ;
            float[] laneX = { LaneLeftX, LaneCenterX, LaneRightX };

            for (int i = units.Count - 1; i >= 0; i--)
            {
                var u = units[i];
                if (u.go == null || u.hp <= 0)
                {
                    if (u.go != null) Destroy(u.go);
                    units.RemoveAt(i);
                    continue;
                }

                u.attackTimer += Time.deltaTime;

                // Flash timer
                if (u.flashTimer > 0)
                {
                    u.flashTimer -= Time.deltaTime;
                    if (u.flashTimer <= 0 && u.unitMat != null)
                    {
                        u.unitMat.color = u.baseColor;
                    }
                }

                // Check for target (Enemy unit or Tower)
                object target = FindBestTarget(u, enemies, isPlayer);

                if (target != null)
                {
                    // In attack range?
                    Vector3 targetPos = (target is BattleUnit bu) ? bu.go.transform.position : ((TowerData)target).position;
                    float dist = Vector3.Distance(u.go.transform.position, targetPos);

                    if (dist <= u.attackRange)
                    {
                        // Attack!
                        if (u.attackTimer >= u.attackInterval)
                        {
                            u.attackTimer = 0f;
                            if (target is BattleUnit targetUnit)
                            {
                                ApplyUnitDamage(targetUnit, u.attackDamage);
                            }
                            else if (target is TowerData targetTower)
                            {
                                ApplyTowerDamage(targetTower, u.attackDamage);
                            }
                            PlaySfx(clipHit);
                        }
                        continue; // Hold position while attacking
                    }
                }

                // Move forward along lane
                Vector3 pos = u.go.transform.position;
                float targetLaneX = laneX[u.laneIndex];
                float dirZ = isPlayer ? 1f : -1f;

                pos.x = Mathf.MoveTowards(pos.x, targetLaneX, 4.0f * Time.deltaTime);
                pos.z += dirZ * u.moveSpeed * Time.deltaTime;
                pos.y = 0.5f;

                u.go.transform.position = pos;
            }
        }

        private object FindBestTarget(BattleUnit u, List<BattleUnit> enemies, bool isPlayer)
        {
            // 1. Check enemy units in same/nearby lane
            BattleUnit closestUnit = null;
            float minUnitDist = u.attackRange * 1.5f;

            foreach (var e in enemies)
            {
                if (e.go == null || e.hp <= 0) continue;
                // Only engage if in front
                bool isInFront = isPlayer ? (e.go.transform.position.z > u.go.transform.position.z) : (e.go.transform.position.z < u.go.transform.position.z);
                if (!isInFront) continue;

                float d = Vector3.Distance(u.go.transform.position, e.go.transform.position);
                if (d < minUnitDist)
                {
                    minUnitDist = d;
                    closestUnit = e;
                }
            }

            if (closestUnit != null) return closestUnit;

            // 2. Check enemy towers
            TowerData[] candidateTowers = isPlayer
                ? new[] { u.laneIndex == 0 ? enemySubLeft : (u.laneIndex == 2 ? enemySubRight : null), enemyMainTower }
                : new[] { u.laneIndex == 0 ? playerSubLeft : (u.laneIndex == 2 ? playerSubRight : null), playerMainTower };

            foreach (var t in candidateTowers)
            {
                if (t == null || t.hp <= 0 || t.go == null) continue;
                float d = Vector3.Distance(u.go.transform.position, t.position);
                if (d <= u.attackRange + 1.2f) return t;
            }

            return null;
        }

        public void ApplyUnitDamage(BattleUnit target, float dmg)
        {
            target.hp -= dmg;
            target.flashTimer = 0.12f;
            if (target.unitMat != null) target.unitMat.color = Color.white;

            SpawnFloatingText("-" + (int)dmg, target.go.transform.position + Vector3.up * 1.2f, Color.yellow);

            if (target.hp <= 0)
            {
                SpawnFloatingText("퇴근!", target.go.transform.position + Vector3.up * 1.8f, Color.green);
            }
        }

        public void ApplyTowerDamage(TowerData tower, float dmg)
        {
            tower.hp = Mathf.Max(0f, tower.hp - dmg);
            SpawnFloatingText("-" + (int)dmg, tower.position + Vector3.up * 2.5f, Color.red);

            if (tower.hp <= 0 && tower.go != null)
            {
                tower.go.GetComponent<Renderer>().material = CreateSolidMaterial(new Color(0.2f, 0.2f, 0.2f));
                SpawnFloatingText("💥 파괴됨!", tower.position + Vector3.up * 3.0f, Color.red);
            }
        }

        private void SpawnFloatingText(string txt, Vector3 pos, Color col)
        {
            floatingTexts.Add(new FloatingText { text = txt, worldPos = pos, color = col, lifetime = 0.9f, maxLifetime = 0.9f });
        }

        private void SpawnEnemyAI()
        {
            int lane = UnityEngine.Random.Range(0, 3);
            int roll = UnityEngine.Random.Range(0, 100);

            if (roll < 55)
            {
                // 야근 요정 (Fast swarm)
                SpawnUnitInternal("야근 요정", lane, false, 190f, 30f, 3.6f, 1.4f, 0.75f, PrimitiveType.Sphere, new Color(0.95f, 0.30f, 0.30f), new Vector3(0.9f, 0.9f, 0.9f));
            }
            else if (roll < 85)
            {
                // 꼰대 이사 (Balanced warrior)
                SpawnUnitInternal("꼰대 이사", lane, false, 450f, 65f, 2.3f, 1.6f, 1.0f, PrimitiveType.Cylinder, new Color(0.80f, 0.15f, 0.25f), new Vector3(1.1f, 1.3f, 1.1f));
            }
            else
            {
                // 주말특근 악마 (Heavy tank boss)
                SpawnUnitInternal("주말특근 악마", lane, false, 1100f, 105f, 1.5f, 1.8f, 1.3f, PrimitiveType.Cube, new Color(0.50f, 0.10f, 0.65f), new Vector3(1.6f, 1.8f, 1.6f));
            }
        }

        public void SpawnPlayerUnit(int cardIndex)
        {
            if (isGameOver) return;

            int[] costs = { 2, 3, 4, 4, 1 };
            if (cardIndex < 0 || cardIndex >= costs.Length) return;
            int cost = costs[cardIndex];

            if (caffeine < cost)
            {
                SpawnFloatingText("카페인 부족!", playerMainTower.position + Vector3.up * 2f, Color.red);
                return;
            }

            if (cardIndex == 3)
            {
                // 칼퇴 레이저 (Spell)
                caffeine -= cost;
                CastLaserSpell(selectedLane);
                return;
            }
            else if (cardIndex == 4)
            {
                // 믹스커피 (Instant Mana Boost)
                if (coffeeCooldown > 0f)
                {
                    SpawnFloatingText("쿨타임 중!", playerMainTower.position + Vector3.up * 2f, Color.yellow);
                    return;
                }
                caffeine = Mathf.Min(MaxCaffeine, caffeine + 3.5f);
                coffeeCooldown = 12.0f;
                PlaySfx(clipSpawn);
                SpawnFloatingText("☕ 카페인 +3.5 충전!", playerMainTower.position + Vector3.up * 3f, Color.cyan);
                return;
            }

            caffeine -= cost;

            if (cardIndex == 0)
            {
                // 인턴 사원
                SpawnUnitInternal("인턴 사원", selectedLane, true, 220f, 38f, 3.8f, 3.8f, 0.8f, PrimitiveType.Capsule, new Color(0.30f, 0.75f, 0.95f), new Vector3(0.9f, 1.1f, 0.9f));
            }
            else if (cardIndex == 1)
            {
                // 칼퇴 대리
                SpawnUnitInternal("칼퇴 대리", selectedLane, true, 480f, 65f, 2.7f, 1.6f, 0.95f, PrimitiveType.Cylinder, new Color(0.15f, 0.40f, 0.85f), new Vector3(1.1f, 1.3f, 1.1f));
            }
            else if (cardIndex == 2)
            {
                // 철벽 과장
                SpawnUnitInternal("철벽 과장", selectedLane, true, 1050f, 48f, 1.7f, 1.5f, 1.25f, PrimitiveType.Cube, new Color(0.10f, 0.20f, 0.60f), new Vector3(1.5f, 1.6f, 1.5f));
            }

            PlaySfx(clipSpawn);
        }

        private void SpawnUnitInternal(string name, int lane, bool isPlayer, float hp, float atk, float spd, float rng, float interval, PrimitiveType prim, Color col, Vector3 scale)
        {
            float[] laneX = { LaneLeftX, LaneCenterX, LaneRightX };
            float spawnZ = isPlayer ? (PlayerBaseZ + 2.5f) : (EnemyBaseZ - 2.5f);

            GameObject go = GameObject.CreatePrimitive(prim);
            go.name = name;
            go.transform.position = new Vector3(laneX[lane], 0.5f, spawnZ);
            go.transform.localScale = scale;

            Material uMat = CreateSolidMaterial(col);
            go.GetComponent<Renderer>().material = uMat;

            // Small accessory badge on head
            GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hat.name = "Badge";
            hat.transform.SetParent(go.transform);
            hat.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            hat.transform.localScale = new Vector3(0.4f, 0.2f, 0.4f);
            hat.GetComponent<Renderer>().material = isPlayer ? matGold : matEnemy;

            BattleUnit unit = new BattleUnit
            {
                unitName = name,
                go = go,
                isPlayer = isPlayer,
                laneIndex = lane,
                hp = hp,
                maxHp = hp,
                attackDamage = atk,
                moveSpeed = spd,
                attackRange = rng,
                attackInterval = interval,
                attackTimer = interval * 0.5f,
                unitMat = uMat,
                baseColor = col
            };

            if (isPlayer) playerUnits.Add(unit);
            else enemyUnits.Add(unit);
        }

        private void CastLaserSpell(int lane)
        {
            float[] laneX = { LaneLeftX, LaneCenterX, LaneRightX };
            float targetX = laneX[lane];

            PlaySfx(clipLaser);

            // Laser Visual
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "LaserBeam";
            beam.transform.position = new Vector3(targetX, 4f, 0f);
            beam.transform.localScale = new Vector3(1.4f, 15f, 1.4f);
            beam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            beam.GetComponent<Renderer>().material = matGold;
            Destroy(beam, 0.35f);

            // Damage all enemies in lane
            foreach (var e in enemyUnits)
            {
                if (e.laneIndex == lane && e.hp > 0 && e.go != null)
                {
                    ApplyUnitDamage(e, 260f);
                }
            }

            // Damage enemy sub tower in lane if alive
            TowerData subTower = lane == 0 ? enemySubLeft : (lane == 2 ? enemySubRight : null);
            if (subTower != null && subTower.hp > 0)
            {
                ApplyTowerDamage(subTower, 200f);
            }
            else if (lane == 1 && enemyMainTower.hp > 0)
            {
                ApplyTowerDamage(enemyMainTower, 180f);
            }

            SpawnFloatingText("⚡ 6시 칼퇴 빔!!", new Vector3(targetX, 2f, 0f), Color.yellow);
        }

        private void EvaluateTimeoutWinner()
        {
            float playerTotalHp = playerMainTower.hp + playerSubLeft.hp + playerSubRight.hp;
            float enemyTotalHp = enemyMainTower.hp + enemySubLeft.hp + enemySubRight.hp;
            EndGame(playerTotalHp >= enemyTotalHp);
        }

        private void EndGame(bool victory)
        {
            isGameOver = true;
            isVictory = victory;
            PlaySfx(victory ? clipWin : clipLose);
        }

        // --- Audio Engine ---
        private void InitAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            clipSpawn = MakeChirpClip(220, 520, 0.12f);
            clipHit = MakeNoiseClip(0.08f);
            clipLaser = MakeSweepClip(900, 180, 0.28f);
            clipWin = MakeFanfareClip();
            clipLose = MakeSadClip();
        }

        private void PlaySfx(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, 0.8f);
            }
        }

        private AudioClip MakeChirpClip(float startFreq, float endFreq, float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(startFreq, endFreq, t / duration);
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * (1f - (float)i / samples) * 0.5f;
            }
            AudioClip clip = AudioClip.Create("Chirp", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeNoiseClip(float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            System.Random rand = new System.Random();
            for (int i = 0; i < samples; i++)
            {
                data[i] = ((float)rand.NextDouble() * 2f - 1f) * (1f - (float)i / samples) * 0.4f;
            }
            AudioClip clip = AudioClip.Create("Noise", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeSweepClip(float startFreq, float endFreq, float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(t / duration, 2f));
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * (1f - (float)i / samples) * 0.5f;
            }
            AudioClip clip = AudioClip.Create("Sweep", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeFanfareClip()
        {
            int sampleRate = 44100;
            float duration = 0.6f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            float[] notes = { 440f, 554f, 659f, 880f };
            int noteSamples = samples / notes.Length;

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                for (int i = 0; i < noteSamples; i++)
                {
                    int idx = n * noteSamples + i;
                    if (idx >= samples) break;
                    float t = (float)i / sampleRate;
                    data[idx] = Mathf.Sin(2 * Mathf.PI * freq * t) * (1f - (float)i / noteSamples) * 0.5f;
                }
            }
            AudioClip clip = AudioClip.Create("Win", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeSadClip()
        {
            int sampleRate = 44100;
            float duration = 0.7f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            float[] notes = { 400f, 370f, 330f, 290f };
            int noteSamples = samples / notes.Length;

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                for (int i = 0; i < noteSamples; i++)
                {
                    int idx = n * noteSamples + i;
                    if (idx >= samples) break;
                    float t = (float)i / sampleRate;
                    data[idx] = Mathf.Sin(2 * Mathf.PI * freq * t) * (1f - (float)i / noteSamples) * 0.5f;
                }
            }
            AudioClip clip = AudioClip.Create("Lose", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // --- Mobile High-DPI OnGUI Interface ---
        private void OnGUI()
        {
            // Virtual 750x1334 iPhone portrait layout scaling
            float targetW = 750f;
            float targetH = 1334f;
            float scale = Mathf.Min(Screen.width / targetW, Screen.height / targetH);
            float offsetX = (Screen.width - targetW * scale) * 0.5f;
            float offsetY = (Screen.height - targetH * scale) * 0.5f;

            Matrix4x4 oldMat = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0), Quaternion.identity, new Vector3(scale, scale, 1));

            // Custom UI Styles
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            GUIStyle timeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.2f) }
            };

            GUIStyle hpStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUIStyle cardBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            GUIStyle laneBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            // 1. Top Header (Status)
            GUI.Box(new Rect(20, 20, 710, 110), "");

            // Title
            GUI.Label(new Rect(30, 25, 690, 35), "⚔️ 칼퇴 럼블 (Office Rumble)", titleStyle);

            // Timer
            int min = (int)(matchTimer / 60);
            int sec = (int)(matchTimer % 60);
            string timeStr = string.Format("⏱️ {0:00}:{1:00}", min, sec);
            if (matchTimer <= 60f) timeStr += " 🔥피버!";
            GUI.Box(new Rect(290, 65, 170, 50), timeStr, timeStyle);

            // Enemy HP (Left)
            float enemyHp = enemyMainTower.hp;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            GUI.Box(new Rect(35, 65, 240, 50), string.Format("👹 야근본사 {0:0}/{1:0}", enemyHp, enemyMainTower.maxHp), hpStyle);

            // Player HP (Right)
            float playerHp = playerMainTower.hp;
            GUI.backgroundColor = new Color(0.3f, 0.7f, 1.0f);
            GUI.Box(new Rect(475, 65, 240, 50), string.Format("🏢 엘베 {0:0}/{1:0}", playerHp, playerMainTower.maxHp), hpStyle);

            GUI.backgroundColor = Color.white;

            // 2. Middle Floating World Texts
            Camera cam = Camera.main;
            if (cam != null)
            {
                foreach (var ft in floatingTexts)
                {
                    Vector3 screenP = cam.WorldToScreenPoint(ft.worldPos);
                    if (screenP.z > 0)
                    {
                        float guiX = (screenP.x - offsetX) / scale;
                        float guiY = (Screen.height - screenP.y - offsetY) / scale;

                        GUIStyle ftStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 22,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = ft.color }
                        };
                        GUI.Label(new Rect(guiX - 100, guiY - 20, 200, 40), ft.text, ftStyle);
                    }
                }
            }

            // 3. Lane Selector (Above Bottom Bar)
            GUI.Box(new Rect(20, 1030, 710, 70), "");
            GUI.Label(new Rect(35, 1045, 160, 40), "배치 복도 선택:", titleStyle);

            string[] laneNames = { "◀ 1번 복도(좌)", "● 2번 복도(중앙)", "▶ 3번 복도(우)" };
            for (int i = 0; i < 3; i++)
            {
                if (selectedLane == i) GUI.backgroundColor = new Color(1.0f, 0.85f, 0.2f);
                else GUI.backgroundColor = new Color(0.3f, 0.4f, 0.5f);

                if (GUI.Button(new Rect(210 + i * 165, 1040, 155, 50), laneNames[i], laneBtnStyle))
                {
                    selectedLane = i;
                }
            }
            GUI.backgroundColor = Color.white;

            // 4. Caffeine (Mana) Bar
            GUI.Box(new Rect(20, 1115, 710, 55), "");
            float barW = 690f * (caffeine / MaxCaffeine);
            GUI.backgroundColor = new Color(0.95f, 0.55f, 0.1f);
            GUI.Box(new Rect(30, 1125, barW, 35), "");
            GUI.backgroundColor = Color.white;

            GUIStyle manaTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(30, 1125, 690, 35), string.Format("☕ 카페인: {0:0.0} / {1:0.0}", caffeine, MaxCaffeine), manaTextStyle);

            // 5. Card Buttons (Bottom Row)
            GUI.Box(new Rect(20, 1180, 710, 135), "");

            string[] cardLabels =
            {
                "🏃 인턴 사원\n(2☕)",
                "💼 칼퇴 대리\n(3☕)",
                "🛡️ 철벽 과장\n(4☕)",
                "⚡ 칼퇴 빔\n(4☕)",
                "☕ 믹스커피\n(1☕)"
            };

            int[] cardCosts = { 2, 3, 4, 4, 1 };

            for (int i = 0; i < 5; i++)
            {
                bool canAfford = caffeine >= cardCosts[i];
                if (i == 4 && coffeeCooldown > 0f) canAfford = false;

                GUI.backgroundColor = canAfford ? new Color(0.2f, 0.65f, 0.95f) : new Color(0.35f, 0.35f, 0.35f);

                string btnText = cardLabels[i];
                if (i == 4 && coffeeCooldown > 0f) btnText += string.Format("\n({0:0}s)", coffeeCooldown);

                if (GUI.Button(new Rect(30 + i * 138, 1192, 130, 112), btnText, cardBtnStyle))
                {
                    SpawnPlayerUnit(i);
                }
            }
            GUI.backgroundColor = Color.white;

            // 6. Game Over Modal
            if (isGameOver)
            {
                GUI.backgroundColor = new Color(0.05f, 0.08f, 0.12f, 0.95f);
                GUI.Box(new Rect(60, 380, 630, 480), "");
                GUI.backgroundColor = Color.white;

                GUIStyle modalTitle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 42,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = isVictory ? new Color(1.0f, 0.85f, 0.2f) : new Color(0.95f, 0.3f, 0.3f) }
                };

                GUIStyle modalBody = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };

                GUIStyle retryBtn = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 26,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                string title = isVictory ? "🎉 칼퇴 성공!!" : "😭 야근 확정...";
                string desc = isVictory
                    ? "대표이사 야근 결재실을 완벽히 격파했습니다!\n당당하게 6시 정시 퇴근에 성공했습니다!"
                    : "퇴근 엘리베이터가 봉쇄되었습니다...\n내일은 꼭 야근 빌런들을 물리치고 칼퇴합시다!";

                GUI.Label(new Rect(80, 420, 590, 70), title, modalTitle);
                GUI.Label(new Rect(90, 510, 570, 140), desc, modalBody);

                GUI.backgroundColor = isVictory ? new Color(1.0f, 0.75f, 0.15f) : new Color(0.3f, 0.6f, 0.9f);
                if (GUI.Button(new Rect(180, 680, 390, 90), isVictory ? "다시 정시 퇴근하기" : "다시 칼퇴 도전하기", retryBtn))
                {
                    ResetGame();
                }
                GUI.backgroundColor = Color.white;
            }

            GUI.matrix = oldMat;
        }
    }
}
