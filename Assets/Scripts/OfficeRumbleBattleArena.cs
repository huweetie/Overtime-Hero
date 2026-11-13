using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OfficeRumble
{
    public enum GameScreen
    {
        Title,
        Battle,
        Codex,
        Settings,
        Victory,
        Defeat
    }

    public enum UnitType
    {
        Intern,             // 2 Caffeine: Fast runner
        AssistantManager,   // 3 Caffeine: Balanced melee
        SeniorManager,      // 4 Caffeine: Heavy shield tank
        TeamLeader,         // 5 Caffeine: Buffer bruiser with giant tumbler
        OvertimeLaser,      // 4 Caffeine: Full lane pierce laser spell
        MixCoffee,          // 3 Caffeine: Global ally heal & haste spell
        EnemyGoblin,        // Swarm minion
        EnemyDirector,      // Boomer director with reject stamp
        EnemyExecutive,     // Ranged document barrage
        EnemyBoss           // Weekend overtime demon
    }

    public class BattleUnit
    {
        public GameObject go;
        public UnitType type;
        public bool isPlayer;
        public int lane;
        public float speed;
        public int damage;
        public int maxHp;
        public int hp;
        public float attackRange;
        public float attackCooldown;
        public float currentCooldown;
        public float animTimer;
        public Transform modelRoot;
        public Transform weaponTransform;
    }

    public class TowerData
    {
        public string id;
        public string name;
        public int hp;
        public int maxHp;
        public Vector3 pos;
        public bool isPlayer;
        public bool isMain;
        public bool isMeetingHub;
        public GameObject go;
        public Renderer indicatorRenderer;
    }

    public class PantryChestData
    {
        public int hp;
        public int maxHp;
        public Vector3 pos;
        public bool isDestroyed;
        public float respawnTimer;
        public GameObject go;
    }

    public class FloatingText
    {
        public Vector3 worldPos;
        public string text;
        public Color color;
        public float life;
        public float maxLife;
    }

    public enum SfxType
    {
        Summon,
        Hit,
        Laser,
        Coffee,
        TowerExplode,
        ChestCollect,
        Victory,
        Defeat
    }

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

        // --- Grand Arena Dimensions (2.7x Expanded Battlefield) ---
        public const float LaneLeftX = -4.8f;
        public const float LaneCenterX = 0.0f;
        public const float LaneRightX = 4.8f;

        public const float PlayerBaseZ = -22.0f;
        public const float EnemyBaseZ = 22.0f;
        public const float MeetingHubLeftZ = -2.0f;
        public const float MeetingHubRightZ = 2.0f;
        public const float PantryChestZ = 0.0f;

        // --- Game Balance & Runtime State ---
        public GameScreen currentScreen = GameScreen.Title;
        public int selectedLane = 1; // 0: Left, 1: Center, 2: Right
        public bool useForwardSpawn = false;
        public float caffeine = 4.5f;
        public const float MaxCaffeine = 10.0f;
        public float matchTimer = 180.0f;
        public bool isGameOver = false;
        public bool isVictory = false;
        public int gameSpeed = 1; // 1x or 2x

        private float enemySpawnTimer = 3.0f;
        private float laserCooldown = 0f;
        private float coffeeCooldown = 0f;

        // --- Towers & Entities ---
        public TowerData playerMainTower;
        public TowerData playerSubLeft;
        public TowerData playerSubRight;

        public TowerData enemyMainTower;
        public TowerData enemySubLeft;
        public TowerData enemySubRight;

        public TowerData meetingHubWest; // Capture point
        public TowerData meetingHubEast; // Capture point

        public PantryChestData pantryChest;

        public List<BattleUnit> playerUnits = new List<BattleUnit>();
        public List<BattleUnit> enemyUnits = new List<BattleUnit>();
        public List<FloatingText> floatingTexts = new List<FloatingText>();

        // --- Camera & Scrolling ---
        private Camera mainCam;
        private float camTargetZ = -12.0f;
        private float camCurrentZ = -12.0f;
        private bool isDraggingCam = false;
        private Vector2 dragStartPos;
        private float dragStartCamZ;
        private float shakeIntensity = 0f;
        private float titleCamAngle = 0f;

        // --- Materials & Textures ---
        private Material matCarpetLobby;
        private Material matCarpetConference;
        private Material matCarpetExecutive;
        private Material matAisle;
        private Material matWall;
        private Material matGlass;
        private Material matDeskWood;
        private Material matDeskMetal;
        private Material matScreenBlue;
        private Material matChair;
        private Material matPlantPot;
        private Material matPlantLeaf;
        private Material matElevatorSteel;
        private Material matElevatorLight;
        private Material matCeoMahogany;
        private Material matGoldTrim;
        private Material matPaperStack;

        private Material matHubNeutral;
        private Material matHubPlayer;
        private Material matHubEnemy;
        private Material matChestGold;

        // Unit Materials
        private Material matSkin;
        private Material matInternHoodie;
        private Material matDaeriShirt;
        private Material matDaeriSlacks;
        private Material matGwajangVest;
        private Material matGwajangShield;
        private Material matLeaderSuit;
        private Material matLeaderTumbler;
        private Material matEnemyGoblin;
        private Material matEnemyDirector;
        private Material matEnemyExecutive;
        private Material matEnemyBoss;

        // Procedural UI Textures
        private Texture2D texWhite;
        private Texture2D texPanelDark;
        private Texture2D texPanelHeader;
        private Texture2D texBtnNormal;
        private Texture2D texBtnHover;
        private Texture2D texBtnActive;
        private Texture2D texBtnSpecial;
        private Texture2D texBarBg;
        private Texture2D texHpGreen;
        private Texture2D texHpRed;
        private Texture2D texHpYellow;
        private Texture2D texCaffeineFill;
        private Texture2D texCardBg;
        private Texture2D texCardActive;

        private AudioSource audioSource;
        private GameObject arenaRoot;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Time.timeScale = 1.0f;
        }

        private void Start()
        {
            mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                mainCam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.08f, 0.10f, 0.14f);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            SetupDirectionalLight();
            CreateMaterials();
            CreateProceduralTextures();
            BuildDetailedGrandArena();
            ResetGame(false);

            SetScreen(GameScreen.Title);
        }

        private void SetupDirectionalLight()
        {
            Light sun = FindObjectOfType<Light>();
            if (sun == null)
            {
                GameObject lightGo = new GameObject("OfficeDirectionalLight");
                sun = lightGo.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(54f, -28f, 0f);
            sun.color = new Color(1.0f, 0.97f, 0.92f);
            sun.intensity = 1.4f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.44f, 0.47f, 0.55f);
        }

        private void CreateMaterials()
        {
            matCarpetLobby = CreateSolidMaterial(new Color(0.16f, 0.22f, 0.32f));
            matCarpetConference = CreateSolidMaterial(new Color(0.22f, 0.25f, 0.30f));
            matCarpetExecutive = CreateSolidMaterial(new Color(0.28f, 0.14f, 0.16f));
            matAisle = CreateSolidMaterial(new Color(0.12f, 0.15f, 0.20f));
            matWall = CreateSolidMaterial(new Color(0.10f, 0.12f, 0.16f));
            matGlass = CreateSolidMaterial(new Color(0.35f, 0.65f, 0.85f, 0.55f), new Color(0.10f, 0.35f, 0.55f));
            matDeskWood = CreateSolidMaterial(new Color(0.85f, 0.72f, 0.58f));
            matDeskMetal = CreateSolidMaterial(new Color(0.32f, 0.36f, 0.42f));
            matScreenBlue = CreateSolidMaterial(new Color(0.12f, 0.25f, 0.45f), new Color(0.25f, 0.70f, 1.0f));
            matChair = CreateSolidMaterial(new Color(0.18f, 0.18f, 0.22f));
            matPlantPot = CreateSolidMaterial(new Color(0.78f, 0.42f, 0.28f));
            matPlantLeaf = CreateSolidMaterial(new Color(0.15f, 0.62f, 0.25f));
            matElevatorSteel = CreateSolidMaterial(new Color(0.72f, 0.76f, 0.82f));
            matElevatorLight = CreateSolidMaterial(new Color(0.20f, 0.95f, 0.45f), new Color(0.35f, 1.0f, 0.55f));
            matCeoMahogany = CreateSolidMaterial(new Color(0.36f, 0.14f, 0.10f));
            matGoldTrim = CreateSolidMaterial(new Color(0.98f, 0.82f, 0.25f), new Color(0.50f, 0.38f, 0.08f));
            matPaperStack = CreateSolidMaterial(new Color(0.94f, 0.94f, 0.96f));

            matHubNeutral = CreateSolidMaterial(new Color(0.55f, 0.55f, 0.60f), new Color(0.20f, 0.20f, 0.25f));
            matHubPlayer = CreateSolidMaterial(new Color(0.15f, 0.65f, 0.95f), new Color(0.25f, 0.80f, 1.0f));
            matHubEnemy = CreateSolidMaterial(new Color(0.90f, 0.25f, 0.25f), new Color(1.0f, 0.35f, 0.35f));
            matChestGold = CreateSolidMaterial(new Color(1.0f, 0.85f, 0.20f), new Color(0.85f, 0.65f, 0.10f));

            // Units
            matSkin = CreateSolidMaterial(new Color(0.98f, 0.84f, 0.72f));
            matInternHoodie = CreateSolidMaterial(new Color(0.20f, 0.68f, 0.95f));
            matDaeriShirt = CreateSolidMaterial(new Color(0.96f, 0.96f, 0.98f));
            matDaeriSlacks = CreateSolidMaterial(new Color(0.14f, 0.18f, 0.28f));
            matGwajangVest = CreateSolidMaterial(new Color(0.42f, 0.46f, 0.52f));
            matGwajangShield = CreateSolidMaterial(new Color(0.92f, 0.94f, 0.98f), new Color(0.20f, 0.35f, 0.50f));
            matLeaderSuit = CreateSolidMaterial(new Color(0.18f, 0.15f, 0.25f));
            matLeaderTumbler = CreateSolidMaterial(new Color(0.85f, 0.85f, 0.88f), new Color(0.30f, 0.60f, 0.90f));

            matEnemyGoblin = CreateSolidMaterial(new Color(0.60f, 0.22f, 0.80f), new Color(0.30f, 0.08f, 0.45f));
            matEnemyDirector = CreateSolidMaterial(new Color(0.30f, 0.12f, 0.15f));
            matEnemyExecutive = CreateSolidMaterial(new Color(0.18f, 0.10f, 0.25f), new Color(0.40f, 0.15f, 0.50f));
            matEnemyBoss = CreateSolidMaterial(new Color(0.22f, 0.05f, 0.05f), new Color(0.90f, 0.18f, 0.18f));
        }

        private Material CreateSolidMaterial(Color col, Color? emission = null)
        {
            Material baseMat = Resources.Load<Material>("Materials/OfficeLit");
            Material mat;
            if (baseMat != null) mat = new Material(baseMat);
            else
            {
                Shader sh = Shader.Find("OfficeRumble/Lit");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                mat = new Material(sh);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            mat.color = col;

            if (emission.HasValue && mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emission.Value);
            }
            return mat;
        }

        private void CreateProceduralTextures()
        {
            texWhite = MakeSolidTexture(2, 2, Color.white);
            texPanelDark = MakeRoundedBoxTexture(128, 128, new Color(0.06f, 0.08f, 0.12f, 0.94f), new Color(0.22f, 0.28f, 0.38f, 0.85f), 2);
            texPanelHeader = MakeRoundedBoxTexture(128, 128, new Color(0.10f, 0.14f, 0.22f, 0.96f), new Color(0.32f, 0.45f, 0.65f, 0.85f), 2);
            texBtnNormal = MakeRoundedBoxTexture(128, 64, new Color(0.14f, 0.18f, 0.26f, 0.95f), new Color(0.28f, 0.36f, 0.48f, 0.90f), 2);
            texBtnHover = MakeRoundedBoxTexture(128, 64, new Color(0.20f, 0.28f, 0.42f, 0.98f), new Color(0.42f, 0.62f, 0.92f, 1.0f), 2);
            texBtnActive = MakeRoundedBoxTexture(128, 64, new Color(0.15f, 0.52f, 0.88f, 1.0f), new Color(0.65f, 0.88f, 1.0f, 1.0f), 3);
            texBtnSpecial = MakeRoundedBoxTexture(128, 64, new Color(0.88f, 0.46f, 0.10f, 1.0f), new Color(1.0f, 0.82f, 0.32f, 1.0f), 2);
            texBarBg = MakeSolidTexture(4, 4, new Color(0.05f, 0.06f, 0.08f, 0.96f));
            texHpGreen = MakeSolidTexture(4, 4, new Color(0.16f, 0.84f, 0.36f, 1.0f));
            texHpRed = MakeSolidTexture(4, 4, new Color(0.92f, 0.24f, 0.24f, 1.0f));
            texHpYellow = MakeSolidTexture(4, 4, new Color(0.95f, 0.82f, 0.18f, 1.0f));
            texCaffeineFill = MakeSolidTexture(4, 4, new Color(0.22f, 0.78f, 1.0f, 1.0f));
            texCardBg = MakeRoundedBoxTexture(128, 160, new Color(0.12f, 0.16f, 0.22f, 0.95f), new Color(0.28f, 0.34f, 0.44f, 0.80f), 2);
            texCardActive = MakeRoundedBoxTexture(128, 160, new Color(0.16f, 0.28f, 0.45f, 0.98f), new Color(0.35f, 0.80f, 1.0f, 1.0f), 3);
        }

        private Texture2D MakeSolidTexture(int w, int h, Color col)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] cols = new Color[w * h];
            for (int i = 0; i < cols.Length; i++) cols[i] = col;
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeRoundedBoxTexture(int w, int h, Color fill, Color border, int borderWidth)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] cols = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x < borderWidth || x >= w - borderWidth || y < borderWidth || y >= h - borderWidth);
                    cols[y * w + x] = isBorder ? border : fill;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        // --- Grand Arena 3D Builder (56m x 16m Multi-Tiered Battlefield) ---
        private void BuildDetailedGrandArena()
        {
            if (arenaRoot != null) Destroy(arenaRoot);
            arenaRoot = new GameObject("GrandOfficeArena_Root");

            // 1. 3-Tier Multi-Zoned Floors (Length: 56m, Width: 16m)
            // Tier 1: 1F Open Office & Lobby (Z: -28 to -10)
            CreateCube("Floor_1F_Lobby", new Vector3(0f, -0.25f, -19.0f), new Vector3(16.0f, 0.5f, 18.0f), matCarpetLobby, arenaRoot);
            // Tier 2: 2F Conference & Pantry Hub (Z: -10 to +10)
            CreateCube("Floor_2F_Hub", new Vector3(0f, -0.25f, 0.0f), new Vector3(16.0f, 0.5f, 20.0f), matCarpetConference, arenaRoot);
            // Tier 3: 3F Executive Penthouse (Z: +10 to +28)
            CreateCube("Floor_3F_Penthouse", new Vector3(0f, -0.25f, 19.0f), new Vector3(16.0f, 0.5f, 18.0f), matCarpetExecutive, arenaRoot);

            // 2. 3 Main Lane Strips (Left, Center, Right)
            float[] laneX = { LaneLeftX, LaneCenterX, LaneRightX };
            for (int i = 0; i < 3; i++)
            {
                CreateCube("LaneStrip_" + i, new Vector3(laneX[i], 0.02f, 0f), new Vector3(2.6f, 0.04f, 54.0f), matAisle, arenaRoot);
            }

            // 3. Perimeter High Walls with Architectural Frame
            CreateCube("Wall_Left", new Vector3(-8.0f, 1.8f, 0f), new Vector3(0.5f, 3.6f, 56.0f), matWall, arenaRoot);
            CreateCube("Wall_Right", new Vector3(8.0f, 1.8f, 0f), new Vector3(0.5f, 3.6f, 56.0f), matWall, arenaRoot);
            CreateCube("Wall_PlayerEnd", new Vector3(0f, 1.8f, -28.0f), new Vector3(16.0f, 3.6f, 0.5f), matWall, arenaRoot);
            CreateCube("Wall_EnemyEnd", new Vector3(0f, 1.8f, 28.0f), new Vector3(16.0f, 3.6f, 0.5f), matWall, arenaRoot);

            // 4. Cubicle Workstation Clusters (Desks, Monitors, Chairs, Partitions)
            float[] cubicleZ = { -15.0f, -10.5f, 10.5f, 15.0f };
            foreach (float z in cubicleZ)
            {
                BuildCubiclePod(new Vector3(-6.2f, 0f, z), 90f, arenaRoot);
                BuildCubiclePod(new Vector3(6.2f, 0f, z), -90f, arenaRoot);
            }

            // 5. Middle Zone Strategy Meeting Hubs (Capture Points!)
            meetingHubWest = BuildMeetingHub("Hub_WestStrategy", "서쪽 전략회의실", new Vector3(LaneLeftX, 0f, MeetingHubLeftZ), arenaRoot);
            meetingHubEast = BuildMeetingHub("Hub_EastMarketing", "동쪽 마케팅실", new Vector3(LaneRightX, 0f, MeetingHubRightZ), arenaRoot);

            // 6. Central Pantry Coffee Chest (Resource Gimmick at Center 0, 0)
            BuildPantryResourceChest(new Vector3(0f, 0f, PantryChestZ), arenaRoot);

            // 7. Green Office Plants & Dividers
            float[] plantZ = { -20.0f, -6.0f, 6.0f, 20.0f };
            foreach (float z in plantZ)
            {
                BuildPottedPlant(new Vector3(-6.5f, 0f, z), arenaRoot);
                BuildPottedPlant(new Vector3(6.5f, 0f, z), arenaRoot);
            }

            // 8. Player Base: Elevator Bank & Security Gates (Z = -22f)
            BuildPlayerElevatorHQ(new Vector3(0f, 0f, PlayerBaseZ), arenaRoot);

            // 9. Enemy Base: Executive Penthouse CEO Suite (Z = +22f)
            BuildEnemyCeoHQ(new Vector3(0f, 0f, EnemyBaseZ), arenaRoot);
        }

        private GameObject CreateCube(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent = null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = pos;
            cube.transform.localScale = scale;
            if (parent != null) cube.transform.SetParent(parent.transform);
            Renderer r = cube.GetComponent<Renderer>();
            r.material = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return cube;
        }

        private void BuildCubiclePod(Vector3 pos, float rotY, GameObject parent)
        {
            GameObject pod = new GameObject("CubiclePod");
            pod.transform.position = pos;
            pod.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            pod.transform.SetParent(parent.transform);

            // L-Shaped Desk
            CreateCube("DeskMain", pos + new Vector3(0f, 0.72f, 0f), new Vector3(1.6f, 0.08f, 0.8f), matDeskWood, pod);
            CreateCube("DeskReturn", pos + new Vector3(0.5f, 0.72f, 0.6f), new Vector3(0.6f, 0.08f, 0.8f), matDeskWood, pod);
            CreateCube("Leg1", pos + new Vector3(-0.7f, 0.36f, 0f), new Vector3(0.08f, 0.72f, 0.7f), matDeskMetal, pod);
            CreateCube("Leg2", pos + new Vector3(0.7f, 0.36f, 0f), new Vector3(0.08f, 0.72f, 0.7f), matDeskMetal, pod);

            // Curved Dual Monitors with Glowing Screens
            CreateCube("MonitorL", pos + new Vector3(-0.35f, 1.05f, 0.25f), new Vector3(0.48f, 0.32f, 0.05f), matScreenBlue, pod);
            CreateCube("MonitorR", pos + new Vector3(0.25f, 1.05f, 0.35f), new Vector3(0.48f, 0.32f, 0.05f), matScreenBlue, pod);

            // Ergonomic Office Chair
            CreateCube("ChairSeat", pos + new Vector3(0f, 0.45f, -0.45f), new Vector3(0.55f, 0.08f, 0.55f), matChair, pod);
            CreateCube("ChairBack", pos + new Vector3(0f, 0.85f, -0.70f), new Vector3(0.50f, 0.70f, 0.08f), matChair, pod);

            // Glass/Fabric Partition Panel
            CreateCube("PartitionFabric", pos + new Vector3(0f, 0.55f, 0.45f), new Vector3(1.7f, 1.1f, 0.08f), matWall, pod);
            CreateCube("PartitionGlass", pos + new Vector3(0f, 1.30f, 0.45f), new Vector3(1.7f, 0.4f, 0.05f), matGlass, pod);
        }

        private TowerData BuildMeetingHub(string id, string name, Vector3 pos, GameObject parent)
        {
            GameObject hub = new GameObject(id);
            hub.transform.position = pos;
            hub.transform.SetParent(parent.transform);

            // Glass Enclosure Pillars & Beams
            CreateCube("BeamTop", pos + new Vector3(0f, 2.6f, 0f), new Vector3(3.2f, 0.2f, 2.8f), matDeskMetal, hub);
            CreateCube("Pillar1", pos + new Vector3(-1.5f, 1.3f, -1.3f), new Vector3(0.18f, 2.6f, 0.18f), matDeskMetal, hub);
            CreateCube("Pillar2", pos + new Vector3(1.5f, 1.3f, -1.3f), new Vector3(0.18f, 2.6f, 0.18f), matDeskMetal, hub);
            CreateCube("Pillar3", pos + new Vector3(-1.5f, 1.3f, 1.3f), new Vector3(0.18f, 2.6f, 0.18f), matDeskMetal, hub);
            CreateCube("Pillar4", pos + new Vector3(1.5f, 1.3f, 1.3f), new Vector3(0.18f, 2.6f, 0.18f), matDeskMetal, hub);

            // Round Conference Table
            CreateCube("ConfTable", pos + new Vector3(0f, 0.72f, 0f), new Vector3(1.8f, 0.08f, 1.4f), matDeskWood, hub);
            CreateCube("ConfTableLeg", pos + new Vector3(0f, 0.36f, 0f), new Vector3(0.3f, 0.72f, 0.3f), matDeskMetal, hub);

            // Whiteboard with Charts
            CreateCube("Whiteboard", pos + new Vector3(0f, 1.4f, 1.25f), new Vector3(2.0f, 1.0f, 0.08f), matPaperStack, hub);

            // Central Glowing Capture Beacon Indicator
            GameObject beacon = CreateCube("CaptureBeacon", pos + new Vector3(0f, 2.9f, 0f), new Vector3(0.8f, 0.45f, 0.8f), matHubNeutral, hub);
            Renderer r = beacon.GetComponent<Renderer>();

            return new TowerData
            {
                id = id,
                name = name,
                hp = 650,
                maxHp = 650,
                pos = pos,
                isPlayer = false,
                isMain = false,
                isMeetingHub = true,
                go = hub,
                indicatorRenderer = r
            };
        }

        private void BuildPantryResourceChest(Vector3 pos, GameObject parent)
        {
            GameObject chestGo = new GameObject("PantryCoffeeChest");
            chestGo.transform.position = pos;
            chestGo.transform.SetParent(parent.transform);

            // Nespresso / Mix Coffee Bar Unit
            CreateCube("CounterBase", pos + new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 1.0f, 1.0f), matDeskWood, chestGo);
            CreateCube("CoffeeMachine", pos + new Vector3(-0.4f, 1.3f, 0f), new Vector3(0.5f, 0.6f, 0.5f), matDeskMetal, chestGo);
            CreateCube("MixBoxes", pos + new Vector3(0.4f, 1.15f, 0f), new Vector3(0.5f, 0.3f, 0.4f), matChestGold, chestGo);

            // Glowing Floating Golden Coffee Mug
            CreateCube("GoldMug", pos + new Vector3(0f, 1.8f, 0f), new Vector3(0.35f, 0.45f, 0.35f), matChestGold, chestGo);

            pantryChest = new PantryChestData
            {
                hp = 350,
                maxHp = 350,
                pos = pos,
                isDestroyed = false,
                respawnTimer = 0f,
                go = chestGo
            };
        }

        private void BuildPottedPlant(Vector3 pos, GameObject parent)
        {
            GameObject plant = new GameObject("OfficePlant");
            plant.transform.position = pos;
            plant.transform.SetParent(parent.transform);
            CreateCube("Pot", pos + new Vector3(0f, 0.35f, 0f), new Vector3(0.6f, 0.7f, 0.6f), matPlantPot, plant);
            CreateCube("Leaves1", pos + new Vector3(0f, 1.05f, 0f), new Vector3(0.85f, 0.85f, 0.85f), matPlantLeaf, plant);
            CreateCube("Leaves2", pos + new Vector3(0f, 1.55f, 0f), new Vector3(0.55f, 0.55f, 0.55f), matPlantLeaf, plant);
        }

        private void BuildPlayerElevatorHQ(Vector3 pos, GameObject parent)
        {
            GameObject elv = new GameObject("PlayerHQ_Elevator");
            elv.transform.position = pos;
            elv.transform.SetParent(parent.transform);

            // Grand Triple Elevator Bank Arch
            CreateCube("ElevatorArch", pos + new Vector3(0f, 2.2f, -1.4f), new Vector3(5.6f, 4.4f, 0.8f), matElevatorSteel, elv);
            CreateCube("Door1", pos + new Vector3(-1.8f, 1.8f, -1.0f), new Vector3(1.3f, 3.4f, 0.1f), matCarpetLobby, elv);
            CreateCube("Door2", pos + new Vector3(0f, 1.8f, -1.0f), new Vector3(1.3f, 3.4f, 0.1f), matCarpetLobby, elv);
            CreateCube("Door3", pos + new Vector3(1.8f, 1.8f, -1.0f), new Vector3(1.3f, 3.4f, 0.1f), matCarpetLobby, elv);

            // High-Visibility Green EXIT / 6PM SHARP Sign
            CreateCube("ExitSignBoard", pos + new Vector3(0f, 3.9f, -0.9f), new Vector3(3.2f, 0.6f, 0.2f), matElevatorLight, elv);

            // Left / Right Speed Gates (Sub-Towers at Z = -17f)
            GameObject gateL = CreateCube("GateL", new Vector3(LaneLeftX, 0.6f, -17.0f), new Vector3(0.9f, 1.2f, 0.9f), matElevatorSteel, elv);
            GameObject gateR = CreateCube("GateR", new Vector3(LaneRightX, 0.6f, -17.0f), new Vector3(0.9f, 1.2f, 0.9f), matElevatorSteel, elv);

            playerMainTower = new TowerData { id = "P_Main", name = "초고속 칼퇴 엘리베이터", hp = 2000, maxHp = 2000, pos = pos, isPlayer = true, isMain = true, go = elv };
            playerSubLeft = new TowerData { id = "P_SubL", name = "출입게이트(좌)", hp = 800, maxHp = 800, pos = new Vector3(LaneLeftX, 0f, -17f), isPlayer = true, isMain = false, go = gateL };
            playerSubRight = new TowerData { id = "P_SubR", name = "출입게이트(우)", hp = 800, maxHp = 800, pos = new Vector3(LaneRightX, 0f, -17f), isPlayer = true, isMain = false, go = gateR };
        }

        private void BuildEnemyCeoHQ(Vector3 pos, GameObject parent)
        {
            GameObject ceo = new GameObject("EnemyHQ_CeoSuite");
            ceo.transform.position = pos;
            ceo.transform.SetParent(parent.transform);

            // Grand Executive Mahogany Desk & Gold Trim
            CreateCube("CeoDesk", pos + new Vector3(0f, 0.85f, 1.2f), new Vector3(4.8f, 0.16f, 1.6f), matCeoMahogany, ceo);
            CreateCube("CeoDeskGold", pos + new Vector3(0f, 0.90f, 1.2f), new Vector3(4.9f, 0.04f, 1.7f), matGoldTrim, ceo);
            CreateCube("LegL", pos + new Vector3(-2.1f, 0.42f, 1.2f), new Vector3(0.3f, 0.84f, 1.4f), matCeoMahogany, ceo);
            CreateCube("LegR", pos + new Vector3(2.1f, 0.42f, 1.2f), new Vector3(0.3f, 0.84f, 1.4f), matCeoMahogany, ceo);

            // Stacks of Overtime Approval Sheets
            CreateCube("PaperTowers1", pos + new Vector3(-1.4f, 1.25f, 1.2f), new Vector3(0.6f, 0.7f, 0.5f), matPaperStack, ceo);
            CreateCube("PaperTowers2", pos + new Vector3(1.4f, 1.40f, 1.2f), new Vector3(0.6f, 0.9f, 0.5f), matPaperStack, ceo);

            // Executive Leather Throne Chair
            CreateCube("ThroneSeat", pos + new Vector3(0f, 0.65f, 2.3f), new Vector3(1.2f, 0.16f, 1.1f), matCeoMahogany, ceo);
            CreateCube("ThroneBack", pos + new Vector3(0f, 1.70f, 2.7f), new Vector3(1.2f, 1.9f, 0.25f), matCeoMahogany, ceo);

            // Left / Right Executive Director Offices (Sub-Towers at Z = +17f)
            GameObject dirL = CreateCube("DirSuiteL", new Vector3(LaneLeftX, 0.8f, 17.0f), new Vector3(1.2f, 1.6f, 1.2f), matCeoMahogany, ceo);
            GameObject dirR = CreateCube("DirSuiteR", new Vector3(LaneRightX, 0.8f, 17.0f), new Vector3(1.2f, 1.6f, 1.2f), matCeoMahogany, ceo);

            enemyMainTower = new TowerData { id = "E_Main", name = "대표이사 야근본부", hp = 2000, maxHp = 2000, pos = pos, isPlayer = false, isMain = true, go = ceo };
            enemySubLeft = new TowerData { id = "E_SubL", name = "부사장실(좌)", hp = 800, maxHp = 800, pos = new Vector3(LaneLeftX, 0f, 17f), isPlayer = false, isMain = false, go = dirL };
            enemySubRight = new TowerData { id = "E_SubR", name = "전무실(우)", hp = 800, maxHp = 800, pos = new Vector3(LaneRightX, 0f, 17f), isPlayer = false, isMain = false, go = dirR };
        }

        public void SetScreen(GameScreen next)
        {
            currentScreen = next;
            if (next == GameScreen.Battle)
            {
                camTargetZ = -14.0f;
                camCurrentZ = -14.0f;
                UpdateCameraTransform();
            }
        }

        public void StartMatch()
        {
            ResetGame(true);
            SetScreen(GameScreen.Battle);
            PlaySfx(SfxType.Summon);
        }

        public void ResetGame(bool playSound)
        {
            foreach (var u in playerUnits) if (u.go != null) Destroy(u.go);
            foreach (var u in enemyUnits) if (u.go != null) Destroy(u.go);
            playerUnits.Clear();
            enemyUnits.Clear();
            floatingTexts.Clear();

            // Reset Towers
            playerMainTower.hp = playerMainTower.maxHp;
            playerSubLeft.hp = playerSubLeft.maxHp;
            playerSubRight.hp = playerSubRight.maxHp;

            enemyMainTower.hp = enemyMainTower.maxHp;
            enemySubLeft.hp = enemySubLeft.maxHp;
            enemySubRight.hp = enemySubRight.maxHp;

            meetingHubWest.hp = meetingHubWest.maxHp;
            meetingHubWest.isPlayer = false;
            meetingHubWest.indicatorRenderer.material = matHubEnemy;

            meetingHubEast.hp = meetingHubEast.maxHp;
            meetingHubEast.isPlayer = false;
            meetingHubEast.indicatorRenderer.material = matHubEnemy;

            pantryChest.hp = pantryChest.maxHp;
            pantryChest.isDestroyed = false;
            pantryChest.respawnTimer = 0f;
            if (pantryChest.go != null) pantryChest.go.SetActive(true);

            caffeine = 5.0f;
            matchTimer = 180.0f;
            isGameOver = false;
            isVictory = false;
            enemySpawnTimer = 3.0f;
            coffeeCooldown = 0f;
            laserCooldown = 0f;
            useForwardSpawn = false;
            shakeIntensity = 0f;

            if (playSound) PlaySfx(SfxType.Summon);
        }

        private void Update()
        {
            // Title Screen Camera Orbit
            if (currentScreen == GameScreen.Title)
            {
                titleCamAngle += Time.deltaTime * 10f;
                float rad = titleCamAngle * Mathf.Deg2Rad;
                float camX = Mathf.Sin(rad) * 12.0f;
                float camZ = Mathf.Cos(rad) * 12.0f - 2f;
                mainCam.transform.position = new Vector3(camX, 16f, camZ);
                mainCam.transform.LookAt(new Vector3(0f, 1f, 0f));
                return;
            }

            if (currentScreen != GameScreen.Battle) return;

            // Screen Shake Decay
            if (shakeIntensity > 0f)
            {
                shakeIntensity = Mathf.Max(0f, shakeIntensity - Time.deltaTime * 6f);
            }

            HandleCameraInput();
            UpdateCameraTransform();

            if (isGameOver) return;

            // 1. Caffeine Mana Regeneration (Accelerates in Overtime last 60s!)
            float regenRate = (matchTimer <= 60.0f) ? 1.5f : 1.0f;
            caffeine = Mathf.Min(MaxCaffeine, caffeine + Time.deltaTime * regenRate);

            // 2. Cooldowns
            if (coffeeCooldown > 0f) coffeeCooldown -= Time.deltaTime;
            if (laserCooldown > 0f) laserCooldown -= Time.deltaTime;

            // 3. Match Timer
            matchTimer -= Time.deltaTime;
            if (matchTimer <= 0f)
            {
                EndMatch(playerMainTower.hp >= enemyMainTower.hp);
                return;
            }

            // 4. Enemy AI Waves
            enemySpawnTimer -= Time.deltaTime;
            if (enemySpawnTimer <= 0f)
            {
                SpawnEnemyWave();
                enemySpawnTimer = UnityEngine.Random.Range(4.0f, 6.5f);
            }

            // 5. Update Units & Combat
            UpdateUnitList(playerUnits, true);
            UpdateUnitList(enemyUnits, false);

            // 6. Pantry Chest Respawn
            if (pantryChest.isDestroyed)
            {
                pantryChest.respawnTimer -= Time.deltaTime;
                if (pantryChest.respawnTimer <= 0f)
                {
                    pantryChest.isDestroyed = false;
                    pantryChest.hp = pantryChest.maxHp;
                    if (pantryChest.go != null) pantryChest.go.SetActive(true);
                    AddFloatingText("☕ 탕비실 믹스커피 보급 상자 등장!", pantryChest.pos + Vector3.up * 2f, Color.yellow);
                }
            }

            // 7. Check Main Tower Destruction
            if (enemyMainTower.hp <= 0) EndMatch(true);
            else if (playerMainTower.hp <= 0) EndMatch(false);

            // 8. Update Floating Damage Texts
            for (int i = floatingTexts.Count - 1; i >= 0; i--)
            {
                floatingTexts[i].life -= Time.deltaTime;
                floatingTexts[i].worldPos += Vector3.up * Time.deltaTime * 1.6f;
                if (floatingTexts[i].life <= 0f) floatingTexts.RemoveAt(i);
            }
        }

        // --- Camera Scrolling & Drag System ---
        private void HandleCameraInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Only start drag if touch is in the upper 70% gameplay area
                if (Input.mousePosition.y > Screen.height * 0.32f)
                {
                    isDraggingCam = true;
                    dragStartPos = Input.mousePosition;
                    dragStartCamZ = camTargetZ;
                }
            }
            else if (Input.GetMouseButton(0) && isDraggingCam)
            {
                float deltaY = (Input.mousePosition.y - dragStartPos.y) / Screen.height;
                // Dragging down moves camera forward (up the battlefield)
                camTargetZ = Mathf.Clamp(dragStartCamZ - deltaY * 35.0f, PlayerBaseZ + 6f, EnemyBaseZ - 6f);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingCam = false;
            }

            // Smooth interpolation
            camCurrentZ = Mathf.Lerp(camCurrentZ, camTargetZ, Time.deltaTime * 8f);
        }

        private void UpdateCameraTransform()
        {
            Vector3 shake = Vector3.zero;
            if (shakeIntensity > 0f)
            {
                shake = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                ) * shakeIntensity;
            }

            // Tilted Top-Down Quarter View (56 deg tilt)
            mainCam.transform.position = new Vector3(0f, 21.0f, camCurrentZ - 14.5f) + shake;
            mainCam.transform.rotation = Quaternion.Euler(56f, 0f, 0f);
        }

        public void TriggerScreenShake(float intensity)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        }

        public void QuickJumpCamera(float targetZ)
        {
            camTargetZ = Mathf.Clamp(targetZ, PlayerBaseZ + 6f, EnemyBaseZ - 6f);
            PlaySfx(SfxType.Hit);
        }

        private void EndMatch(bool victory)
        {
            isGameOver = true;
            isVictory = victory;
            SetScreen(victory ? GameScreen.Victory : GameScreen.Defeat);
            PlaySfx(victory ? SfxType.Victory : SfxType.Defeat);
            TriggerScreenShake(1.2f);
        }

        private void SpawnEnemyWave()
        {
            int lane = UnityEngine.Random.Range(0, 3);
            float roll = UnityEngine.Random.value;

            if (roll < 0.40f)
            {
                SpawnEnemyUnit(UnitType.EnemyGoblin, lane);
            }
            else if (roll < 0.70f)
            {
                SpawnEnemyUnit(UnitType.EnemyDirector, lane);
            }
            else if (roll < 0.90f)
            {
                SpawnEnemyUnit(UnitType.EnemyExecutive, lane);
            }
            else
            {
                SpawnEnemyUnit(UnitType.EnemyBoss, lane);
            }
        }

        // --- Unit Spawning System ---
        public bool TrySpawnPlayerUnit(UnitType type)
        {
            float cost = GetUnitCost(type);
            if (caffeine < cost) return false;

            if (type == UnitType.OvertimeLaser)
            {
                if (laserCooldown > 0f) return false;
                caffeine -= cost;
                laserCooldown = 8.0f;
                ExecuteOvertimeLaser(selectedLane);
                return true;
            }

            if (type == UnitType.MixCoffee)
            {
                if (coffeeCooldown > 0f) return false;
                caffeine -= cost;
                coffeeCooldown = 12.0f;
                ExecuteMixCoffee();
                return true;
            }

            caffeine -= cost;
            SpawnPlayerUnit(type, selectedLane);
            PlaySfx(SfxType.Summon);
            return true;
        }

        private void SpawnPlayerUnit(UnitType type, int lane)
        {
            float x = GetLaneX(lane);
            float spawnZ = PlayerBaseZ + 2.5f;

            // Check Forward Spawn Option
            if (useForwardSpawn && (meetingHubWest.isPlayer || meetingHubEast.isPlayer))
            {
                spawnZ = 0.0f; // Forward spawn at middle hub!
                AddFloatingText("🚩 전진 기지 급파!", new Vector3(x, 1f, spawnZ), Color.cyan);
            }

            Vector3 spawnPos = new Vector3(x, 0.05f, spawnZ);
            GameObject go = Build3DCharacterObject(type, spawnPos, true);
            BattleUnit u = new BattleUnit
            {
                go = go,
                type = type,
                isPlayer = true,
                lane = lane,
                speed = GetUnitSpeed(type),
                damage = GetUnitDamage(type),
                maxHp = GetUnitMaxHp(type),
                hp = GetUnitMaxHp(type),
                attackRange = 1.5f,
                attackCooldown = 1.0f,
                modelRoot = go.transform
            };
            playerUnits.Add(u);
        }

        private void SpawnEnemyUnit(UnitType type, int lane)
        {
            float x = GetLaneX(lane);
            float spawnZ = EnemyBaseZ - 2.5f;

            // If enemy holds forward hubs, enemy can also push forward
            if (!meetingHubWest.isPlayer && !meetingHubEast.isPlayer && UnityEngine.Random.value < 0.35f)
            {
                spawnZ = 4.0f;
            }

            Vector3 spawnPos = new Vector3(x, 0.05f, spawnZ);
            GameObject go = Build3DCharacterObject(type, spawnPos, false);
            BattleUnit u = new BattleUnit
            {
                go = go,
                type = type,
                isPlayer = false,
                lane = lane,
                speed = GetUnitSpeed(type),
                damage = GetUnitDamage(type),
                maxHp = GetUnitMaxHp(type),
                hp = GetUnitMaxHp(type),
                attackRange = 1.5f,
                attackCooldown = 1.0f,
                modelRoot = go.transform
            };
            enemyUnits.Add(u);
        }

        // --- 3D Procedural Humanoid Characters ---
        private GameObject Build3DCharacterObject(UnitType type, Vector3 pos, bool isPlayer)
        {
            GameObject charGo = new GameObject(type.ToString());
            charGo.transform.position = pos;
            float rotY = isPlayer ? 0f : 180f;
            charGo.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            // Torso
            Material torsoMat = matInternHoodie;
            if (type == UnitType.AssistantManager) torsoMat = matDaeriShirt;
            else if (type == UnitType.SeniorManager) torsoMat = matGwajangVest;
            else if (type == UnitType.TeamLeader) torsoMat = matLeaderSuit;
            else if (type == UnitType.EnemyGoblin) torsoMat = matEnemyGoblin;
            else if (type == UnitType.EnemyDirector) torsoMat = matEnemyDirector;
            else if (type == UnitType.EnemyExecutive) torsoMat = matEnemyExecutive;
            else if (type == UnitType.EnemyBoss) torsoMat = matEnemyBoss;

            Vector3 torsoScale = new Vector3(0.55f, 0.70f, 0.40f);
            if (type == UnitType.SeniorManager || type == UnitType.TeamLeader) torsoScale = new Vector3(0.70f, 0.75f, 0.50f);
            if (type == UnitType.EnemyBoss) torsoScale = new Vector3(0.95f, 1.10f, 0.70f);

            CreateCube("Torso", pos + new Vector3(0f, 0.80f, 0f), torsoScale, torsoMat, charGo);

            // Head
            Material headMat = (type == UnitType.EnemyGoblin || type == UnitType.EnemyBoss) ? torsoMat : matSkin;
            Vector3 headScale = (type == UnitType.EnemyBoss) ? new Vector3(0.55f, 0.55f, 0.55f) : new Vector3(0.42f, 0.42f, 0.42f);
            float headY = (type == UnitType.EnemyBoss) ? 1.65f : 1.35f;
            CreateCube("Head", pos + new Vector3(0f, headY, 0f), headScale, headMat, charGo);

            // Legs
            Material legMat = (type == UnitType.Intern) ? matInternHoodie : matDaeriSlacks;
            CreateCube("LegL", pos + new Vector3(-0.18f, 0.28f, 0f), new Vector3(0.20f, 0.56f, 0.24f), legMat, charGo);
            CreateCube("LegR", pos + new Vector3(0.18f, 0.28f, 0f), new Vector3(0.20f, 0.56f, 0.24f), legMat, charGo);

            // Weapons & Props
            if (type == UnitType.SeniorManager)
            {
                // Giant Riot Partition Shield
                CreateCube("PartitionShield", pos + new Vector3(0.42f, 0.85f, 0.35f), new Vector3(0.85f, 1.3f, 0.12f), matGwajangShield, charGo);
            }
            else if (type == UnitType.TeamLeader)
            {
                // Giant Stainless Coffee Tumbler
                CreateCube("Tumbler", pos + new Vector3(0.40f, 0.90f, 0.30f), new Vector3(0.35f, 0.80f, 0.35f), matLeaderTumbler, charGo);
            }
            else if (type == UnitType.AssistantManager)
            {
                // Briefcase Club
                CreateCube("Briefcase", pos + new Vector3(0.38f, 0.65f, 0.20f), new Vector3(0.22f, 0.50f, 0.65f), matChair, charGo);
            }
            else if (type == UnitType.EnemyDirector)
            {
                // Red Rejection Stamp Hammer
                CreateCube("RejectStamp", pos + new Vector3(0.40f, 0.90f, 0.30f), new Vector3(0.35f, 0.50f, 0.35f), matHpRed, charGo);
            }

            return charGo;
        }

        // --- Combat & Update Loop ---
        private void UpdateUnitList(List<BattleUnit> list, bool isPlayerList)
        {
            float dt = Time.deltaTime;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                BattleUnit u = list[i];
                if (u.go == null || u.hp <= 0)
                {
                    if (u.go != null) Destroy(u.go);
                    list.RemoveAt(i);
                    continue;
                }

                // Team Leader Speed Aura Effect
                float effectiveSpeed = u.speed;
                if (isPlayerList)
                {
                    foreach (var ally in playerUnits)
                    {
                        if (ally.type == UnitType.TeamLeader && Vector3.Distance(ally.go.transform.position, u.go.transform.position) < 5.0f)
                        {
                            effectiveSpeed *= 1.25f;
                            break;
                        }
                    }
                }

                // Walking Bobbing Animation
                u.animTimer += dt * 10f;
                float bobY = Mathf.Sin(u.animTimer) * 0.06f;
                Vector3 curPos = u.go.transform.position;
                curPos.y = 0.05f + Mathf.Max(0f, bobY);
                u.go.transform.position = curPos;

                if (u.currentCooldown > 0f) u.currentCooldown -= dt;

                // 1. Check Target: Enemy Units in same lane
                BattleUnit targetUnit = FindTargetUnit(u, !isPlayerList);
                if (targetUnit != null)
                {
                    // Attack Unit
                    if (u.currentCooldown <= 0f)
                    {
                        AttackUnit(u, targetUnit);
                        u.currentCooldown = u.attackCooldown;
                    }
                    continue; // In combat, halt movement
                }

                // 2. Check Intermediate Meeting Hubs & Pantry Chest
                TowerData targetHub = CheckMeetingHubTarget(u);
                if (targetHub != null)
                {
                    if (u.currentCooldown <= 0f)
                    {
                        AttackTower(u, targetHub);
                        u.currentCooldown = u.attackCooldown;
                    }
                    continue;
                }

                if (!pantryChest.isDestroyed && Mathf.Abs(u.lane - 1) == 0) // Center lane attacks coffee chest
                {
                    if (Mathf.Abs(u.go.transform.position.z - pantryChest.pos.z) <= u.attackRange)
                    {
                        if (u.currentCooldown <= 0f)
                        {
                            AttackPantryChest(u);
                            u.currentCooldown = u.attackCooldown;
                        }
                        continue;
                    }
                }

                // 3. Check Target Towers (Sub Towers then Main HQ)
                TowerData targetTower = FindTargetTower(u, !isPlayerList);
                if (targetTower != null && Mathf.Abs(u.go.transform.position.z - targetTower.pos.z) <= u.attackRange)
                {
                    if (u.currentCooldown <= 0f)
                    {
                        AttackTower(u, targetTower);
                        u.currentCooldown = u.attackCooldown;
                    }
                    continue;
                }

                // 4. Move Forward along Lane
                float dir = isPlayerList ? 1.0f : -1.0f;
                u.go.transform.position += new Vector3(0f, 0f, dir * effectiveSpeed * dt);
            }
        }

        private BattleUnit FindTargetUnit(BattleUnit self, bool targetIsPlayer)
        {
            List<BattleUnit> targets = targetIsPlayer ? playerUnits : enemyUnits;
            BattleUnit nearest = null;
            float minDist = self.attackRange;

            foreach (var t in targets)
            {
                if (t.go == null || t.hp <= 0) continue;
                if (t.lane != self.lane) continue;

                float dist = Vector3.Distance(self.go.transform.position, t.go.transform.position);
                if (dist <= minDist)
                {
                    minDist = dist;
                    nearest = t;
                }
            }
            return nearest;
        }

        private TowerData CheckMeetingHubTarget(BattleUnit u)
        {
            // West Hub is on Left Lane, East Hub is on Right Lane
            TowerData hub = (u.lane == 0) ? meetingHubWest : (u.lane == 2) ? meetingHubEast : null;
            if (hub == null) return null;

            // If player unit, attack if hub is NOT owned by player
            if (u.isPlayer && !hub.isPlayer)
            {
                if (Mathf.Abs(u.go.transform.position.z - hub.pos.z) <= u.attackRange) return hub;
            }
            // If enemy unit, attack if hub IS owned by player
            else if (!u.isPlayer && hub.isPlayer)
            {
                if (Mathf.Abs(u.go.transform.position.z - hub.pos.z) <= u.attackRange) return hub;
            }
            return null;
        }

        private TowerData FindTargetTower(BattleUnit u, bool targetIsPlayer)
        {
            TowerData subL = targetIsPlayer ? playerSubLeft : enemySubLeft;
            TowerData subR = targetIsPlayer ? playerSubRight : enemySubRight;
            TowerData main = targetIsPlayer ? playerMainTower : enemyMainTower;

            if (u.lane == 0 && subL.hp > 0) return subL;
            if (u.lane == 2 && subR.hp > 0) return subR;
            return main;
        }

        private void AttackUnit(BattleUnit attacker, BattleUnit target)
        {
            target.hp -= attacker.damage;
            AddFloatingText("-" + attacker.damage, target.go.transform.position + Vector3.up * 1.5f, Color.red);
            PlaySfx(SfxType.Hit);

            if (target.hp <= 0)
            {
                AddFloatingText("퇴근 완료!", target.go.transform.position + Vector3.up * 1.8f, Color.yellow);
            }
        }

        private void AttackTower(BattleUnit attacker, TowerData tower)
        {
            tower.hp = Mathf.Max(0, tower.hp - attacker.damage);
            AddFloatingText("-" + attacker.damage, tower.pos + Vector3.up * 2.2f, Color.red);
            TriggerScreenShake(0.15f);
            PlaySfx(SfxType.Hit);

            if (tower.hp <= 0)
            {
                if (tower.isMeetingHub)
                {
                    // Hub Captured!
                    tower.isPlayer = attacker.isPlayer;
                    tower.hp = 500; // Reset HP for captured state
                    tower.indicatorRenderer.material = tower.isPlayer ? matHubPlayer : matHubEnemy;

                    string msg = tower.isPlayer ? "🚩 회의실 거점 탈환! (전진 소환 개통)" : "⚠️ 회의실 거점 상실!";
                    AddFloatingText(msg, tower.pos + Vector3.up * 3f, tower.isPlayer ? Color.cyan : Color.red);
                    TriggerScreenShake(0.4f);
                    PlaySfx(SfxType.TowerExplode);
                }
                else
                {
                    AddFloatingText("💥 거점 함락!", tower.pos + Vector3.up * 3f, Color.red);
                    TriggerScreenShake(0.6f);
                    PlaySfx(SfxType.TowerExplode);
                }
            }
        }

        private void AttackPantryChest(BattleUnit attacker)
        {
            pantryChest.hp = Mathf.Max(0, pantryChest.hp - attacker.damage);
            AddFloatingText("-" + attacker.damage, pantryChest.pos + Vector3.up * 1.8f, Color.yellow);
            PlaySfx(SfxType.Hit);

            if (pantryChest.hp <= 0)
            {
                pantryChest.isDestroyed = true;
                pantryChest.respawnTimer = 35.0f; // Respawns in 35s
                if (pantryChest.go != null) pantryChest.go.SetActive(false);

                if (attacker.isPlayer)
                {
                    caffeine = Mathf.Min(MaxCaffeine, caffeine + 3.5f);
                    AddFloatingText("☕ 믹스커피 대보급! +3.5 카페인!", pantryChest.pos + Vector3.up * 2.5f, Color.cyan);
                    PlaySfx(SfxType.ChestCollect);
                }
                else
                {
                    AddFloatingText("적에게 커피 보급 강탈당함!", pantryChest.pos + Vector3.up * 2.5f, Color.red);
                }
                TriggerScreenShake(0.3f);
            }
        }

        // --- Spells ---
        private void ExecuteOvertimeLaser(int lane)
        {
            float x = GetLaneX(lane);
            TriggerScreenShake(0.8f);
            PlaySfx(SfxType.Laser);

            // Visual Beam
            GameObject beam = CreateCube("LaserBeamFX", new Vector3(x, 1.2f, 0f), new Vector3(1.2f, 0.4f, 54f), matElevatorLight);
            Destroy(beam, 0.45f);

            // Damage all enemies in lane
            foreach (var e in enemyUnits)
            {
                if (e.lane == lane && e.go != null)
                {
                    e.hp -= 320;
                    AddFloatingText("-320 LASER!", e.go.transform.position + Vector3.up * 2f, Color.cyan);
                }
            }
            AddFloatingText("⚡ 칼퇴 레이저 발사!", new Vector3(x, 2f, -10f), Color.cyan);
        }

        private void ExecuteMixCoffee()
        {
            PlaySfx(SfxType.Coffee);
            AddFloatingText("☕ 전 사원 믹스커피 충전! (+280 HP & 이속 증가)", new Vector3(0f, 3f, 0f), Color.green);

            foreach (var u in playerUnits)
            {
                if (u.go != null)
                {
                    u.hp = Mathf.Min(u.maxHp, u.hp + 280);
                    u.speed *= 1.3f;
                    AddFloatingText("+280", u.go.transform.position + Vector3.up * 1.5f, Color.green);
                }
            }
            playerMainTower.hp = Mathf.Min(playerMainTower.maxHp, playerMainTower.hp + 200);
        }

        private void AddFloatingText(string text, Vector3 worldPos, Color col)
        {
            floatingTexts.Add(new FloatingText
            {
                text = text,
                worldPos = worldPos,
                color = col,
                life = 1.5f,
                maxLife = 1.5f
            });
        }

        // --- Audio System (Procedural 8-Bit Synth) ---
        private void PlaySfx(SfxType type)
        {
            if (audioSource == null) return;
            switch (type)
            {
                case SfxType.Summon:
                    audioSource.PlayOneShot(CreateBeep(440f, 0.12f, 0.25f));
                    break;
                case SfxType.Hit:
                    audioSource.PlayOneShot(CreateBeep(180f, 0.08f, 0.35f));
                    break;
                case SfxType.Laser:
                    audioSource.PlayOneShot(CreateBeep(880f, 0.35f, 0.45f));
                    break;
                case SfxType.Coffee:
                    audioSource.PlayOneShot(CreateBeep(587f, 0.25f, 0.30f));
                    break;
                case SfxType.TowerExplode:
                    audioSource.PlayOneShot(CreateBeep(90f, 0.50f, 0.60f));
                    break;
                case SfxType.ChestCollect:
                    audioSource.PlayOneShot(CreateBeep(659f, 0.20f, 0.40f));
                    break;
                case SfxType.Victory:
                    audioSource.PlayOneShot(CreateBeep(523f, 0.60f, 0.50f));
                    break;
                case SfxType.Defeat:
                    audioSource.PlayOneShot(CreateBeep(130f, 0.70f, 0.50f));
                    break;
            }
        }

        private AudioClip CreateBeep(float freq, float duration, float vol)
        {
            int rate = 44100;
            int count = (int)(rate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * vol * (1f - t / duration);
            }
            AudioClip c = AudioClip.Create("SFX_" + freq, count, 1, rate, false);
            c.SetData(samples, 0);
            return c;
        }

        // --- High-End Mobile UI (Virtual 750 x 1334 High-DPI Canvas) ---
        private void OnGUI()
        {
            float rx = Screen.width / 750f;
            float ry = Screen.height / 1334f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(rx, ry, 1f));

            switch (currentScreen)
            {
                case GameScreen.Title:
                    DrawTitleScreen();
                    break;
                case GameScreen.Battle:
                    DrawBattleHUD();
                    break;
                case GameScreen.Codex:
                    DrawCodexScreen();
                    break;
                case GameScreen.Settings:
                    DrawSettingsScreen();
                    break;
                case GameScreen.Victory:
                    DrawBattleHUD();
                    DrawEndPopup(true);
                    break;
                case GameScreen.Defeat:
                    DrawBattleHUD();
                    DrawEndPopup(false);
                    break;
            }

            // Draw Floating Combat Texts
            DrawFloatingCombatTexts();
        }

        private void DrawTitleScreen()
        {
            GUI.DrawTexture(new Rect(40, 110, 670, 270), texPanelHeader);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 46,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(50, 130, 650, 60), "칼퇴 럼블", titleStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            subStyle.normal.textColor = new Color(0.35f, 0.75f, 1.0f);
            GUI.Label(new Rect(50, 195, 650, 30), "OFFICE RUMBLE: GRAND ARENA v0.3.0", subStyle);

            GUIStyle editionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            editionStyle.normal.textColor = new Color(0.75f, 0.82f, 0.92f);
            GUI.Label(new Rect(50, 235, 650, 30), "워크래프트 럼블 스타일 대형 종스크롤 전장 & 거점 기믹", editionStyle);
            GUI.Label(new Rect(50, 290, 650, 40), "중간 회의실 거점을 탈환하여 대표이사 본사를 정복하라!", editionStyle);

            GUIStyle btnMainStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            btnMainStyle.normal.background = texBtnSpecial;
            btnMainStyle.normal.textColor = Color.white;

            if (GUI.Button(new Rect(95, 780, 560, 85), "[ 게임 시작 (출근 및 야근 저지) ]", btnMainStyle))
            {
                StartMatch();
            }

            GUIStyle btnSubStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            btnSubStyle.normal.background = texBtnNormal;
            btnSubStyle.normal.textColor = new Color(0.85f, 0.90f, 0.98f);

            if (GUI.Button(new Rect(95, 885, 560, 65), "[ 사원 카드 도감 (유닛 정보) ]", btnSubStyle))
            {
                SetScreen(GameScreen.Codex);
                PlaySfx(SfxType.Hit);
            }

            if (GUI.Button(new Rect(95, 965, 560, 65), "[ 게임 환경 설정 ]", btnSubStyle))
            {
                SetScreen(GameScreen.Settings);
                PlaySfx(SfxType.Hit);
            }
        }

        private void DrawBattleHUD()
        {
            // 1. Top Status Bar (Boss HP vs Player HQ HP, Timer, Hub Status)
            GUI.DrawTexture(new Rect(20, 20, 710, 130), texPanelDark);

            // Enemy HQ
            float enemyHpPct = Mathf.Clamp01((float)enemyMainTower.hp / enemyMainTower.maxHp);
            GUI.DrawTexture(new Rect(35, 40, 230, 26), texBarBg);
            GUI.DrawTexture(new Rect(35, 40, 230 * enemyHpPct, 26), texHpRed);

            GUIStyle textSmall = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            textSmall.normal.textColor = Color.white;
            GUI.Label(new Rect(40, 42, 220, 22), string.Format("[야근본부] {0}/{1}", enemyMainTower.hp, enemyMainTower.maxHp), textSmall);

            // Timer & Speed Toggle
            int min = (int)(matchTimer / 60);
            int sec = (int)(matchTimer % 60);
            GUIStyle timerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            timerStyle.normal.textColor = (matchTimer <= 30f) ? Color.red : (matchTimer <= 60f) ? Color.yellow : Color.white;
            GUI.Label(new Rect(280, 35, 190, 36), string.Format("{0:00}:{1:00}", min, sec), timerStyle);

            GUIStyle spdBtn = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            spdBtn.normal.background = (gameSpeed == 2) ? texBtnActive : texBtnNormal;
            spdBtn.normal.textColor = Color.white;
            if (GUI.Button(new Rect(335, 75, 80, 28), (gameSpeed == 2) ? "▶▶ 2X" : "▶ 1X", spdBtn))
            {
                gameSpeed = (gameSpeed == 1) ? 2 : 1;
                Time.timeScale = gameSpeed;
                PlaySfx(SfxType.Hit);
            }

            // Player HQ
            float playerHpPct = Mathf.Clamp01((float)playerMainTower.hp / playerMainTower.maxHp);
            GUI.DrawTexture(new Rect(485, 40, 230, 26), texBarBg);
            GUI.DrawTexture(new Rect(485, 40, 230 * playerHpPct, 26), texHpGreen);
            GUI.Label(new Rect(490, 42, 220, 22), string.Format("[칼퇴엘베] {0}/{1}", playerMainTower.hp, playerMainTower.maxHp), textSmall);

            // Hub Status Indicators
            GUIStyle hubStatusStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            string westHubStr = meetingHubWest.isPlayer ? "[좌 전략회의실: 아군탈환🚩]" : "[좌 전략회의실: 적점령]";
            string eastHubStr = meetingHubEast.isPlayer ? "[우 마케팅실: 아군탈환🚩]" : "[우 마케팅실: 적점령]";
            hubStatusStyle.normal.textColor = meetingHubWest.isPlayer ? Color.cyan : new Color(0.85f, 0.50f, 0.50f);
            GUI.Label(new Rect(35, 110, 330, 22), westHubStr, hubStatusStyle);
            hubStatusStyle.normal.textColor = meetingHubEast.isPlayer ? Color.cyan : new Color(0.85f, 0.50f, 0.50f);
            GUI.Label(new Rect(385, 110, 330, 22), eastHubStr, hubStatusStyle);

            // 2. Camera Quick Jump Buttons (Right Side)
            GUIStyle jumpBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            jumpBtnStyle.normal.background = texBtnNormal;
            jumpBtnStyle.normal.textColor = Color.white;

            if (GUI.Button(new Rect(630, 165, 100, 40), "적진 22m", jumpBtnStyle)) QuickJumpCamera(EnemyBaseZ - 6f);
            if (GUI.Button(new Rect(630, 215, 100, 40), "전선 0m", jumpBtnStyle)) QuickJumpCamera(0f);
            if (GUI.Button(new Rect(630, 265, 100, 40), "본진 -22m", jumpBtnStyle)) QuickJumpCamera(PlayerBaseZ + 6f);

            // 3. Lane Selector & Forward Deploy Toggle
            GUI.DrawTexture(new Rect(20, 930, 710, 65), texPanelDark);
            GUIStyle laneBtn = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            laneBtn.normal.textColor = Color.white;

            laneBtn.normal.background = (selectedLane == 0) ? texBtnActive : texBtnNormal;
            if (GUI.Button(new Rect(30, 940, 140, 45), "◀ 좌측 레인", laneBtn)) selectedLane = 0;

            laneBtn.normal.background = (selectedLane == 1) ? texBtnActive : texBtnNormal;
            if (GUI.Button(new Rect(180, 940, 140, 45), "▲ 중앙 레인", laneBtn)) selectedLane = 1;

            laneBtn.normal.background = (selectedLane == 2) ? texBtnActive : texBtnNormal;
            if (GUI.Button(new Rect(330, 940, 140, 45), "▶ 우측 레인", laneBtn)) selectedLane = 2;

            // Forward Spawn Toggle Button
            bool canForwardSpawn = (meetingHubWest.isPlayer || meetingHubEast.isPlayer);
            GUIStyle fwdBtn = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            fwdBtn.normal.background = useForwardSpawn ? texBtnSpecial : texBtnNormal;
            fwdBtn.normal.textColor = canForwardSpawn ? Color.white : Color.gray;

            string fwdText = useForwardSpawn ? "🚩 전진소환: 회의실" : "🏠 본부소환";
            if (GUI.Button(new Rect(485, 940, 235, 45), fwdText, fwdBtn))
            {
                if (canForwardSpawn)
                {
                    useForwardSpawn = !useForwardSpawn;
                    PlaySfx(SfxType.Hit);
                }
                else
                {
                    AddFloatingText("회의실 거점을 먼저 탈환해야 합니다!", new Vector3(0f, 1f, camCurrentZ), Color.yellow);
                }
            }

            // 4. Caffeine Mana Bar
            GUI.DrawTexture(new Rect(20, 1005, 710, 40), texPanelDark);
            float caffPct = Mathf.Clamp01(caffeine / MaxCaffeine);
            GUI.DrawTexture(new Rect(35, 1013, 680, 24), texBarBg);
            GUI.DrawTexture(new Rect(35, 1013, 680 * caffPct, 24), texCaffeineFill);

            GUIStyle caffText = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            caffText.normal.textColor = Color.white;
            GUI.Label(new Rect(35, 1013, 680, 24), string.Format("☕ 카페인: {0:F1} / {1:F0}", caffeine, MaxCaffeine), caffText);

            // 5. Unit Cards & Spells Deck
            GUI.DrawTexture(new Rect(20, 1055, 710, 260), texPanelDark);
            DrawUnitCard(new Rect(30, 1065, 105, 240), UnitType.Intern, "인턴", "2☕", "신속 침투", 2);
            DrawUnitCard(new Rect(145, 1065, 105, 240), UnitType.AssistantManager, "대리", "3☕", "표준 딜러", 3);
            DrawUnitCard(new Rect(260, 1065, 105, 240), UnitType.SeniorManager, "과장", "4☕", "방패 탱커", 4);
            DrawUnitCard(new Rect(375, 1065, 105, 240), UnitType.TeamLeader, "기획부장", "5☕", "이속 버퍼", 5);
            DrawUnitCard(new Rect(490, 1065, 105, 240), UnitType.OvertimeLaser, "칼퇴빔", "4☕", "전체 관통", 4);
            DrawUnitCard(new Rect(605, 1065, 105, 240), UnitType.MixCoffee, "믹스커피", "3☕", "전체 힐", 3);
        }

        private void DrawUnitCard(Rect rect, UnitType type, string name, string costStr, string desc, int cost)
        {
            bool canAfford = (caffeine >= cost);
            if (type == UnitType.OvertimeLaser && laserCooldown > 0f) canAfford = false;
            if (type == UnitType.MixCoffee && coffeeCooldown > 0f) canAfford = false;

            GUI.DrawTexture(rect, canAfford ? texCardActive : texCardBg);

            GUIStyle cardTitle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            cardTitle.normal.textColor = Color.white;
            GUI.Label(new Rect(rect.x, rect.y + 12, rect.width, 24), name, cardTitle);

            GUIStyle cardCost = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            cardCost.normal.textColor = Color.yellow;
            GUI.Label(new Rect(rect.x, rect.y + 38, rect.width, 22), costStr, cardCost);

            GUIStyle cardDesc = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            cardDesc.normal.textColor = new Color(0.80f, 0.85f, 0.92f);
            GUI.Label(new Rect(rect.x + 4, rect.y + 65, rect.width - 8, 45), desc, cardDesc);

            // Cooldown overlay for spells
            if (type == UnitType.OvertimeLaser && laserCooldown > 0f)
            {
                GUI.Label(new Rect(rect.x, rect.y + 120, rect.width, 30), string.Format("{0:F1}s", laserCooldown), cardCost);
            }
            else if (type == UnitType.MixCoffee && coffeeCooldown > 0f)
            {
                GUI.Label(new Rect(rect.x, rect.y + 120, rect.width, 30), string.Format("{0:F1}s", coffeeCooldown), cardCost);
            }

            GUIStyle spawnBtn = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            spawnBtn.normal.background = canAfford ? texBtnActive : texBtnNormal;
            spawnBtn.normal.textColor = canAfford ? Color.white : Color.gray;

            if (GUI.Button(new Rect(rect.x + 8, rect.y + 175, rect.width - 16, 52), "출격", spawnBtn))
            {
                TrySpawnPlayerUnit(type);
            }
        }

        private void DrawFloatingCombatTexts()
        {
            GUIStyle floatStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            foreach (var ft in floatingTexts)
            {
                Vector3 screenPos = mainCam.WorldToScreenPoint(ft.worldPos);
                if (screenPos.z > 0)
                {
                    float alpha = Mathf.Clamp01(ft.life / ft.maxLife);
                    Color c = ft.color;
                    c.a = alpha;
                    floatStyle.normal.textColor = c;

                    // Convert to 750x1334 scaled coordinates
                    float virtualX = (screenPos.x / Screen.width) * 750f;
                    float virtualY = ((Screen.height - screenPos.y) / Screen.height) * 1334f;
                    GUI.Label(new Rect(virtualX - 150, virtualY - 20, 300, 40), ft.text, floatStyle);
                }
            }
        }

        private void DrawEndPopup(bool victory)
        {
            GUI.DrawTexture(new Rect(80, 400, 590, 450), texPanelDark);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = victory ? Color.cyan : Color.red;
            GUI.Label(new Rect(90, 430, 570, 60), victory ? "🏆 정시 퇴근 달성!" : "💀 야근 확정 (패배)", titleStyle);

            GUIStyle descStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            descStyle.normal.textColor = Color.white;
            string desc = victory ? "대표이사 본사를 함락시키고 칼퇴에 성공했습니다!" : "대표이사 본사의 결재 폭풍에 밀려 정시퇴근에 실패했습니다.";
            GUI.Label(new Rect(100, 510, 550, 70), desc, descStyle);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            btnStyle.normal.background = texBtnSpecial;
            btnStyle.normal.textColor = Color.white;

            if (GUI.Button(new Rect(145, 620, 460, 75), "[ 다시 도전하기 ]", btnStyle))
            {
                StartMatch();
            }

            GUIStyle btnSub = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            btnSub.normal.background = texBtnNormal;
            btnSub.normal.textColor = Color.white;

            if (GUI.Button(new Rect(145, 715, 460, 65), "[ 메인 메뉴로 이동 ]", btnSub))
            {
                SetScreen(GameScreen.Title);
            }
        }

        private void DrawCodexScreen()
        {
            GUI.DrawTexture(new Rect(40, 100, 670, 1140), texPanelDark);

            GUIStyle header = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            header.normal.textColor = Color.white;
            GUI.Label(new Rect(50, 120, 650, 50), "사원 카드 및 기믹 도감", header);

            GUIStyle itemTitle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            itemTitle.normal.textColor = Color.yellow;
            GUIStyle itemDesc = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            itemDesc.normal.textColor = new Color(0.85f, 0.88f, 0.95f);

            int y = 190;
            DrawCodexEntry(ref y, "1. 인턴 (2☕)", "초반 기동성이 뛰어난 침투 유닛. 거점 선점 및 적 빈집 털이에 최적화.", itemTitle, itemDesc);
            DrawCodexEntry(ref y, "2. 대리 (3☕)", "서류 가방을 휘두르는 밸런스형 전선 딜러. 안정적인 라인 유지력 보유.", itemTitle, itemDesc);
            DrawCodexEntry(ref y, "3. 과장 (4☕)", "거대 파티션 방패로 적의 공격을 온몸으로 막아내는 든든한 최전방 탱커.", itemTitle, itemDesc);
            DrawCodexEntry(ref y, "4. 기획부장 (5☕)", "거대 텀블러를 든 버퍼. 주변 아군 유닛의 이동/공격 속도를 25% 가속.", itemTitle, itemDesc);
            DrawCodexEntry(ref y, "5. 칼퇴 레이저 (4☕)", "지정한 레인 전체를 관통하여 적 다수에게 320 광역 피해와 넉백 부여.", itemTitle, itemDesc);
            DrawCodexEntry(ref y, "6. 믹스커피 샤워 (3☕)", "전장에 믹스커피 향기를 뿌려 아군 전원 280 체력 회복 및 이속 증가.", itemTitle, itemDesc);
            DrawCodexEntry(ref y, "★ 중간 회의실 거점 (Capture Point)", "좌/우 중간 회의실을 파괴해 탈환하면 전진 소환 기지 개통! (동선 50% 단축)", itemTitle, itemDesc);
            DrawCodexEntry(ref y, "★ 탕비실 믹스커피 상자 (Gold Chest)", "중앙 레인의 커피머신을 부수면 +3.5 카페인 보급 즉시 지급! (35초 후 리젠)", itemTitle, itemDesc);

            GUIStyle btnBack = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            btnBack.normal.background = texBtnNormal;
            btnBack.normal.textColor = Color.white;

            if (GUI.Button(new Rect(145, 1160, 460, 65), "[ 메인 메뉴로 돌아가기 ]", btnBack))
            {
                SetScreen(GameScreen.Title);
            }
        }

        private void DrawCodexEntry(ref int y, string title, string desc, GUIStyle titleStyle, GUIStyle descStyle)
        {
            GUI.Label(new Rect(60, y, 630, 26), title, titleStyle);
            GUI.Label(new Rect(60, y + 26, 630, 45), desc, descStyle);
            y += 85;
        }

        private void DrawSettingsScreen()
        {
            GUI.DrawTexture(new Rect(60, 300, 630, 650), texPanelDark);

            GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = Color.white;
            GUI.Label(new Rect(70, 330, 610, 50), "게임 환경 설정", title);

            GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            label.normal.textColor = Color.white;

            GUI.Label(new Rect(90, 420, 200, 35), "그래픽 품질", label);
            GUI.Label(new Rect(90, 480, 200, 35), "프레임 레이트", label);
            GUI.Label(new Rect(90, 540, 200, 35), "사운드 효과음", label);
            GUI.Label(new Rect(90, 600, 200, 35), "배포 버전", label);

            GUIStyle valStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleRight };
            valStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(320, 420, 320, 35), "Synty 3D Ultra (URP Lit)", valStyle);
            GUI.Label(new Rect(320, 480, 320, 35), "60 FPS High-DPI", valStyle);
            GUI.Label(new Rect(320, 540, 320, 35), "8-Bit Dynamic Synth ON", valStyle);
            GUI.Label(new Rect(320, 600, 320, 35), "v0.3.0 (Build 3)", valStyle);

            GUIStyle btnBack = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            btnBack.normal.background = texBtnSpecial;
            btnBack.normal.textColor = Color.white;

            if (GUI.Button(new Rect(135, 780, 480, 75), "[ 설정 저장 및 메뉴 복귀 ]", btnBack))
            {
                SetScreen(GameScreen.Title);
            }
        }

        // --- Balance Helpers ---
        private float GetLaneX(int lane)
        {
            if (lane == 0) return LaneLeftX;
            if (lane == 2) return LaneRightX;
            return LaneCenterX;
        }

        private float GetUnitCost(UnitType t)
        {
            switch (t)
            {
                case UnitType.Intern: return 2.0f;
                case UnitType.AssistantManager: return 3.0f;
                case UnitType.SeniorManager: return 4.0f;
                case UnitType.TeamLeader: return 5.0f;
                case UnitType.OvertimeLaser: return 4.0f;
                case UnitType.MixCoffee: return 3.0f;
                default: return 3.0f;
            }
        }

        private float GetUnitSpeed(UnitType t)
        {
            switch (t)
            {
                case UnitType.Intern: return 3.2f;
                case UnitType.AssistantManager: return 2.2f;
                case UnitType.SeniorManager: return 1.5f;
                case UnitType.TeamLeader: return 1.8f;
                case UnitType.EnemyGoblin: return 3.0f;
                case UnitType.EnemyDirector: return 1.9f;
                case UnitType.EnemyExecutive: return 2.1f;
                case UnitType.EnemyBoss: return 1.3f;
                default: return 2.0f;
            }
        }

        private int GetUnitDamage(UnitType t)
        {
            switch (t)
            {
                case UnitType.Intern: return 45;
                case UnitType.AssistantManager: return 90;
                case UnitType.SeniorManager: return 65;
                case UnitType.TeamLeader: return 130;
                case UnitType.EnemyGoblin: return 35;
                case UnitType.EnemyDirector: return 85;
                case UnitType.EnemyExecutive: return 110;
                case UnitType.EnemyBoss: return 190;
                default: return 50;
            }
        }

        private int GetUnitMaxHp(UnitType t)
        {
            switch (t)
            {
                case UnitType.Intern: return 190;
                case UnitType.AssistantManager: return 420;
                case UnitType.SeniorManager: return 950;
                case UnitType.TeamLeader: return 750;
                case UnitType.EnemyGoblin: return 160;
                case UnitType.EnemyDirector: return 450;
                case UnitType.EnemyExecutive: return 380;
                case UnitType.EnemyBoss: return 1200;
                default: return 300;
            }
        }
    }
}
