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

        // --- Game State & Screens ---
        public GameScreen currentScreen = GameScreen.Title;
        private float screenTransitionAlpha = 0f;
        private float targetTransitionAlpha = 0f;
        private GameScreen pendingScreen = GameScreen.Title;

        // --- Game Balance Constants ---
        public const float LaneLeftX = -3.4f;
        public const float LaneCenterX = 0.0f;
        public const float LaneRightX = 3.4f;
        public const float PlayerBaseZ = -9.5f;
        public const float EnemyBaseZ = 9.5f;

        public int selectedLane = 1; // 0: Left, 1: Center, 2: Right
        public float caffeine = 4.0f;
        public const float MaxCaffeine = 10.0f;
        public float matchTimer = 180.0f; // 3 minutes
        public bool isGameOver = false;
        public bool isVictory = false;

        // Settings
        public bool bgmEnabled = true;
        public bool sfxEnabled = true;
        public bool highFpsMode = true;

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

        // Timers & Cooldowns
        private float enemySpawnTimer = 3.5f;
        private float coffeeCooldown = 0f;
        private float laserCooldown = 0f;
        private float bgmLoopTimer = 0f;
        private int currentBgmNote = 0;

        // Camera Orbit on Title
        private Camera mainCam;
        private float titleCamAngle = 0f;

        // Shared Materials (Cached & Guaranteed Non-Magenta)
        private Material matCarpet;
        private Material matAisle;
        private Material matLaneActive;
        private Material matWall;
        private Material matDeskWood;
        private Material matDeskMetal;
        private Material matScreenBlue;
        private Material matChair;
        private Material matPlantPot;
        private Material matPlantLeaf;
        private Material matWaterDispenser;
        private Material matWaterBottle;
        private Material matElevatorSteel;
        private Material matElevatorLight;
        private Material matCeoMahogany;
        private Material matGoldTrim;
        private Material matPaperStack;

        // Character Materials
        private Material matSkin;
        private Material matInternHoodie;
        private Material matDaeriShirt;
        private Material matDaeriSlacks;
        private Material matDaeriTie;
        private Material matGwajangVest;
        private Material matGwajangShield;
        private Material matEnemyGoblin;
        private Material matEnemyDirector;
        private Material matEnemyBoss;

        // Procedural Textures for High-End UI
        private Texture2D texPanelDark;
        private Texture2D texPanelHeader;
        private Texture2D texBtnNormal;
        private Texture2D texBtnHover;
        private Texture2D texBtnActive;
        private Texture2D texBtnSpecial;
        private Texture2D texBarBg;
        private Texture2D texHpGreen;
        private Texture2D texHpRed;
        private Texture2D texCaffeineFill;
        private Texture2D texCardBg;
        private Texture2D texCardActive;
        private Texture2D texWhite;

        private AudioSource audioSource;
        private GameObject arenaRoot;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
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
            mainCam.backgroundColor = new Color(0.10f, 0.12f, 0.16f); // Dark Navy Office Ambient

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            SetupDirectionalLight();
            CreateMaterials();
            CreateProceduralTextures();
            BuildDetailedOfficeArena();
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
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.color = new Color(1.0f, 0.96f, 0.90f); // Warm corporate lighting
            sun.intensity = 1.35f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.52f);
        }

        private void CreateMaterials()
        {
            matCarpet = CreateSolidMaterial(new Color(0.18f, 0.22f, 0.28f)); // Deep corporate carpet
            matAisle = CreateSolidMaterial(new Color(0.24f, 0.29f, 0.36f)); // Lane corridor runner
            matLaneActive = CreateSolidMaterial(new Color(0.15f, 0.65f, 0.95f), new Color(0.05f, 0.25f, 0.45f)); // Cyan highlight
            matWall = CreateSolidMaterial(new Color(0.12f, 0.14f, 0.18f));
            matDeskWood = CreateSolidMaterial(new Color(0.82f, 0.70f, 0.55f)); // Birch desk
            matDeskMetal = CreateSolidMaterial(new Color(0.35f, 0.38f, 0.42f));
            matScreenBlue = CreateSolidMaterial(new Color(0.10f, 0.20f, 0.35f), new Color(0.20f, 0.60f, 1.0f)); // Glowing monitor
            matChair = CreateSolidMaterial(new Color(0.22f, 0.22f, 0.25f));
            matPlantPot = CreateSolidMaterial(new Color(0.75f, 0.40f, 0.25f)); // Terracotta
            matPlantLeaf = CreateSolidMaterial(new Color(0.18f, 0.60f, 0.25f)); // Lush Ficus
            matWaterDispenser = CreateSolidMaterial(new Color(0.88f, 0.90f, 0.92f));
            matWaterBottle = CreateSolidMaterial(new Color(0.20f, 0.60f, 0.90f), new Color(0.10f, 0.30f, 0.50f));
            matElevatorSteel = CreateSolidMaterial(new Color(0.70f, 0.75f, 0.80f));
            matElevatorLight = CreateSolidMaterial(new Color(0.20f, 0.90f, 0.40f), new Color(0.30f, 1.0f, 0.50f)); // Green Exit Sign
            matCeoMahogany = CreateSolidMaterial(new Color(0.35f, 0.15f, 0.10f)); // Luxury mahogany
            matGoldTrim = CreateSolidMaterial(new Color(0.95f, 0.80f, 0.20f), new Color(0.40f, 0.30f, 0.05f));
            matPaperStack = CreateSolidMaterial(new Color(0.92f, 0.92f, 0.94f));

            // Character Materials
            matSkin = CreateSolidMaterial(new Color(0.98f, 0.82f, 0.70f));
            matInternHoodie = CreateSolidMaterial(new Color(0.20f, 0.65f, 0.95f)); // Blue hoodie
            matDaeriShirt = CreateSolidMaterial(new Color(0.95f, 0.95f, 0.98f)); // White shirt
            matDaeriSlacks = CreateSolidMaterial(new Color(0.15f, 0.20f, 0.30f)); // Navy slacks
            matDaeriTie = CreateSolidMaterial(new Color(0.85f, 0.20f, 0.20f)); // Red tie
            matGwajangVest = CreateSolidMaterial(new Color(0.45f, 0.48f, 0.52f)); // Grey vest
            matGwajangShield = CreateSolidMaterial(new Color(0.92f, 0.92f, 0.95f), new Color(0.10f, 0.20f, 0.30f)); // White office partition shield
            matEnemyGoblin = CreateSolidMaterial(new Color(0.55f, 0.20f, 0.75f), new Color(0.25f, 0.05f, 0.40f)); // Purple goblin
            matEnemyDirector = CreateSolidMaterial(new Color(0.28f, 0.12f, 0.15f)); // Pinstripe suit
            matEnemyBoss = CreateSolidMaterial(new Color(0.20f, 0.05f, 0.05f), new Color(0.85f, 0.15f, 0.15f)); // Fiery Red
        }

        private Material CreateSolidMaterial(Color col, Color? emission = null)
        {
            Material baseMat = Resources.Load<Material>("Materials/OfficeLit");
            Material mat;
            if (baseMat != null)
            {
                mat = new Material(baseMat);
            }
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
            texPanelDark = MakeRoundedBoxTexture(128, 128, new Color(0.08f, 0.10f, 0.14f, 0.92f), new Color(0.25f, 0.30f, 0.40f, 0.80f), 2);
            texPanelHeader = MakeRoundedBoxTexture(128, 128, new Color(0.12f, 0.15f, 0.22f, 0.95f), new Color(0.35f, 0.45f, 0.60f, 0.80f), 2);
            texBtnNormal = MakeRoundedBoxTexture(128, 64, new Color(0.16f, 0.20f, 0.28f, 0.95f), new Color(0.30f, 0.38f, 0.50f, 0.90f), 2);
            texBtnHover = MakeRoundedBoxTexture(128, 64, new Color(0.22f, 0.30f, 0.45f, 0.98f), new Color(0.45f, 0.65f, 0.95f, 1.0f), 2);
            texBtnActive = MakeRoundedBoxTexture(128, 64, new Color(0.15f, 0.50f, 0.85f, 1.0f), new Color(0.70f, 0.90f, 1.0f, 1.0f), 2);
            texBtnSpecial = MakeRoundedBoxTexture(128, 64, new Color(0.85f, 0.45f, 0.10f, 1.0f), new Color(1.0f, 0.80f, 0.30f, 1.0f), 2);
            texBarBg = MakeSolidTexture(4, 4, new Color(0.06f, 0.08f, 0.10f, 0.95f));
            texHpGreen = MakeSolidTexture(4, 4, new Color(0.18f, 0.82f, 0.35f, 1.0f));
            texHpRed = MakeSolidTexture(4, 4, new Color(0.90f, 0.25f, 0.25f, 1.0f));
            texCaffeineFill = MakeSolidTexture(4, 4, new Color(0.20f, 0.75f, 1.0f, 1.0f));
            texCardBg = MakeRoundedBoxTexture(128, 160, new Color(0.14f, 0.17f, 0.24f, 0.95f), new Color(0.30f, 0.35f, 0.45f, 0.80f), 2);
            texCardActive = MakeRoundedBoxTexture(128, 160, new Color(0.18f, 0.26f, 0.40f, 0.98f), new Color(0.30f, 0.75f, 1.0f, 1.0f), 3);
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

        // --- Detailed 3D Office Arena Builder ---
        private void BuildDetailedOfficeArena()
        {
            if (arenaRoot != null) Destroy(arenaRoot);
            arenaRoot = new GameObject("OfficeArena_Root");

            // 1. Base Carpet Floor
            GameObject floor = CreateCube("OfficeCarpetFloor", new Vector3(0f, -0.25f, 0f), new Vector3(12.5f, 0.5f, 26.0f), matCarpet, arenaRoot);

            // 2. 3 Lane Strips
            float[] laneX = { LaneLeftX, LaneCenterX, LaneRightX };
            for (int i = 0; i < 3; i++)
            {
                CreateCube("LaneStrip_" + i, new Vector3(laneX[i], 0.02f, 0f), new Vector3(2.4f, 0.05f, 24.0f), matAisle, arenaRoot);
            }

            // 3. Center Partition Divider with Door Openings
            CreateCube("CenterPartition_Left", new Vector3(-1.7f, 0.45f, 0f), new Vector3(1.2f, 0.9f, 0.25f), matWall, arenaRoot);
            CreateCube("CenterPartition_Right", new Vector3(1.7f, 0.45f, 0f), new Vector3(1.2f, 0.9f, 0.25f), matWall, arenaRoot);
            CreateCube("CenterPartition_FarLeft", new Vector3(-4.8f, 0.45f, 0f), new Vector3(1.6f, 0.9f, 0.25f), matWall, arenaRoot);
            CreateCube("CenterPartition_FarRight", new Vector3(4.8f, 0.45f, 0f), new Vector3(1.6f, 0.9f, 0.25f), matWall, arenaRoot);

            // 4. Perimeter Office Walls
            CreateCube("Wall_Left", new Vector3(-6.2f, 1.5f, 0f), new Vector3(0.4f, 3.0f, 26.0f), matWall, arenaRoot);
            CreateCube("Wall_Right", new Vector3(6.2f, 1.5f, 0f), new Vector3(0.4f, 3.0f, 26.0f), matWall, arenaRoot);
            CreateCube("Wall_Bottom", new Vector3(0f, 1.5f, -13.0f), new Vector3(12.5f, 3.0f, 0.4f), matWall, arenaRoot);
            CreateCube("Wall_Top", new Vector3(0f, 1.5f, 13.0f), new Vector3(12.5f, 3.0f, 0.4f), matWall, arenaRoot);

            // 5. Cubicle Workstations Along Left & Right Corridors
            float[] deskZPositions = { -6.5f, -2.5f, 2.5f, 6.5f };
            foreach (float z in deskZPositions)
            {
                BuildCubicleDesk(new Vector3(-4.9f, 0f, z), 90f, arenaRoot);
                BuildCubicleDesk(new Vector3(4.9f, 0f, z), -90f, arenaRoot);
            }

            // 6. Water Cooler & Pantry Corner
            BuildWaterCooler(new Vector3(-5.3f, 0f, 0f), arenaRoot);
            BuildLaserCopier(new Vector3(5.3f, 0f, 0f), arenaRoot);

            // 7. Potted Office Plants
            BuildPottedPlant(new Vector3(-5.2f, 0f, -9.5f), arenaRoot);
            BuildPottedPlant(new Vector3(5.2f, 0f, -9.5f), arenaRoot);
            BuildPottedPlant(new Vector3(-5.2f, 0f, 9.5f), arenaRoot);
            BuildPottedPlant(new Vector3(5.2f, 0f, 9.5f), arenaRoot);

            // 8. Player Base - 6 o'clock Elevator Bank
            BuildElevatorBank(new Vector3(0f, 0f, PlayerBaseZ), arenaRoot);

            // 9. Enemy Base - 12 o'clock Executive CEO Desk
            BuildCeoExecutiveSuite(new Vector3(0f, 0f, EnemyBaseZ), arenaRoot);
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

        private void BuildCubicleDesk(Vector3 pos, float rotY, GameObject parent)
        {
            GameObject deskGroup = new GameObject("CubicleDesk");
            deskGroup.transform.position = pos;
            deskGroup.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            deskGroup.transform.SetParent(parent.transform);

            // Desk surface
            CreateCube("DeskTop", pos + new Vector3(0f, 0.72f, 0f), new Vector3(1.4f, 0.08f, 0.8f), matDeskWood, deskGroup);
            // Metal legs
            CreateCube("DeskLegL", pos + new Vector3(-0.6f, 0.36f, 0f), new Vector3(0.08f, 0.72f, 0.7f), matDeskMetal, deskGroup);
            CreateCube("DeskLegR", pos + new Vector3(0.6f, 0.36f, 0f), new Vector3(0.08f, 0.72f, 0.7f), matDeskMetal, deskGroup);
            // Dual Monitor
            CreateCube("Monitor1", pos + new Vector3(-0.25f, 1.05f, 0.2f), new Vector3(0.45f, 0.30f, 0.05f), matScreenBlue, deskGroup);
            CreateCube("Monitor2", pos + new Vector3(0.25f, 1.05f, 0.2f), new Vector3(0.45f, 0.30f, 0.05f), matScreenBlue, deskGroup);
            CreateCube("MonitorStand", pos + new Vector3(0f, 0.85f, 0.2f), new Vector3(0.2f, 0.2f, 0.1f), matDeskMetal, deskGroup);
            // Office Chair
            CreateCube("ChairSeat", pos + new Vector3(0f, 0.45f, -0.4f), new Vector3(0.5f, 0.08f, 0.5f), matChair, deskGroup);
            CreateCube("ChairBack", pos + new Vector3(0f, 0.80f, -0.6f), new Vector3(0.45f, 0.60f, 0.08f), matChair, deskGroup);
        }

        private void BuildWaterCooler(Vector3 pos, GameObject parent)
        {
            GameObject wc = new GameObject("WaterCooler");
            wc.transform.position = pos;
            wc.transform.SetParent(parent.transform);
            CreateCube("DispenserBody", pos + new Vector3(0f, 0.6f, 0f), new Vector3(0.45f, 1.2f, 0.45f), matWaterDispenser, wc);
            CreateCube("WaterBottle", pos + new Vector3(0f, 1.45f, 0f), new Vector3(0.35f, 0.5f, 0.35f), matWaterBottle, wc);
        }

        private void BuildLaserCopier(Vector3 pos, GameObject parent)
        {
            GameObject cp = new GameObject("LaserCopier");
            cp.transform.position = pos;
            cp.transform.SetParent(parent.transform);
            CreateCube("CopierBody", pos + new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 1.1f, 0.65f), matWaterDispenser, cp);
            CreateCube("ScannerGlass", pos + new Vector3(0f, 1.15f, 0f), new Vector3(0.7f, 0.08f, 0.55f), matScreenBlue, cp);
        }

        private void BuildPottedPlant(Vector3 pos, GameObject parent)
        {
            GameObject plant = new GameObject("OfficeFicus");
            plant.transform.position = pos;
            plant.transform.SetParent(parent.transform);
            CreateCube("Pot", pos + new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.6f, 0.5f), matPlantPot, plant);
            CreateCube("Leaves1", pos + new Vector3(0f, 0.85f, 0f), new Vector3(0.6f, 0.6f, 0.6f), matPlantLeaf, plant);
            CreateCube("Leaves2", pos + new Vector3(0f, 1.2f, 0f), new Vector3(0.4f, 0.4f, 0.4f), matPlantLeaf, plant);
        }

        private void BuildElevatorBank(Vector3 pos, GameObject parent)
        {
            GameObject elv = new GameObject("ElevatorBank_PlayerHQ");
            elv.transform.position = pos;
            elv.transform.SetParent(parent.transform);

            // Double Elevator Arch Frame
            CreateCube("ElevatorFrame", pos + new Vector3(0f, 1.6f, -1.2f), new Vector3(3.2f, 3.2f, 0.6f), matElevatorSteel, elv);
            CreateCube("ElevatorDoorL", pos + new Vector3(-0.7f, 1.4f, -0.9f), new Vector3(1.1f, 2.6f, 0.1f), matCarpet, elv);
            CreateCube("ElevatorDoorR", pos + new Vector3(0.7f, 1.4f, -0.9f), new Vector3(1.1f, 2.6f, 0.1f), matCarpet, elv);

            // Green EXIT / 6F Sign
            CreateCube("ExitSign", pos + new Vector3(0f, 3.0f, -0.85f), new Vector3(1.6f, 0.35f, 0.15f), matElevatorLight, elv);

            // Security Turnstiles (Sub-towers)
            CreateCube("TurnstileL", new Vector3(LaneLeftX, 0.5f, pos.z + 0.8f), new Vector3(0.6f, 1.0f, 0.8f), matElevatorSteel, elv);
            CreateCube("TurnstileR", new Vector3(LaneRightX, 0.5f, pos.z + 0.8f), new Vector3(0.6f, 1.0f, 0.8f), matElevatorSteel, elv);
        }

        private void BuildCeoExecutiveSuite(Vector3 pos, GameObject parent)
        {
            GameObject suite = new GameObject("CeoSuite_EnemyHQ");
            suite.transform.position = pos;
            suite.transform.SetParent(parent.transform);

            // Executive Dark Mahogany Desk
            CreateCube("CeoDesk", pos + new Vector3(0f, 0.75f, 1.0f), new Vector3(3.4f, 0.12f, 1.2f), matCeoMahogany, suite);
            CreateCube("CeoDeskTrim", pos + new Vector3(0f, 0.78f, 1.0f), new Vector3(3.45f, 0.04f, 1.25f), matGoldTrim, suite);
            CreateCube("CeoDeskLegL", pos + new Vector3(-1.5f, 0.36f, 1.0f), new Vector3(0.2f, 0.72f, 1.1f), matCeoMahogany, suite);
            CreateCube("CeoDeskLegR", pos + new Vector3(1.5f, 0.36f, 1.0f), new Vector3(0.2f, 0.72f, 1.1f), matCeoMahogany, suite);

            // Stacks of Overtime Reports
            CreateCube("Papers1", pos + new Vector3(-0.9f, 0.95f, 1.0f), new Vector3(0.4f, 0.35f, 0.3f), matPaperStack, suite);
            CreateCube("Papers2", pos + new Vector3(0.9f, 1.05f, 1.0f), new Vector3(0.4f, 0.55f, 0.3f), matPaperStack, suite);

            // Executive Leather Chair
            CreateCube("CeoChairSeat", pos + new Vector3(0f, 0.55f, 1.9f), new Vector3(0.8f, 0.12f, 0.8f), matCeoMahogany, suite);
            CreateCube("CeoChairBack", pos + new Vector3(0f, 1.30f, 2.2f), new Vector3(0.8f, 1.4f, 0.15f), matCeoMahogany, suite);

            // Executive Office Sub-towers (부사장실 / 전무실)
            CreateCube("ExecRoomL", new Vector3(LaneLeftX, 0.6f, pos.z - 0.8f), new Vector3(0.8f, 1.2f, 0.8f), matWall, suite);
            CreateCube("ExecRoomR", new Vector3(LaneRightX, 0.6f, pos.z - 0.8f), new Vector3(0.8f, 1.2f, 0.8f), matWall, suite);
        }

        public void SetScreen(GameScreen next)
        {
            currentScreen = next;
            if (next == GameScreen.Battle)
            {
                // Reset camera to standard battle view
                mainCam.transform.position = new Vector3(0f, 17.5f, -12.5f);
                mainCam.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
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

            playerMainTower = new TowerData { hp = 1500, maxHp = 1500, pos = new Vector3(0f, 0f, PlayerBaseZ), name = "칼퇴 엘리베이터" };
            playerSubLeft   = new TowerData { hp = 750,  maxHp = 750,  pos = new Vector3(LaneLeftX, 0f, PlayerBaseZ + 1f), name = "안내게이트(좌)" };
            playerSubRight  = new TowerData { hp = 750,  maxHp = 750,  pos = new Vector3(LaneRightX, 0f, PlayerBaseZ + 1f), name = "안내게이트(우)" };

            enemyMainTower  = new TowerData { hp = 1500, maxHp = 1500, pos = new Vector3(0f, 0f, EnemyBaseZ), name = "대표이사 야근본사" };
            enemySubLeft   = new TowerData { hp = 750,  maxHp = 750,  pos = new Vector3(LaneLeftX, 0f, EnemyBaseZ - 1f), name = "부사장실(좌)" };
            enemySubRight  = new TowerData { hp = 750,  maxHp = 750,  pos = new Vector3(LaneRightX, 0f, EnemyBaseZ - 1f), name = "전무실(우)" };

            caffeine = 4.5f;
            matchTimer = 180.0f;
            isGameOver = false;
            isVictory = false;
            enemySpawnTimer = 3.0f;
            coffeeCooldown = 0f;
            laserCooldown = 0f;

            if (playSound) PlaySfx(SfxType.Summon);
        }

        private void Update()
        {
            // Title Camera Motion
            if (currentScreen == GameScreen.Title)
            {
                titleCamAngle += Time.deltaTime * 12f;
                float rad = titleCamAngle * Mathf.Deg2Rad;
                float camX = Mathf.Sin(rad) * 6.5f;
                float camZ = Mathf.Cos(rad) * 6.5f - 2f;
                mainCam.transform.position = new Vector3(camX, 12f, camZ);
                mainCam.transform.LookAt(new Vector3(0f, 1f, 0f));

                UpdateProceduralBgm();
                return;
            }

            if (currentScreen != GameScreen.Battle) return;

            UpdateProceduralBgm();

            if (isGameOver) return;

            // 1. Caffeine Recovery (1.0 / sec)
            caffeine = Mathf.Min(MaxCaffeine, caffeine + Time.deltaTime * 1.0f);

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

            // 4. Enemy AI Spawn Waves
            enemySpawnTimer -= Time.deltaTime;
            if (enemySpawnTimer <= 0f)
            {
                SpawnEnemyWave();
                enemySpawnTimer = UnityEngine.Random.Range(4.5f, 7.0f);
            }

            // 5. Update Units
            UpdateUnitList(playerUnits, true);
            UpdateUnitList(enemyUnits, false);

            // 6. Check Tower Destruction
            if (enemyMainTower.hp <= 0) EndMatch(true);
            else if (playerMainTower.hp <= 0) EndMatch(false);

            // 7. Update Floating Text
            for (int i = floatingTexts.Count - 1; i >= 0; i--)
            {
                floatingTexts[i].life -= Time.deltaTime;
                floatingTexts[i].worldPos += Vector3.up * Time.deltaTime * 1.5f;
                if (floatingTexts[i].life <= 0f) floatingTexts.RemoveAt(i);
            }
        }

        private void EndMatch(bool victory)
        {
            isGameOver = true;
            isVictory = victory;
            SetScreen(victory ? GameScreen.Victory : GameScreen.Defeat);
            PlaySfx(victory ? SfxType.Victory : SfxType.Defeat);
        }

        private void SpawnEnemyWave()
        {
            int lane = UnityEngine.Random.Range(0, 3);
            float roll = UnityEngine.Random.value;

            if (roll < 0.50f)
            {
                // 야근 요정 (Swarm)
                SpawnEnemyUnit(UnitType.EnemyGoblin, lane);
            }
            else if (roll < 0.85f)
            {
                // 꼰대 이사 (Elite)
                SpawnEnemyUnit(UnitType.EnemyDirector, lane);
            }
            else
            {
                // 주말특근 악마 (Boss)
                SpawnEnemyUnit(UnitType.EnemyBoss, lane);
            }
        }

        // --- Character Spawning with Detailed 3D Humanoid Meshes ---
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
            Vector3 spawnPos = new Vector3(x, 0.05f, PlayerBaseZ + 2.0f);

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
                attackRange = 1.4f,
                attackCooldown = 1.0f
            };
            playerUnits.Add(u);
        }

        private void SpawnEnemyUnit(UnitType type, int lane)
        {
            float x = GetLaneX(lane);
            Vector3 spawnPos = new Vector3(x, 0.05f, EnemyBaseZ - 2.0f);

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
                attackRange = 1.4f,
                attackCooldown = 1.0f
            };
            enemyUnits.Add(u);
        }

        private GameObject Build3DCharacterObject(UnitType type, Vector3 pos, bool isPlayer)
        {
            GameObject charGo = new GameObject(type.ToString());
            charGo.transform.position = pos;
            float rotY = isPlayer ? 0f : 180f;
            charGo.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            // 1. Torso
            Material torsoMat = matInternHoodie;
            if (type == UnitType.AssistantManager) torsoMat = matDaeriShirt;
            else if (type == UnitType.SeniorManager) torsoMat = matGwajangVest;
            else if (type == UnitType.EnemyGoblin) torsoMat = matEnemyGoblin;
            else if (type == UnitType.EnemyDirector) torsoMat = matEnemyDirector;
            else if (type == UnitType.EnemyBoss) torsoMat = matEnemyBoss;

            Vector3 torsoScale = (type == UnitType.SeniorManager || type == UnitType.EnemyBoss) ? new Vector3(0.65f, 0.7f, 0.45f) : new Vector3(0.5f, 0.65f, 0.35f);
            CreateCube("Torso", pos + new Vector3(0f, 0.75f, 0f), torsoScale, torsoMat, charGo);

            // 2. Head
            Material headMat = (type == UnitType.EnemyGoblin || type == UnitType.EnemyBoss) ? torsoMat : matSkin;
            CreateCube("Head", pos + new Vector3(0f, 1.25f, 0f), new Vector3(0.38f, 0.38f, 0.38f), headMat, charGo);

            // 3. Legs
            Material legMat = (type == UnitType.Intern) ? matInternHoodie : matDaeriSlacks;
            CreateCube("LegL", pos + new Vector3(-0.16f, 0.25f, 0f), new Vector3(0.18f, 0.5f, 0.22f), legMat, charGo);
            CreateCube("LegR", pos + new Vector3(0.16f, 0.25f, 0f), new Vector3(0.18f, 0.5f, 0.22f), legMat, charGo);

            // 4. Props / Weapons
            if (type == UnitType.Intern)
            {
                // Backpack
                CreateCube("Backpack", pos + new Vector3(0f, 0.75f, -0.22f), new Vector3(0.42f, 0.5f, 0.2f), matDeskWood, charGo);
                // Energy Drink
                CreateCube("EnergyDrink", pos + new Vector3(0.32f, 0.75f, 0.2f), new Vector3(0.12f, 0.25f, 0.12f), matElevatorLight, charGo);
            }
            else if (type == UnitType.AssistantManager)
            {
                // Briefcase
                CreateCube("Briefcase", pos + new Vector3(0.38f, 0.5f, 0.1f), new Vector3(0.12f, 0.35f, 0.45f), matWall, charGo);
                // Tie
                CreateCube("Necktie", pos + new Vector3(0f, 0.75f, 0.19f), new Vector3(0.1f, 0.35f, 0.04f), matDaeriTie, charGo);
            }
            else if (type == UnitType.SeniorManager)
            {
                // Giant Office Partition Shield
                CreateCube("PartitionShield", pos + new Vector3(0f, 0.75f, 0.3f), new Vector3(1.1f, 1.2f, 0.12f), matGwajangShield, charGo);
            }
            else if (type == UnitType.EnemyDirector)
            {
                // Red Rejection Stamp Hammer
                CreateCube("RejectStampHandle", pos + new Vector3(0.38f, 0.85f, 0.2f), new Vector3(0.08f, 0.6f, 0.08f), matDeskWood, charGo);
                CreateCube("RejectStampHead", pos + new Vector3(0.38f, 1.15f, 0.2f), new Vector3(0.35f, 0.2f, 0.35f), matDaeriTie, charGo);
            }
            else if (type == UnitType.EnemyBoss)
            {
                // Horns & Giant Scale
                charGo.transform.localScale = Vector3.one * 1.35f;
                CreateCube("HornL", pos + new Vector3(-0.2f, 1.6f, 0f), new Vector3(0.1f, 0.35f, 0.1f), matGoldTrim, charGo);
                CreateCube("HornR", pos + new Vector3(0.2f, 1.6f, 0f), new Vector3(0.1f, 0.35f, 0.1f), matGoldTrim, charGo);
            }

            return charGo;
        }

        private void ExecuteOvertimeLaser(int lane)
        {
            PlaySfx(SfxType.Laser);
            float x = GetLaneX(lane);

            // Visual laser column
            GameObject laser = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            laser.name = "OvertimeLaserBeam";
            laser.transform.position = new Vector3(x, 5f, 0f);
            laser.transform.localScale = new Vector3(2.2f, 10f, 2.2f);
            laser.GetComponent<Renderer>().material = matElevatorLight;
            Destroy(laser, 0.45f);

            // Damage all enemy units in lane
            for (int i = enemyUnits.Count - 1; i >= 0; i--)
            {
                if (enemyUnits[i].lane == lane)
                {
                    enemyUnits[i].hp -= 250;
                    SpawnDamageText(enemyUnits[i].go.transform.position, "-250 (칼퇴빔!)", Color.yellow);
                    if (enemyUnits[i].hp <= 0)
                    {
                        Destroy(enemyUnits[i].go);
                        enemyUnits.RemoveAt(i);
                    }
                }
            }
        }

        private void ExecuteMixCoffee()
        {
            PlaySfx(SfxType.Coffee);
            // Speed and attack boost to all player units
            foreach (var u in playerUnits)
            {
                u.speed *= 1.35f;
                u.damage = (int)(u.damage * 1.35f);
                SpawnDamageText(u.go.transform.position, "카페인 각성! (+35%)", Color.cyan);
            }
        }

        private void UpdateUnitList(List<BattleUnit> units, bool isPlayer)
        {
            float direction = isPlayer ? 1.0f : -1.0f;
            var opposingUnits = isPlayer ? enemyUnits : playerUnits;

            for (int i = units.Count - 1; i >= 0; i--)
            {
                BattleUnit u = units[i];
                if (u.go == null) { units.RemoveAt(i); continue; }

                // Check closest target in lane
                BattleUnit targetUnit = GetClosestOpponentInLane(u, opposingUnits);
                TowerData targetTower = GetTargetTower(u, !isPlayer);

                bool canAttackUnit = targetUnit != null && Vector3.Distance(u.go.transform.position, targetUnit.go.transform.position) <= u.attackRange;
                bool canAttackTower = targetTower != null && targetTower.hp > 0 && Mathf.Abs(u.go.transform.position.z - targetTower.pos.z) <= u.attackRange + 0.8f;

                if (canAttackUnit)
                {
                    // Attack Unit
                    u.currentCooldown -= Time.deltaTime;
                    if (u.currentCooldown <= 0f)
                    {
                        u.currentCooldown = u.attackCooldown;
                        targetUnit.hp -= u.damage;
                        PlaySfx(SfxType.Hit);
                        SpawnDamageText(targetUnit.go.transform.position, "-" + u.damage, isPlayer ? Color.yellow : Color.red);
                        if (targetUnit.hp <= 0)
                        {
                            Destroy(targetUnit.go);
                            opposingUnits.Remove(targetUnit);
                        }
                    }
                }
                else if (canAttackTower)
                {
                    // Attack Tower
                    u.currentCooldown -= Time.deltaTime;
                    if (u.currentCooldown <= 0f)
                    {
                        u.currentCooldown = u.attackCooldown;
                        targetTower.hp -= u.damage;
                        PlaySfx(SfxType.Hit);
                        SpawnDamageText(targetTower.pos + Vector3.up * 1.5f, "-" + u.damage, isPlayer ? Color.yellow : Color.red);
                    }
                }
                else
                {
                    // Advance Forward along corridor lane
                    u.go.transform.position += Vector3.forward * direction * u.speed * Time.deltaTime;
                }
            }
        }

        private BattleUnit GetClosestOpponentInLane(BattleUnit me, List<BattleUnit> opponents)
        {
            BattleUnit best = null;
            float bestDist = float.MaxValue;
            foreach (var op in opponents)
            {
                if (op.lane != me.lane || op.go == null) continue;
                float d = Vector3.Distance(me.go.transform.position, op.go.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = op;
                }
            }
            return best;
        }

        private TowerData GetTargetTower(BattleUnit u, bool targetPlayerTowers)
        {
            if (targetPlayerTowers)
            {
                if (u.lane == 0 && playerSubLeft.hp > 0) return playerSubLeft;
                if (u.lane == 2 && playerSubRight.hp > 0) return playerSubRight;
                return playerMainTower;
            }
            else
            {
                if (u.lane == 0 && enemySubLeft.hp > 0) return enemySubLeft;
                if (u.lane == 2 && enemySubRight.hp > 0) return enemySubRight;
                return enemyMainTower;
            }
        }

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
                case UnitType.OvertimeLaser: return 4.0f;
                case UnitType.MixCoffee: return 1.0f;
                default: return 2.0f;
            }
        }

        private int GetUnitMaxHp(UnitType t)
        {
            switch (t)
            {
                case UnitType.Intern: return 220;
                case UnitType.AssistantManager: return 450;
                case UnitType.SeniorManager: return 900;
                case UnitType.EnemyGoblin: return 160;
                case UnitType.EnemyDirector: return 600;
                case UnitType.EnemyBoss: return 1200;
                default: return 200;
            }
        }

        private int GetUnitDamage(UnitType t)
        {
            switch (t)
            {
                case UnitType.Intern: return 45;
                case UnitType.AssistantManager: return 75;
                case UnitType.SeniorManager: return 40;
                case UnitType.EnemyGoblin: return 30;
                case UnitType.EnemyDirector: return 85;
                case UnitType.EnemyBoss: return 120;
                default: return 30;
            }
        }

        private float GetUnitSpeed(UnitType t)
        {
            switch (t)
            {
                case UnitType.Intern: return 2.8f;
                case UnitType.AssistantManager: return 2.0f;
                case UnitType.SeniorManager: return 1.3f;
                case UnitType.EnemyGoblin: return 3.0f;
                case UnitType.EnemyDirector: return 1.7f;
                case UnitType.EnemyBoss: return 1.1f;
                default: return 2.0f;
            }
        }

        private void SpawnDamageText(Vector3 pos, string text, Color col)
        {
            floatingTexts.Add(new FloatingText { worldPos = pos, text = text, color = col, life = 1.0f });
        }

        // --- High-Polish Procedural 8-Bit Audio ---
        public enum SfxType { Summon, Hit, Laser, Coffee, Victory, Defeat }

        private void PlaySfx(SfxType type)
        {
            if (!sfxEnabled || audioSource == null) return;
            AudioClip clip = GenerateProceduralClip(type);
            if (clip != null) audioSource.PlayOneShot(clip, 0.75f);
        }

        private AudioClip GenerateProceduralClip(SfxType type)
        {
            int sampleRate = 22050;
            float duration = 0.2f;
            if (type == SfxType.Victory || type == SfxType.Defeat) duration = 1.0f;
            if (type == SfxType.Laser) duration = 0.45f;

            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float val = 0f;

                switch (type)
                {
                    case SfxType.Summon:
                        float f1 = Mathf.Lerp(300f, 650f, t / duration);
                        val = Mathf.Sin(2f * Mathf.PI * f1 * t) * (1f - t / duration);
                        break;
                    case SfxType.Hit:
                        float noise = (UnityEngine.Random.value * 2f - 1f) * 0.5f;
                        float fHit = Mathf.Lerp(180f, 60f, t / duration);
                        val = (Mathf.Sin(2f * Mathf.PI * fHit * t) + noise) * (1f - t / duration);
                        break;
                    case SfxType.Laser:
                        float fLaser = Mathf.Lerp(900f, 150f, t / duration);
                        val = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * fLaser * t)) * (1f - t / duration) * 0.6f;
                        break;
                    case SfxType.Coffee:
                        float fCof = Mathf.Lerp(500f, 950f, t / duration);
                        val = Mathf.Sin(2f * Mathf.PI * fCof * t) * (1f - t / duration);
                        break;
                    case SfxType.Victory:
                        float fVic = (t < 0.25f) ? 523.25f : (t < 0.5f) ? 659.25f : 783.99f;
                        val = Mathf.Sin(2f * Mathf.PI * fVic * t) * (1f - t / duration);
                        break;
                    case SfxType.Defeat:
                        float fDef = Mathf.Lerp(440f, 110f, t / duration);
                        val = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * fDef * t)) * (1f - t / duration) * 0.5f;
                        break;
                }
                samples[i] = Mathf.Clamp(val, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_" + type, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void UpdateProceduralBgm()
        {
            if (!bgmEnabled || audioSource == null) return;
            bgmLoopTimer -= Time.deltaTime;
            if (bgmLoopTimer <= 0f)
            {
                bgmLoopTimer = 0.28f;
                float[] notes = { 261.63f, 293.66f, 329.63f, 392.00f, 440.00f, 392.00f, 329.63f, 293.66f };
                float freq = notes[currentBgmNote % notes.Length];
                currentBgmNote++;

                AudioClip noteClip = GenerateTone(freq, 0.22f, 0.12f);
                audioSource.PlayOneShot(noteClip, 0.25f);
            }
        }

        private AudioClip GenerateTone(float freq, float duration, float vol)
        {
            int rate = 22050;
            int count = (int)(rate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * vol * (1f - t / duration);
            }
            AudioClip c = AudioClip.Create("BGM_Tone", count, 1, rate, false);
            c.SetData(samples, 0);
            return c;
        }

        // --- High-End Mobile UI (No Tofu Squares, Clean MVP Design) ---
        private void OnGUI()
        {
            // Responsive GUI scaling (750x1334 High-DPI Virtual Canvas)
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
        }

        private void DrawTitleScreen()
        {
            // Dark glass header banner
            GUI.DrawTexture(new Rect(40, 120, 670, 260), texPanelHeader);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 46,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(50, 140, 650, 60), "칼퇴 럼블", titleStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            subStyle.normal.textColor = new Color(0.35f, 0.75f, 1.0f);
            GUI.Label(new Rect(50, 205, 650, 30), "OFFICE RUMBLE: THE OVERTIME BATTLE", subStyle);

            GUIStyle editionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            editionStyle.normal.textColor = new Color(0.75f, 0.80f, 0.88f);
            GUI.Label(new Rect(50, 245, 650, 30), "Synty 3D Low-Poly Edition • MVP v0.2.0", editionStyle);

            GUI.Label(new Rect(50, 300, 650, 40), "정시 퇴근을 위해 대표이사의 야근 본사를 정복하라!", editionStyle);

            // Menu Buttons
            GUIStyle btnMainStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            btnMainStyle.normal.background = texBtnSpecial;
            btnMainStyle.normal.textColor = Color.white;

            if (GUI.Button(new Rect(95, 760, 560, 85), "[ 게임 시작 (출근 및 야근 저지) ]", btnMainStyle))
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

            if (GUI.Button(new Rect(95, 875, 560, 65), "[ 사원 카드 도감 (유닛 정보) ]", btnSubStyle))
            {
                SetScreen(GameScreen.Codex);
                PlaySfx(SfxType.Hit);
            }

            if (GUI.Button(new Rect(95, 960, 560, 65), "[ 게임 환경 설정 ]", btnSubStyle))
            {
                SetScreen(GameScreen.Settings);
                PlaySfx(SfxType.Hit);
            }
        }

        private void DrawBattleHUD()
        {
            // 1. Top Status Bar
            GUI.DrawTexture(new Rect(20, 25, 710, 110), texPanelDark);

            // Enemy HQ HP
            float enemyHpPct = Mathf.Clamp01((float)enemyMainTower.hp / enemyMainTower.maxHp);
            GUI.DrawTexture(new Rect(35, 45, 230, 26), texBarBg);
            GUI.DrawTexture(new Rect(35, 45, 230 * enemyHpPct, 26), texHpRed);

            GUIStyle textSmall = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            textSmall.normal.textColor = Color.white;
            GUI.Label(new Rect(40, 48, 220, 22), string.Format("[야근본사] {0}/{1}", enemyMainTower.hp, enemyMainTower.maxHp), textSmall);

            // Center Match Timer
            int min = (int)(matchTimer / 60);
            int sec = (int)(matchTimer % 60);
            GUIStyle timerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            timerStyle.normal.textColor = (matchTimer <= 30f) ? Color.red : Color.yellow;
            GUI.Label(new Rect(280, 40, 190, 40), string.Format("{0:00}:{1:00}", min, sec), timerStyle);

            // Player Elevator HP
            float playerHpPct = Mathf.Clamp01((float)playerMainTower.hp / playerMainTower.maxHp);
            GUI.DrawTexture(new Rect(485, 45, 230, 26), texBarBg);
            GUI.DrawTexture(new Rect(485, 45, 230 * playerHpPct, 26), texHpGreen);
            GUI.Label(new Rect(490, 48, 220, 22), string.Format("[칼퇴엘베] {0}/{1}", playerMainTower.hp, playerMainTower.maxHp), textSmall);

            // Sub Towers Status
            GUIStyle subInfoStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            subInfoStyle.normal.textColor = new Color(0.75f, 0.80f, 0.88f);
            GUI.Label(new Rect(35, 80, 230, 20), string.Format("부사장실:{0} | 전무실:{1}", enemySubLeft.hp, enemySubRight.hp), subInfoStyle);
            GUI.Label(new Rect(485, 80, 230, 20), string.Format("좌게이트:{0} | 우게이트:{1}", playerSubLeft.hp, playerSubRight.hp), subInfoStyle);

            // 2. Lane Selector (Left / Center / Right)
            GUI.DrawTexture(new Rect(20, 960, 710, 65), texPanelDark);
            GUIStyle laneBtn = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            for (int i = 0; i < 3; i++)
            {
                string label = (i == 0) ? "[ 1번 복도 (좌) ]" : (i == 1) ? "[ 2번 복도 (중앙) ]" : "[ 3번 복도 (우) ]";
                laneBtn.normal.background = (selectedLane == i) ? texBtnActive : texBtnNormal;
                laneBtn.normal.textColor = (selectedLane == i) ? Color.white : new Color(0.70f, 0.75f, 0.85f);

                if (GUI.Button(new Rect(30 + i * 235, 968, 225, 48), label, laneBtn))
                {
                    selectedLane = i;
                    PlaySfx(SfxType.Hit);
                }
            }

            // 3. Caffeine Bar
            GUI.DrawTexture(new Rect(20, 1035, 710, 36), texBarBg);
            float cafPct = Mathf.Clamp01(caffeine / MaxCaffeine);
            GUI.DrawTexture(new Rect(22, 1037, 706 * cafPct, 32), texCaffeineFill);

            GUIStyle cafStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            cafStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(20, 1040, 710, 30), string.Format("카페인 게이지: {0:F1} / {1:F1}", caffeine, MaxCaffeine), cafStyle);

            // 4. 5 Unit Card Deck (Bottom Row)
            DrawUnitCard(new Rect(20, 1080, 136, 175), UnitType.Intern, "인턴 사원", 2, "속도 특화", caffeine >= 2f);
            DrawUnitCard(new Rect(164, 1080, 136, 175), UnitType.AssistantManager, "칼퇴 대리", 3, "근접 딜러", caffeine >= 3f);
            DrawUnitCard(new Rect(308, 1080, 136, 175), UnitType.SeniorManager, "철벽 과장", 4, "방패 탱커", caffeine >= 4f);
            DrawUnitCard(new Rect(452, 1080, 136, 175), UnitType.OvertimeLaser, "칼퇴 빔", 4, (laserCooldown > 0f) ? string.Format("{0:F0}s", laserCooldown) : "라인 폭격", caffeine >= 4f && laserCooldown <= 0f);
            DrawUnitCard(new Rect(596, 1080, 136, 175), UnitType.MixCoffee, "믹스 커피", 1, (coffeeCooldown > 0f) ? string.Format("{0:F0}s", coffeeCooldown) : "전체 버프", caffeine >= 1f && coffeeCooldown <= 0f);

            // 5. Floating Damage Text
            foreach (var ft in floatingTexts)
            {
                Vector3 sp = mainCam.WorldToScreenPoint(ft.worldPos);
                if (sp.z > 0f)
                {
                    float gx = sp.x / (Screen.width / 750f);
                    float gy = (Screen.height - sp.y) / (Screen.height / 1334f);
                    GUIStyle dmgStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                    dmgStyle.normal.textColor = ft.color;
                    GUI.Label(new Rect(gx - 80, gy - 15, 160, 30), ft.text, dmgStyle);
                }
            }
        }

        private void DrawUnitCard(Rect rect, UnitType type, string name, int cost, string sub, bool ready)
        {
            GUI.DrawTexture(rect, ready ? texCardActive : texCardBg);

            GUIStyle costStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            costStyle.normal.textColor = ready ? Color.yellow : Color.gray;
            GUI.Label(new Rect(rect.x + 8, rect.y + 6, 40, 26), cost.ToString(), costStyle);

            GUIStyle nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            nameStyle.normal.textColor = ready ? Color.white : new Color(0.65f, 0.65f, 0.70f);
            GUI.Label(new Rect(rect.x, rect.y + 40, rect.width, 30), name, nameStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            subStyle.normal.textColor = ready ? new Color(0.40f, 0.85f, 1.0f) : Color.gray;
            GUI.Label(new Rect(rect.x, rect.y + 75, rect.width, 24), sub, subStyle);

            // Summon Touch Button
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            btnStyle.normal.background = ready ? texBtnActive : texBtnNormal;
            btnStyle.normal.textColor = ready ? Color.white : Color.gray;

            if (GUI.Button(new Rect(rect.x + 8, rect.y + 115, rect.width - 16, 45), ready ? "출격!" : "대기", btnStyle))
            {
                TrySpawnPlayerUnit(type);
            }
        }

        private void DrawEndPopup(bool victory)
        {
            GUI.DrawTexture(new Rect(0, 0, 750, 1334), MakeSolidTexture(2, 2, new Color(0f, 0f, 0f, 0.65f)));
            GUI.DrawTexture(new Rect(65, 380, 620, 480), texPanelHeader);

            GUIStyle bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            bigStyle.normal.textColor = victory ? new Color(1.0f, 0.85f, 0.20f) : new Color(1.0f, 0.35f, 0.35f);
            GUI.Label(new Rect(75, 420, 600, 60), victory ? "칼퇴 성공! (승리)" : "야근 확정... (패배)", bigStyle);

            GUIStyle descStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleCenter };
            descStyle.normal.textColor = Color.white;
            string desc = victory ? "대표이사를 물리치고 정시 칼퇴 엘리베이터에 탑승했습니다!\n오늘 야근에서 완벽히 탈출했습니다." : "엘리베이터가 봉쇄되었습니다.\n야근 수당 없는 밤샘 잔업에 돌입합니다...";
            GUI.Label(new Rect(85, 500, 580, 80), desc, descStyle);

            GUIStyle btnPopup = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            btnPopup.normal.background = victory ? texBtnSpecial : texBtnNormal;
            btnPopup.normal.textColor = Color.white;

            if (GUI.Button(new Rect(115, 630, 520, 75), victory ? "[ 다음 층 사옥 정복하기 ]" : "[ 칼퇴 다시 도전하기 ]", btnPopup))
            {
                StartMatch();
            }

            if (GUI.Button(new Rect(115, 730, 520, 65), "[ 메인 메뉴로 ]", btnPopup))
            {
                SetScreen(GameScreen.Title);
                PlaySfx(SfxType.Hit);
            }
        }

        private void DrawCodexScreen()
        {
            GUI.DrawTexture(new Rect(30, 60, 690, 1180), texPanelHeader);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(40, 85, 670, 50), "사원 카드 도감 (유닛 도감)", titleStyle);

            string[] codexCards = {
                "[인턴 사원 (2 CAFE)]\n• 역할: 고속 침투 및 정찰\n• 특징: 가벼운 백팩과 에너지 드링크로 빠른 발걸음. 대표이사 타워를 향해 맹렬히 질주!",
                "[칼퇴 대리 (3 CAFE)]\n• 역할: 근접 만능 전사\n• 특징: 칼같은 서류가방 스윙! 균형 잡힌 공수 능력으로 복도 교전의 척추 역할.",
                "[철벽 과장 (4 CAFE)]\n• 역할: 든든한 방패 탱커\n• 특징: 거대한 반려 파티션 방패 장착. 후방 사원들을 야근 공격으로부터 완벽 보호!",
                "[칼퇴 빔 (4 CAFE - 마법)]\n• 역할: 단일 라인 초토화\n• 특징: 천장 칼퇴 조명이 레이저로 변해 복도 전체의 야근 유닛을 일거에 소탕!",
                "[믹스 커피 (1 CAFE - 마법)]\n• 역할: 전 아군 공속/이속 35% 증폭\n• 특징: 황금빛 맥심 커피의 마력으로 전장에 있는 모든 아군에게 카페인 폭풍 각성 부여!"
            };

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft, wordWrap = true, padding = new RectOffset(16, 16, 14, 14) };
            boxStyle.normal.background = texCardBg;
            boxStyle.normal.textColor = new Color(0.90f, 0.94f, 1.0f);

            for (int i = 0; i < codexCards.Length; i++)
            {
                GUI.Box(new Rect(60, 160 + i * 180, 630, 165), codexCards[i], boxStyle);
            }

            GUIStyle btnClose = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            btnClose.normal.background = texBtnSpecial;
            btnClose.normal.textColor = Color.white;

            if (GUI.Button(new Rect(100, 1120, 550, 75), "[ 도감 닫기 (메인으로) ]", btnClose))
            {
                SetScreen(GameScreen.Title);
                PlaySfx(SfxType.Hit);
            }
        }

        private void DrawSettingsScreen()
        {
            GUI.DrawTexture(new Rect(60, 260, 630, 720), texPanelHeader);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(70, 300, 610, 50), "게임 환경 설정", titleStyle);

            GUIStyle btnToggle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            btnToggle.normal.background = texBtnNormal;
            btnToggle.normal.textColor = Color.white;

            if (GUI.Button(new Rect(100, 420, 550, 70), string.Format("배경음악 (BGM): {0}", bgmEnabled ? "ON (켜짐)" : "OFF (꺼짐)"), btnToggle))
            {
                bgmEnabled = !bgmEnabled;
                PlaySfx(SfxType.Hit);
            }

            if (GUI.Button(new Rect(100, 520, 550, 70), string.Format("효과음 (SFX): {0}", sfxEnabled ? "ON (켜짐)" : "OFF (꺼짐)"), btnToggle))
            {
                sfxEnabled = !sfxEnabled;
                PlaySfx(SfxType.Hit);
            }

            if (GUI.Button(new Rect(100, 620, 550, 70), string.Format("프레임 레이트: {0}", highFpsMode ? "60 FPS (고성능)" : "30 FPS (절전)"), btnToggle))
            {
                highFpsMode = !highFpsMode;
                Application.targetFrameRate = highFpsMode ? 60 : 30;
                PlaySfx(SfxType.Hit);
            }

            GUIStyle btnClose = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            btnClose.normal.background = texBtnSpecial;
            btnClose.normal.textColor = Color.white;

            if (GUI.Button(new Rect(100, 840, 550, 75), "[ 설정 저장 및 닫기 ]", btnClose))
            {
                SetScreen(GameScreen.Title);
                PlaySfx(SfxType.Hit);
            }
        }
    }

    // --- Data Structures ---
    public enum UnitType
    {
        Intern,
        AssistantManager,
        SeniorManager,
        OvertimeLaser,
        MixCoffee,
        EnemyGoblin,
        EnemyDirector,
        EnemyBoss
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
    }

    public class TowerData
    {
        public string name;
        public int hp;
        public int maxHp;
        public Vector3 pos;
    }

    public class FloatingText
    {
        public Vector3 worldPos;
        public string text;
        public Color color;
        public float life;
    }
}
