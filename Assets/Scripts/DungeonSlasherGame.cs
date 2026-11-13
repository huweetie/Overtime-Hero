using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlasher
{
    public enum GameScreen
    {
        Title,
        Playing,
        LevelUp,
        Codex,
        Settings,
        Victory,
        Defeat
    }

    public enum HeroClass
    {
        Knight,
        Archer,
        Mage,
        Priest
    }

    public enum EnemyKind
    {
        Slime,
        Goblin,
        Skeleton,
        Orc,
        Boss
    }

    public enum PropKind
    {
        Cage,
        Chest,
        Crate
    }

    public enum PickupKind
    {
        Gem,
        Coin,
        Heart
    }

    public enum UpgradeId
    {
        Whirlwind,
        ChainFire,
        Thunder,
        Haste,
        SquadPower,
        Vampiric
    }

    public enum SfxId
    {
        Slash,
        Bow,
        Boom,
        Heal,
        Pickup,
        Rescue,
        LevelUp,
        Hurt,
        Victory
    }

    [Serializable]
    public class HeroMember
    {
        public string name;
        public HeroClass hClass;
        public GameObject root;
        public Renderer spriteRend;
        public Vector3 pos;
        public float hp;
        public float maxHp;
        public float attackDamage;
        public float attackRange;
        public float attackCooldown;
        public float cooldownTimer;
        public float hitFlash;
        public float animPhase;
        public bool facingRight = true;
    }

    [Serializable]
    public class EnemyUnit
    {
        public EnemyKind kind;
        public string name;
        public GameObject root;
        public Renderer spriteRend;
        public Vector3 pos;
        public float hp;
        public float maxHp;
        public float speed;
        public float damage;
        public float attackRange;
        public float attackCooldown;
        public float cooldownTimer;
        public float hitFlash;
        public float radius;
        public float specialTimer;
        public bool facingRight = false;
    }

    [Serializable]
    public class DungeonProp
    {
        public PropKind kind;
        public HeroClass captiveClass;
        public GameObject root;
        public Renderer spriteRend;
        public Renderer captiveRend;
        public Vector3 pos;
        public float hp;
        public float maxHp;
        public float hitFlash;
        public float radius;
    }

    [Serializable]
    public class Projectile
    {
        public bool isPlayer;
        public bool isFireball;
        public GameObject root;
        public Vector3 pos;
        public Vector3 vel;
        public float damage;
        public float aoeRadius;
        public float life;
    }

    [Serializable]
    public class PickupItem
    {
        public PickupKind kind;
        public GameObject root;
        public Vector3 pos;
        public float value;
        public float bobOffset;
    }

    [Serializable]
    public class DangerTelegraph
    {
        public GameObject root;
        public Vector3 pos;
        public float radius;
        public float timer;
        public float maxTime;
        public float damage;
    }

    [Serializable]
    public class SlashEffect
    {
        public GameObject root;
        public float timer;
        public float maxTime;
        public Vector3 startScale;
    }

    [Serializable]
    public class FloatingText
    {
        public Vector3 worldPos;
        public string text;
        public Color color;
        public float timer;
        public float maxTime;
        public bool big;
    }

    [Serializable]
    public class UpgradeOffer
    {
        public UpgradeId id;
        public string title;
        public string badge;
        public string desc;
        public Color accent;
    }

    public class DungeonSlasherGame : MonoBehaviour
    {
        private static DungeonSlasherGame instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            GameObject go = new GameObject("DungeonSlasherManager");
            instance = go.AddComponent<DungeonSlasherGame>();
            DontDestroyOnLoad(go);
        }

        // Core State
        public GameScreen screen = GameScreen.Title;
        public int currentFloor = 1;
        public int waveIndex = 1;
        public float runTimer = 0f;
        public float spawnTimer = 0f;
        public bool bossSpawned = false;
        public bool bossDefeated = false;

        // Player Meta & Progression
        public bool autoMode = true;
        public int squadLevel = 1;
        public float squadXp = 0f;
        public float nextXp = 30f;
        public int runGold = 0;
        public int totalGold = 0;
        public int bestFloor = 1;
        public int kills = 0;
        public float ultCharge = 0f; // 0..100
        public float hasteBuffTimer = 0f;

        // Upgrade Ranks
        public int rankWhirlwind = 0;
        public int rankChainFire = 0;
        public int rankThunder = 0;
        public float multHaste = 1f;
        public float multDamage = 1f;
        public float vampiricPct = 0f;

        private float whirlwindTimer = 0f;
        private float chainFireTimer = 0f;
        private float thunderTimer = 0f;
        private GameObject whirlwindOrbRoot;

        // Audio & Settings
        public bool sfxEnabled = true;
        public bool showDmgText = true;
        private AudioSource audioSrc;
        private Dictionary<SfxId, AudioClip> sfxMap = new Dictionary<SfxId, AudioClip>();

        // Camera & Shake
        private Camera mainCam;
        private float shakeTimer = 0f;
        private float shakePower = 0f;
        private const float ArenaHalf = 13.5f;

        // Input / Floating Joystick
        private bool draggingJoy = false;
        private Vector2 joyOriginGui;
        private Vector2 joyCurrentGui;
        private Vector2 manualDir;

        // World Containers
        private GameObject worldRoot;
        private GameObject entitiesRoot;
        private List<HeroMember> heroes = new List<HeroMember>();
        private List<EnemyUnit> enemies = new List<EnemyUnit>();
        private List<DungeonProp> props = new List<DungeonProp>();
        private List<Projectile> projectiles = new List<Projectile>();
        private List<PickupItem> pickups = new List<PickupItem>();
        private List<DangerTelegraph> telegraphs = new List<DangerTelegraph>();
        private List<SlashEffect> effects = new List<SlashEffect>();
        private List<FloatingText> floatTexts = new List<FloatingText>();
        private List<Vector3> pillarPositions = new List<Vector3>();
        private List<Light> torchLights = new List<Light>();

        private UpgradeOffer[] currentOffers = new UpgradeOffer[3];

        // Materials & Textures
        private Material matSpriteBase;
        private Material matSolidUnlit;
        private Material matLit;
        private Dictionary<string, Texture2D> sprites = new Dictionary<string, Texture2D>();
        private Mesh quadMesh;

        // UI Textures & Font
        private Font uiFont;
        private Texture2D texDarkPanel;
        private Texture2D texHeaderPanel;
        private Texture2D texBtnGold;
        private Texture2D texBtnDark;
        private Texture2D texBtnAutoOn;
        private Texture2D texBtnAutoOff;
        private Texture2D texBarBg;
        private Texture2D texHpGreen;
        private Texture2D texHpRed;
        private Texture2D texXpCyan;
        private Texture2D texUltOrange;
        private Texture2D texCardBg;
        private Texture2D texJoyRing;
        private Texture2D texJoyKnob;
        private Texture2D texDimOverlay;

        private float animClock = 0f;
        private int animFrame = 0;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Time.timeScale = 1f;
            totalGold = PlayerPrefs.GetInt("DS_TotalGold", 0);
            bestFloor = PlayerPrefs.GetInt("DS_BestFloor", 1);
        }

        private void Start()
        {
            SetupCameraAndLight();
            LoadResourcesAndShaders();
            BuildUiTextures();
            BuildAudio();
            BuildDungeonArena();
            StartTitleDiorama();
        }

        private void SetupCameraAndLight()
        {
            mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                mainCam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.06f, 0.07f, 0.11f);
            mainCam.fieldOfView = 46f;

            Light sun = FindAnyObjectByType<Light>();
            if (sun == null)
            {
                GameObject lg = new GameObject("DungeonLight");
                sun = lg.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(55f, -20f, 0f);
            sun.color = new Color(0.85f, 0.88f, 1.0f);
            sun.intensity = 1.05f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.40f, 0.50f);
        }

        private void LoadResourcesAndShaders()
        {
            matSpriteBase = Resources.Load<Material>("Materials/DungeonSprite");
            if (matSpriteBase == null)
            {
                Shader sh = Shader.Find("DungeonSlasher/SpriteCutout");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                matSpriteBase = new Material(sh);
            }

            matSolidUnlit = Resources.Load<Material>("Materials/OfficeUnlit");
            if (matSolidUnlit == null)
            {
                Shader sh = Shader.Find("OfficeRumble/Unlit");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
                matSolidUnlit = new Material(sh);
            }

            matLit = Resources.Load<Material>("Materials/OfficeLit");
            if (matLit == null)
            {
                Shader sh = Shader.Find("OfficeRumble/Lit");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
                matLit = new Material(sh);
            }

            string[] names = {
                "hero_knight_0", "hero_knight_1",
                "hero_archer_0", "hero_archer_1",
                "hero_mage_0", "hero_mage_1",
                "hero_priest_0", "hero_priest_1",
                "enemy_slime_0", "enemy_slime_1",
                "enemy_goblin_0", "enemy_goblin_1",
                "enemy_skeleton_0", "enemy_skeleton_1",
                "enemy_orc_0", "enemy_orc_1",
                "enemy_boss_0", "enemy_boss_1",
                "prop_cage", "prop_chest", "prop_crate",
                "fx_coin", "fx_gem", "fx_heart", "fx_fireball", "fx_arrow", "fx_slash",
                "tile_floor"
            };
            foreach (string nm in names)
            {
                Texture2D t = Resources.Load<Texture2D>("Sprites/" + nm);
                if (t != null)
                {
                    t.filterMode = FilterMode.Point;
                    t.wrapMode = nm == "tile_floor" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                    sprites[nm] = t;
                }
            }

            GameObject tmpQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadMesh = tmpQuad.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmpQuad);
        }

        private void BuildUiTextures()
        {
            try
            {
                uiFont = Font.CreateDynamicFontFromOSFont(
                    new string[] { "AppleSDGothicNeo-Bold", "AppleSDGothicNeo-Regular", "PingFangSC-Regular", "Arial" },
                    24);
            }
            catch { uiFont = null; }

            texDarkPanel = MakeBox(64, 64, new Color(0.07f, 0.09f, 0.14f, 0.92f), new Color(0.28f, 0.34f, 0.48f, 0.9f), 2);
            texHeaderPanel = MakeBox(64, 64, new Color(0.10f, 0.12f, 0.20f, 0.96f), new Color(0.95f, 0.72f, 0.24f, 0.95f), 3);
            texBtnGold = MakeBox(64, 64, new Color(0.86f, 0.50f, 0.12f, 1f), new Color(1f, 0.85f, 0.35f, 1f), 3);
            texBtnDark = MakeBox(64, 64, new Color(0.16f, 0.20f, 0.28f, 0.96f), new Color(0.38f, 0.46f, 0.62f, 1f), 2);
            texBtnAutoOn = MakeBox(64, 64, new Color(0.12f, 0.66f, 0.40f, 1f), new Color(0.45f, 1f, 0.68f, 1f), 2);
            texBtnAutoOff = MakeBox(64, 64, new Color(0.24f, 0.26f, 0.32f, 0.95f), new Color(0.55f, 0.58f, 0.66f, 1f), 2);
            texBarBg = MakeSolid(new Color(0.05f, 0.06f, 0.09f, 0.96f));
            texHpGreen = MakeSolid(new Color(0.18f, 0.86f, 0.42f, 1f));
            texHpRed = MakeSolid(new Color(0.92f, 0.24f, 0.26f, 1f));
            texXpCyan = MakeSolid(new Color(0.22f, 0.80f, 1.0f, 1f));
            texUltOrange = MakeSolid(new Color(1.0f, 0.62f, 0.16f, 1f));
            texCardBg = MakeBox(64, 64, new Color(0.12f, 0.15f, 0.24f, 0.98f), new Color(0.52f, 0.72f, 1.0f, 1f), 3);
            texJoyRing = MakeCircle(96, new Color(1f, 1f, 1f, 0.10f), new Color(0.45f, 0.85f, 1f, 0.55f), 4);
            texJoyKnob = MakeCircle(64, new Color(0.35f, 0.82f, 1f, 0.72f), new Color(1f, 1f, 1f, 0.9f), 2);
            texDimOverlay = MakeSolid(new Color(0f, 0f, 0f, 0.65f));
        }

        private Texture2D MakeSolid(Color c)
        {
            Texture2D t = new Texture2D(2, 2);
            t.SetPixels(new[] { c, c, c, c });
            t.Apply();
            return t;
        }

        private Texture2D MakeBox(int w, int h, Color fill, Color border, int bw)
        {
            Texture2D t = new Texture2D(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool edge = x < bw || x >= w - bw || y < bw || y >= h - bw;
                    t.SetPixel(x, y, edge ? border : fill);
                }
            }
            t.Apply();
            return t;
        }

        private Texture2D MakeCircle(int sz, Color fill, Color ring, int rw)
        {
            Texture2D t = new Texture2D(sz, sz);
            float r = sz * 0.5f;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    if (d > r) t.SetPixel(x, y, Color.clear);
                    else if (d >= r - rw) t.SetPixel(x, y, ring);
                    else t.SetPixel(x, y, fill);
                }
            }
            t.Apply();
            return t;
        }

        private void BuildAudio()
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            sfxMap[SfxId.Slash] = MakeTone(360f, 140f, 0.08f, 0.18f);
            sfxMap[SfxId.Bow] = MakeTone(780f, 420f, 0.06f, 0.14f);
            sfxMap[SfxId.Boom] = MakeTone(180f, 55f, 0.16f, 0.24f);
            sfxMap[SfxId.Heal] = MakeTone(520f, 880f, 0.14f, 0.15f);
            sfxMap[SfxId.Pickup] = MakeTone(880f, 1320f, 0.07f, 0.13f);
            sfxMap[SfxId.Rescue] = MakeTone(440f, 990f, 0.25f, 0.22f);
            sfxMap[SfxId.LevelUp] = MakeTone(523f, 1046f, 0.30f, 0.25f);
            sfxMap[SfxId.Hurt] = MakeTone(150f, 80f, 0.09f, 0.20f);
            sfxMap[SfxId.Victory] = MakeTone(587f, 1174f, 0.42f, 0.26f);
        }

        private AudioClip MakeTone(float fStart, float fEnd, float dur, float vol)
        {
            int sr = 22050;
            int n = Mathf.Max(1, (int)(sr * dur));
            float[] data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float freq = Mathf.Lerp(fStart, fEnd, t);
                phase += 2f * Mathf.PI * freq / sr;
                float env = 1f - t;
                data[i] = Mathf.Sign(Mathf.Sin(phase)) * vol * env * 0.65f;
            }
            AudioClip clip = AudioClip.Create("sfx", n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void PlaySfx(SfxId id)
        {
            if (!sfxEnabled || audioSrc == null) return;
            if (sfxMap.TryGetValue(id, out AudioClip c) && c != null)
            {
                audioSrc.PlayOneShot(c);
            }
        }

        private void BuildDungeonArena()
        {
            if (worldRoot != null) Destroy(worldRoot);
            worldRoot = new GameObject("DungeonWorld");
            entitiesRoot = new GameObject("DungeonEntities");
            entitiesRoot.transform.SetParent(worldRoot.transform);
            pillarPositions.Clear();
            torchLights.Clear();

            // Tiled Cobblestone Floor
            GameObject floor = new GameObject("DungeonFloor");
            floor.transform.SetParent(worldRoot.transform);
            floor.transform.position = Vector3.zero;
            floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            floor.transform.localScale = new Vector3(ArenaHalf * 2.2f, ArenaHalf * 2.2f, 1f);
            floor.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            MeshRenderer fr = floor.AddComponent<MeshRenderer>();
            Material fMat = new Material(matSpriteBase);
            if (sprites.ContainsKey("tile_floor")) fMat.SetTexture("_MainTex", sprites["tile_floor"]);
            fMat.SetTextureScale("_MainTex", new Vector2(13f, 13f));
            fMat.SetColor("_BaseColor", new Color(0.88f, 0.90f, 0.96f, 1f));
            fr.sharedMaterial = fMat;

            // Surrounding Stone Dungeon Walls
            Material wallMat = MakeSolidMat(new Color(0.18f, 0.20f, 0.27f));
            Material trimMat = MakeSolidMat(new Color(0.28f, 0.31f, 0.40f));
            float wDist = ArenaHalf + 0.8f;
            CreateBox(worldRoot.transform, new Vector3(0, 1.0f, wDist), new Vector3(ArenaHalf * 2.2f + 2f, 2.2f, 1.4f), wallMat);
            CreateBox(worldRoot.transform, new Vector3(0, 1.0f, -wDist), new Vector3(ArenaHalf * 2.2f + 2f, 2.2f, 1.4f), wallMat);
            CreateBox(worldRoot.transform, new Vector3(-wDist, 1.0f, 0), new Vector3(1.4f, 2.2f, ArenaHalf * 2.2f + 2f), wallMat);
            CreateBox(worldRoot.transform, new Vector3(wDist, 1.0f, 0), new Vector3(1.4f, 2.2f, ArenaHalf * 2.2f + 2f), wallMat);

            // 8 Wall Torches with Warm Flickering Point Lights
            Material torchFlameMat = MakeSolidMat(new Color(1.0f, 0.58f, 0.14f));
            Vector3[] torchCoords = {
                new Vector3(-7f, 1.6f, wDist - 0.8f), new Vector3(7f, 1.6f, wDist - 0.8f),
                new Vector3(-7f, 1.6f, -wDist + 0.8f), new Vector3(7f, 1.6f, -wDist + 0.8f),
                new Vector3(-wDist + 0.8f, 1.6f, 6f), new Vector3(-wDist + 0.8f, 1.6f, -6f),
                new Vector3(wDist - 0.8f, 1.6f, 6f), new Vector3(wDist - 0.8f, 1.6f, -6f)
            };
            foreach (Vector3 tp in torchCoords)
            {
                GameObject flame = CreateBox(worldRoot.transform, tp, new Vector3(0.32f, 0.48f, 0.32f), torchFlameMat);
                Light pt = flame.AddComponent<Light>();
                pt.type = LightType.Point;
                pt.color = new Color(1.0f, 0.62f, 0.25f);
                pt.range = 8.5f;
                pt.intensity = 1.6f;
                torchLights.Add(pt);
            }

            // 4 Stone Pillars (Tactical Cover & Visual Richness)
            Vector3[] pillars = {
                new Vector3(-6.2f, 0f, 6.2f), new Vector3(6.2f, 0f, 6.2f),
                new Vector3(-6.2f, 0f, -6.2f), new Vector3(6.2f, 0f, -6.2f)
            };
            Material runeMat = MakeSolidMat(new Color(0.25f, 0.72f, 1.0f));
            foreach (Vector3 pp in pillars)
            {
                CreateBox(worldRoot.transform, pp + Vector3.up * 0.9f, new Vector3(1.25f, 1.8f, 1.25f), wallMat);
                CreateBox(worldRoot.transform, pp + Vector3.up * 1.85f, new Vector3(1.45f, 0.22f, 1.45f), trimMat);
                CreateBox(worldRoot.transform, pp + Vector3.up * 1.1f, new Vector3(1.32f, 0.16f, 1.32f), runeMat);
                pillarPositions.Add(pp);
            }
        }

        private GameObject CreateBox(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private Material MakeSolidMat(Color c)
        {
            Material m = new Material(matLit != null ? matLit : matSolidUnlit);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }

        private GameObject CreateBillboard(string spriteKey, Vector3 pos, float size, out Renderer rend)
        {
            GameObject root = new GameObject("Ent_" + spriteKey);
            root.transform.SetParent(entitiesRoot.transform);
            root.transform.position = pos;

            // Ground shadow
            GameObject sh = new GameObject("Shadow");
            sh.transform.SetParent(root.transform);
            sh.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            sh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            sh.transform.localScale = new Vector3(size * 0.72f, size * 0.36f, 1f);
            sh.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            MeshRenderer sr = sh.AddComponent<MeshRenderer>();
            Material sm = new Material(matSolidUnlit);
            sm.SetColor("_BaseColor", new Color(0.03f, 0.03f, 0.05f));
            sr.sharedMaterial = sm;

            // Tilted Pixel Quad
            GameObject quad = new GameObject("Sprite");
            quad.transform.SetParent(root.transform);
            quad.transform.localPosition = new Vector3(0f, size * 0.48f, 0f);
            quad.transform.localRotation = Quaternion.Euler(52f, 0f, 0f);
            quad.transform.localScale = new Vector3(size, size, 1f);
            quad.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            MeshRenderer mr = quad.AddComponent<MeshRenderer>();
            Material mm = new Material(matSpriteBase);
            if (sprites.TryGetValue(spriteKey, out Texture2D tex))
            {
                mm.SetTexture("_MainTex", tex);
            }
            mm.SetColor("_BaseColor", Color.white);
            mr.sharedMaterial = mm;
            rend = mr;
            return root;
        }

        private void ClearEntities()
        {
            if (entitiesRoot != null) Destroy(entitiesRoot);
            entitiesRoot = new GameObject("DungeonEntities");
            entitiesRoot.transform.SetParent(worldRoot.transform);
            heroes.Clear();
            enemies.Clear();
            props.Clear();
            projectiles.Clear();
            pickups.Clear();
            telegraphs.Clear();
            effects.Clear();
            floatTexts.Clear();
            whirlwindOrbRoot = null;
        }

        private void StartTitleDiorama()
        {
            ClearEntities();
            screen = GameScreen.Title;
            SpawnHero(HeroClass.Knight, new Vector3(-1.2f, 0, -1f));
            SpawnHero(HeroClass.Archer, new Vector3(1.2f, 0, -1.8f));
            SpawnHero(HeroClass.Mage, new Vector3(0f, 0, -2.5f));
            SpawnProp(PropKind.Chest, new Vector3(-3.2f, 0, 1.5f), HeroClass.Knight);
            SpawnProp(PropKind.Cage, new Vector3(3.2f, 0, 1.5f), HeroClass.Priest);
            SpawnEnemy(EnemyKind.Slime, new Vector3(-2.5f, 0, 4.0f));
            SpawnEnemy(EnemyKind.Goblin, new Vector3(2.4f, 0, 4.2f));
        }

        public void StartRun(int floor)
        {
            ClearEntities();
            currentFloor = floor;
            waveIndex = 1;
            runTimer = 0f;
            spawnTimer = 0.5f;
            bossSpawned = false;
            bossDefeated = false;

            squadLevel = 1;
            squadXp = 0f;
            nextXp = 25f;
            runGold = 0;
            kills = 0;
            ultCharge = 35f;
            hasteBuffTimer = 0f;

            rankWhirlwind = 0;
            rankChainFire = 0;
            rankThunder = 0;
            multHaste = 1f;
            multDamage = 1f;
            vampiricPct = 0f;

            // Start with Knight + Archer duo (instant 우르릉 feel!)
            SpawnHero(HeroClass.Knight, new Vector3(0f, 0f, -2f));
            SpawnHero(HeroClass.Archer, new Vector3(-1.3f, 0f, -3.2f));

            // Initial Dungeon Props: 2 Prison Cages + 3 Chests + Crates
            SpawnProp(PropKind.Cage, new Vector3(-8.5f, 0f, 3.0f), HeroClass.Mage);
            SpawnProp(PropKind.Cage, new Vector3(8.5f, 0f, 5.5f), HeroClass.Priest);
            SpawnProp(PropKind.Chest, new Vector3(-3.5f, 0f, 8.5f), HeroClass.Knight);
            SpawnProp(PropKind.Chest, new Vector3(4.0f, 0f, -8.5f), HeroClass.Knight);
            SpawnProp(PropKind.Crate, new Vector3(2.5f, 0f, 2.5f), HeroClass.Knight);
            SpawnProp(PropKind.Crate, new Vector3(-2.8f, 0f, -5.5f), HeroClass.Knight);

            // Initial Wave
            for (int i = 0; i < 6; i++)
            {
                SpawnEnemyNearEdge(i < 4 ? EnemyKind.Slime : EnemyKind.Goblin);
            }

            screen = GameScreen.Playing;
            AddFloatText(Vector3.zero + Vector3.up * 2f, "제 " + currentFloor + "층 탐험 시작!", new Color(1f, 0.86f, 0.25f), true);
            PlaySfx(SfxId.Rescue);
        }

        private void SpawnHero(HeroClass hc, Vector3 pos)
        {
            HeroMember h = new HeroMember { hClass = hc, pos = pos };
            float scaleMult = 1f + (currentFloor - 1) * 0.15f;
            switch (hc)
            {
                case HeroClass.Knight:
                    h.name = "기사 아서";
                    h.maxHp = 260f * scaleMult;
                    h.attackDamage = 32f;
                    h.attackRange = 2.3f;
                    h.attackCooldown = 0.72f;
                    break;
                case HeroClass.Archer:
                    h.name = "궁수 레나";
                    h.maxHp = 165f * scaleMult;
                    h.attackDamage = 24f;
                    h.attackRange = 8.0f;
                    h.attackCooldown = 0.55f;
                    break;
                case HeroClass.Mage:
                    h.name = "마법사 루나";
                    h.maxHp = 155f * scaleMult;
                    h.attackDamage = 42f;
                    h.attackRange = 7.2f;
                    h.attackCooldown = 1.05f;
                    break;
                case HeroClass.Priest:
                    h.name = "사제 세라";
                    h.maxHp = 185f * scaleMult;
                    h.attackDamage = 22f;
                    h.attackRange = 6.5f;
                    h.attackCooldown = 1.15f;
                    break;
            }
            h.hp = h.maxHp;
            h.animPhase = UnityEngine.Random.Range(0f, 3f);
            string key = GetHeroSpriteKey(hc, 0);
            h.root = CreateBillboard(key, pos, 1.45f, out h.spriteRend);
            heroes.Add(h);
        }

        private string GetHeroSpriteKey(HeroClass hc, int frame)
        {
            string f = frame == 0 ? "_0" : "_1";
            switch (hc)
            {
                case HeroClass.Knight: return "hero_knight" + f;
                case HeroClass.Archer: return "hero_archer" + f;
                case HeroClass.Mage: return "hero_mage" + f;
                default: return "hero_priest" + f;
            }
        }

        private void SpawnEnemyNearEdge(EnemyKind kind)
        {
            Vector3 leader = heroes.Count > 0 ? heroes[0].pos : Vector3.zero;
            Vector3 p = Vector3.zero;
            for (int tries = 0; tries < 10; tries++)
            {
                float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float dist = UnityEngine.Random.Range(7.5f, 12.0f);
                p = new Vector3(
                    Mathf.Clamp(leader.x + Mathf.Cos(ang) * dist, -ArenaHalf + 1.5f, ArenaHalf - 1.5f),
                    0f,
                    Mathf.Clamp(leader.z + Mathf.Sin(ang) * dist, -ArenaHalf + 1.5f, ArenaHalf - 1.5f)
                );
                if (Vector3.Distance(p, leader) > 6.0f) break;
            }
            SpawnEnemy(kind, p);
        }

        private void SpawnEnemy(EnemyKind kind, Vector3 pos)
        {
            float fm = 1f + (currentFloor - 1) * 0.45f;
            EnemyUnit e = new EnemyUnit { kind = kind, pos = pos };
            float size = 1.35f;
            switch (kind)
            {
                case EnemyKind.Slime:
                    e.name = "초록 슬라임"; e.maxHp = 48f * fm; e.speed = 2.6f; e.damage = 9f * fm;
                    e.attackRange = 1.35f; e.attackCooldown = 1.0f; e.radius = 0.6f;
                    break;
                case EnemyKind.Goblin:
                    e.name = "고블린 창병"; e.maxHp = 78f * fm; e.speed = 3.5f; e.damage = 13f * fm;
                    e.attackRange = 1.55f; e.attackCooldown = 0.85f; e.radius = 0.65f;
                    break;
                case EnemyKind.Skeleton:
                    e.name = "해골 궁수"; e.maxHp = 65f * fm; e.speed = 2.4f; e.damage = 15f * fm;
                    e.attackRange = 7.5f; e.attackCooldown = 1.65f; e.radius = 0.65f;
                    break;
                case EnemyKind.Orc:
                    e.name = "오크 광전사"; e.maxHp = 210f * fm; e.speed = 2.2f; e.damage = 24f * fm;
                    e.attackRange = 1.85f; e.attackCooldown = 1.15f; e.radius = 0.85f;
                    size = 1.75f;
                    break;
                case EnemyKind.Boss:
                    e.name = "심연의 마왕 발록"; e.maxHp = 1650f * fm; e.speed = 2.35f; e.damage = 34f * fm;
                    e.attackRange = 2.5f; e.attackCooldown = 1.25f; e.radius = 1.3f; e.specialTimer = 3.2f;
                    size = 2.85f;
                    break;
            }
            e.hp = e.maxHp;
            e.root = CreateBillboard(GetEnemySpriteKey(kind, 0), pos, size, out e.spriteRend);
            enemies.Add(e);
        }

        private string GetEnemySpriteKey(EnemyKind kind, int frame)
        {
            string f = frame == 0 ? "_0" : "_1";
            switch (kind)
            {
                case EnemyKind.Slime: return "enemy_slime" + f;
                case EnemyKind.Goblin: return "enemy_goblin" + f;
                case EnemyKind.Skeleton: return "enemy_skeleton" + f;
                case EnemyKind.Orc: return "enemy_orc" + f;
                default: return "enemy_boss" + f;
            }
        }

        private void SpawnProp(PropKind kind, Vector3 pos, HeroClass captive)
        {
            DungeonProp p = new DungeonProp { kind = kind, pos = pos, captiveClass = captive, radius = 0.9f };
            string key = kind == PropKind.Cage ? "prop_cage" : (kind == PropKind.Chest ? "prop_chest" : "prop_crate");
            p.maxHp = kind == PropKind.Cage ? 140f : (kind == PropKind.Chest ? 110f : 55f);
            p.hp = p.maxHp;
            p.root = CreateBillboard(key, pos, kind == PropKind.Crate ? 1.2f : 1.65f, out p.spriteRend);

            if (kind == PropKind.Cage)
            {
                // Show tiny trapped hero preview slightly behind cage bars
                GameObject cap = new GameObject("Captive");
                cap.transform.SetParent(p.root.transform);
                cap.transform.localPosition = new Vector3(0f, 0.65f, 0.14f);
                cap.transform.localRotation = Quaternion.Euler(52f, 0f, 0f);
                cap.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
                cap.AddComponent<MeshFilter>().sharedMesh = quadMesh;
                MeshRenderer mr = cap.AddComponent<MeshRenderer>();
                Material mm = new Material(matSpriteBase);
                if (sprites.TryGetValue(GetHeroSpriteKey(captive, 0), out Texture2D tex))
                    mm.SetTexture("_MainTex", tex);
                mm.SetColor("_BaseColor", new Color(0.85f, 0.9f, 1f));
                mr.sharedMaterial = mm;
                p.captiveRend = mr;
            }
            props.Add(p);
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            // Animate flickering torches
            for (int i = 0; i < torchLights.Count; i++)
            {
                if (torchLights[i] != null)
                    torchLights[i].intensity = 1.45f + Mathf.Sin(Time.time * 7f + i * 1.7f) * 0.28f;
            }

            animClock += dt;
            if (animClock >= 0.22f)
            {
                animClock = 0f;
                animFrame = 1 - animFrame;
            }

            if (screen == GameScreen.Title)
            {
                UpdateTitleDiorama(dt);
                return;
            }

            if (screen != GameScreen.Playing)
            {
                UpdateCamera(dt);
                return;
            }

            runTimer += dt;
            if (hasteBuffTimer > 0f) hasteBuffTimer -= dt;
            if (shakeTimer > 0f) shakeTimer -= dt;

            HandleTouchJoystick();
            UpdateWavesAndSpawning(dt);
            UpdateHeroes(dt);
            UpdatePassiveSkills(dt);
            UpdateEnemies(dt);
            UpdateProjectiles(dt);
            UpdateTelegraphs(dt);
            UpdatePickups(dt);
            UpdateEffectsAndText(dt);
            UpdateCamera(dt);
            UpdateSpriteAnimations();
        }

        private void UpdateTitleDiorama(float dt)
        {
            float t = Time.time * 0.45f;
            for (int i = 0; i < heroes.Count; i++)
            {
                float a = t + i * 2.1f;
                Vector3 target = new Vector3(Mathf.Cos(a) * 2.2f, 0f, Mathf.Sin(a) * 1.5f);
                heroes[i].facingRight = target.x >= heroes[i].pos.x;
                heroes[i].pos = Vector3.Lerp(heroes[i].pos, target, dt * 3f);
                heroes[i].root.transform.position = heroes[i].pos + Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 6f + i)) * 0.12f;
            }
            UpdateSpriteAnimations();
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(Mathf.Sin(t * 0.5f) * 1.5f, 11.5f, -9.5f);
                mainCam.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
            }
        }

        private void HandleTouchJoystick()
        {
            float rx = Screen.width / 750f;
            float ry = Screen.height / 1334f;

            if (Input.GetMouseButtonDown(0))
            {
                Vector2 gPos = new Vector2(Input.mousePosition.x / rx, (Screen.height - Input.mousePosition.y) / ry);
                // Ignore top HUD & bottom-right Ult button
                bool inTopHud = gPos.y < 185f;
                bool inUltBtn = gPos.x > 550f && gPos.y > 1060f;
                if (!inTopHud && !inUltBtn)
                {
                    draggingJoy = true;
                    joyOriginGui = gPos;
                    joyCurrentGui = gPos;
                    manualDir = Vector2.zero;
                }
            }
            else if (Input.GetMouseButton(0) && draggingJoy)
            {
                joyCurrentGui = new Vector2(Input.mousePosition.x / rx, (Screen.height - Input.mousePosition.y) / ry);
                Vector2 delta = joyCurrentGui - joyOriginGui;
                float maxR = 85f;
                if (delta.magnitude > maxR) delta = delta.normalized * maxR;
                joyCurrentGui = joyOriginGui + delta;
                manualDir = delta.magnitude > 10f ? delta / maxR : Vector2.zero;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                draggingJoy = false;
                manualDir = Vector2.zero;
            }
        }

        private void UpdateWavesAndSpawning(float dt)
        {
            if (runTimer < 26f) waveIndex = 1;
            else if (runTimer < 58f) waveIndex = 2;
            else waveIndex = 3;

            if (waveIndex == 3 && !bossSpawned)
            {
                bossSpawned = true;
                SpawnEnemyNearEdge(EnemyKind.Boss);
                SpawnProp(PropKind.Cage, new Vector3(0f, 0f, 7.5f), heroes.Count < 4 ? HeroClass.Mage : HeroClass.Knight);
                SpawnProp(PropKind.Chest, new Vector3(-6.5f, 0f, -6.5f), HeroClass.Knight);
                AddFloatText(heroes[0].pos + Vector3.up * 2.5f, "경고! 심연의 마왕 발록 등장!", new Color(1f, 0.25f, 0.25f), true);
                TriggerShake(0.35f, 0.35f);
                PlaySfx(SfxId.Hurt);
            }

            spawnTimer -= dt;
            if (spawnTimer <= 0f && enemies.Count < 22 && !bossDefeated)
            {
                spawnTimer = waveIndex == 1 ? 2.4f : (waveIndex == 2 ? 2.0f : 3.2f);
                int batch = waveIndex == 1 ? 2 : 3;
                for (int i = 0; i < batch; i++)
                {
                    float r = UnityEngine.Random.value;
                    EnemyKind k = EnemyKind.Slime;
                    if (waveIndex == 1) k = r < 0.65f ? EnemyKind.Slime : EnemyKind.Goblin;
                    else if (waveIndex == 2) k = r < 0.35f ? EnemyKind.Goblin : (r < 0.72f ? EnemyKind.Skeleton : EnemyKind.Orc);
                    else k = r < 0.45f ? EnemyKind.Skeleton : (r < 0.80f ? EnemyKind.Goblin : EnemyKind.Orc);
                    SpawnEnemyNearEdge(k);
                }
            }
        }

        private Vector3 ComputeAutoMoveDir(Vector3 leaderPos)
        {
            // 1. Dodge incoming danger telegraphs
            foreach (DangerTelegraph dt in telegraphs)
            {
                Vector3 away = leaderPos - dt.pos;
                away.y = 0f;
                if (away.magnitude < dt.radius + 1.8f)
                {
                    if (away.sqrMagnitude < 0.01f) away = Vector3.right;
                    return away.normalized;
                }
            }

            // 2. Grab nearby healing heart if injured, or XP gem very close
            float avgHpPct = GetSquadHpRatio();
            PickupItem bestPickup = null;
            float bestPickDist = 5.5f;
            foreach (PickupItem p in pickups)
            {
                if (p.kind == PickupKind.Heart && avgHpPct > 0.85f) continue;
                float d = Vector3.Distance(leaderPos, p.pos);
                float weight = p.kind == PickupKind.Heart && avgHpPct < 0.6f ? d * 0.4f : d;
                if (weight < bestPickDist)
                {
                    bestPickDist = weight;
                    bestPickup = p;
                }
            }
            if (bestPickup != null && bestPickDist < 3.8f)
            {
                return (bestPickup.pos - leaderPos).normalized;
            }

            // 3. Rescue Prison Cage (highest priority when squad < 5!)
            DungeonProp bestCage = null;
            float cageDist = 999f;
            foreach (DungeonProp pr in props)
            {
                if (pr.kind == PropKind.Cage || pr.kind == PropKind.Chest)
                {
                    float d = Vector3.Distance(leaderPos, pr.pos);
                    if (pr.kind == PropKind.Cage) d *= 0.65f;
                    if (d < cageDist)
                    {
                        cageDist = d;
                        bestCage = pr;
                    }
                }
            }

            // 4. Nearest Enemy
            EnemyUnit nearestE = FindNearestEnemy(leaderPos);
            float eDist = nearestE != null ? Vector3.Distance(leaderPos, nearestE.pos) : 999f;

            if (bestCage != null && (cageDist < 8.5f || nearestE == null))
            {
                Vector3 toProp = bestCage.pos - leaderPos;
                if (toProp.magnitude > 1.7f) return toProp.normalized;
                return Vector3.zero;
            }

            if (nearestE != null)
            {
                Vector3 toE = nearestE.pos - leaderPos;
                float idealStop = 1.9f;
                if (toE.magnitude > idealStop) return toE.normalized;
                // Gentle strafe around boss
                if (nearestE.kind == EnemyKind.Boss)
                    return new Vector3(-toE.z, 0f, toE.x).normalized * 0.45f;
                return Vector3.zero;
            }

            return Vector3.zero;
        }

        private void UpdateHeroes(float dt)
        {
            if (heroes.Count == 0)
            {
                screen = GameScreen.Defeat;
                return;
            }

            HeroMember leader = heroes[0];
            float moveSpeed = 5.4f * multHaste * (hasteBuffTimer > 0f ? 1.45f : 1f);

            // Movement: Manual drag overrides AUTO cleanly
            Vector3 moveDir = Vector3.zero;
            if (draggingJoy && manualDir.sqrMagnitude > 0.01f)
            {
                moveDir = new Vector3(manualDir.x, 0f, -manualDir.y); // Screen Y down -> world -Z
                // Note: Input.mousePosition in OnGUI conversion: top is 0, bottom is 1334.
                // Dragging UP gives negative gPos delta.y, so -manualDir.y is positive +Z (North)!
            }
            else if (autoMode)
            {
                moveDir = ComputeAutoMoveDir(leader.pos);
                // Auto Ult when charged and enemies clustered
                if (ultCharge >= 100f && enemies.Count >= 3)
                {
                    TriggerUltimate();
                }
            }

            if (moveDir.sqrMagnitude > 0.001f)
            {
                leader.pos += moveDir * moveSpeed * dt;
                leader.pos = ClampInsideArena(leader.pos, 0.7f);
                if (Mathf.Abs(moveDir.x) > 0.08f) leader.facingRight = moveDir.x > 0f;
            }

            // Formation follow for squadmates (우르릉 모험단 쫄래쫄래 대열!)
            for (int i = 1; i < heroes.Count; i++)
            {
                HeroMember hm = heroes[i];
                float ang = (i - 1) * (Mathf.PI * 2f / Mathf.Max(1, heroes.Count - 1)) + 0.7f;
                Vector3 offset = new Vector3(Mathf.Cos(ang) * 1.45f, 0f, Mathf.Sin(ang) * 1.45f - 0.5f);
                Vector3 targetPos = ClampInsideArena(leader.pos + offset, 0.65f);
                Vector3 diff = targetPos - hm.pos;
                if (diff.magnitude > 0.15f)
                {
                    hm.pos += diff.normalized * Mathf.Min(diff.magnitude, moveSpeed * 1.15f * dt);
                    if (Mathf.Abs(diff.x) > 0.05f) hm.facingRight = diff.x > 0f;
                }
            }

            // Hero Auto-Attack & Healing
            float atkSpeedMult = multHaste * (hasteBuffTimer > 0f ? 1.55f : 1f);
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroMember hm = heroes[i];
                if (hm.hitFlash > 0f) hm.hitFlash -= dt;
                hm.cooldownTimer -= dt * atkSpeedMult;

                bool walking = (i == 0 && moveDir.sqrMagnitude > 0.05f);
                float bob = walking ? Mathf.Abs(Mathf.Sin(Time.time * 12f + hm.animPhase)) * 0.16f : 0f;
                hm.root.transform.position = hm.pos + Vector3.up * bob;

                if (hm.cooldownTimer <= 0f)
                {
                    // Priest prioritizes healing if anyone < 80% HP
                    if (hm.hClass == HeroClass.Priest && GetSquadHpRatio() < 0.85f)
                    {
                        foreach (HeroMember ally in heroes)
                        {
                            ally.hp = Mathf.Min(ally.maxHp, ally.hp + 22f * multDamage);
                        }
                        AddFloatText(hm.pos + Vector3.up * 1.5f, "+치유!", new Color(0.42f, 1f, 0.55f), false);
                        PlaySfx(SfxId.Heal);
                        hm.cooldownTimer = hm.attackCooldown;
                        continue;
                    }

                    // Find closest enemy or breakable prop in range
                    EnemyUnit tgtE = FindNearestEnemy(hm.pos);
                    DungeonProp tgtP = FindNearestProp(hm.pos);
                    float dE = tgtE != null ? Vector3.Distance(hm.pos, tgtE.pos) : 999f;
                    float dP = tgtP != null ? Vector3.Distance(hm.pos, tgtP.pos) : 999f;

                    Vector3? aimPos = null;
                    if (dE <= hm.attackRange && dE <= dP + 0.5f) aimPos = tgtE.pos;
                    else if (dP <= hm.attackRange) aimPos = tgtP.pos;

                    if (aimPos.HasValue)
                    {
                        hm.facingRight = aimPos.Value.x >= hm.pos.x;
                        PerformHeroAttack(hm, aimPos.Value, tgtE, tgtP, dE <= hm.attackRange && dE <= dP + 0.5f);
                        hm.cooldownTimer = hm.attackCooldown;
                    }
                }
            }
        }

        private void PerformHeroAttack(HeroMember hm, Vector3 aim, EnemyUnit tgtE, DungeonProp tgtP, bool hitEnemy)
        {
            float dmg = hm.attackDamage * multDamage;
            Vector3 dir = (aim - hm.pos); dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            dir.Normalize();

            if (hm.hClass == HeroClass.Knight)
            {
                // Cleave all enemies/props in 2.6m arc
                SpawnSlashEffect(hm.pos + dir * 1.1f + Vector3.up * 0.6f, 1.4f, new Color(0.5f, 0.9f, 1f));
                PlaySfx(SfxId.Slash);
                foreach (EnemyUnit e in enemies.ToArray())
                {
                    if (Vector3.Distance(hm.pos, e.pos) <= hm.attackRange + e.radius)
                        DamageEnemy(e, dmg);
                }
                foreach (DungeonProp p in props.ToArray())
                {
                    if (Vector3.Distance(hm.pos, p.pos) <= hm.attackRange + p.radius)
                        DamageProp(p, dmg);
                }
            }
            else if (hm.hClass == HeroClass.Archer)
            {
                SpawnProjectile(true, false, hm.pos + Vector3.up * 0.7f, dir * 16f, dmg, 0f);
                PlaySfx(SfxId.Bow);
            }
            else if (hm.hClass == HeroClass.Mage)
            {
                SpawnProjectile(true, true, hm.pos + Vector3.up * 0.7f, dir * 11.5f, dmg, 2.3f);
                PlaySfx(SfxId.Boom);
            }
            else if (hm.hClass == HeroClass.Priest)
            {
                // Holy Smite instant bolt
                if (hitEnemy && tgtE != null)
                {
                    SpawnSlashEffect(tgtE.pos + Vector3.up * 0.6f, 1.3f, new Color(1f, 0.92f, 0.45f));
                    DamageEnemy(tgtE, dmg);
                }
                else if (tgtP != null)
                {
                    SpawnSlashEffect(tgtP.pos + Vector3.up * 0.6f, 1.2f, new Color(1f, 0.92f, 0.45f));
                    DamageProp(tgtP, dmg);
                }
                PlaySfx(SfxId.Heal);
            }
        }

        private void UpdatePassiveSkills(float dt)
        {
            if (heroes.Count == 0) return;
            Vector3 center = heroes[0].pos;

            // 1. Whirlwind
            if (rankWhirlwind > 0)
            {
                if (whirlwindOrbRoot == null)
                {
                    Renderer r;
                    whirlwindOrbRoot = CreateBillboard("fx_slash", center, 1.3f, out r);
                }
                float ang = Time.time * (5.5f + rankWhirlwind);
                whirlwindOrbRoot.transform.position = center + new Vector3(Mathf.Cos(ang) * 2.4f, 0.5f, Mathf.Sin(ang) * 2.4f);
                whirlwindTimer -= dt;
                if (whirlwindTimer <= 0f)
                {
                    whirlwindTimer = 0.42f;
                    foreach (EnemyUnit e in enemies.ToArray())
                    {
                        if (Vector3.Distance(center, e.pos) <= 3.1f)
                            DamageEnemy(e, (20f + 12f * rankWhirlwind) * multDamage);
                    }
                }
            }

            // 2. Chain Fireball
            if (rankChainFire > 0)
            {
                chainFireTimer -= dt;
                if (chainFireTimer <= 0f && enemies.Count > 0)
                {
                    chainFireTimer = Mathf.Max(0.75f, 1.8f - rankChainFire * 0.2f);
                    EnemyUnit tgt = FindNearestEnemy(center);
                    if (tgt != null)
                    {
                        Vector3 dir = (tgt.pos - center).normalized;
                        SpawnProjectile(true, true, center + Vector3.up * 0.7f, dir * 12.5f, (35f + 18f * rankChainFire) * multDamage, 2.5f);
                        PlaySfx(SfxId.Boom);
                    }
                }
            }

            // 3. Thunder Strike
            if (rankThunder > 0)
            {
                thunderTimer -= dt;
                if (thunderTimer <= 0f && enemies.Count > 0)
                {
                    thunderTimer = Mathf.Max(0.9f, 2.2f - rankThunder * 0.25f);
                    EnemyUnit high = enemies[0];
                    foreach (EnemyUnit e in enemies) if (e.hp > high.hp) high = e;
                    SpawnSlashEffect(high.pos + Vector3.up * 1.2f, 2.1f, new Color(0.45f, 0.92f, 1f));
                    DamageEnemy(high, (55f + 28f * rankThunder) * multDamage);
                    PlaySfx(SfxId.Slash);
                }
            }
        }

        public void TriggerUltimate()
        {
            if (ultCharge < 100f || heroes.Count == 0) return;
            ultCharge = 0f;
            hasteBuffTimer = 5.0f;
            Vector3 center = heroes[0].pos;
            TriggerShake(0.4f, 0.45f);
            PlaySfx(SfxId.Boom);
            AddFloatText(center + Vector3.up * 2.3f, "⚡ 일제 돌격 메테오!!", new Color(1f, 0.78f, 0.22f), true);
            foreach (EnemyUnit e in enemies.ToArray())
            {
                if (Vector3.Distance(center, e.pos) <= 10.5f)
                {
                    SpawnSlashEffect(e.pos + Vector3.up * 0.6f, 2.2f, new Color(1f, 0.55f, 0.15f));
                    DamageEnemy(e, 110f * multDamage);
                }
            }
            foreach (DungeonProp p in props.ToArray())
            {
                if (Vector3.Distance(center, p.pos) <= 9.5f) DamageProp(p, 90f);
            }
        }

        private void UpdateEnemies(float dt)
        {
            if (heroes.Count == 0) return;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyUnit e = enemies[i];
                if (e.hitFlash > 0f) e.hitFlash -= dt;
                e.cooldownTimer -= dt;

                HeroMember tgt = FindNearestHero(e.pos);
                if (tgt == null) continue;

                Vector3 diff = tgt.pos - e.pos;
                diff.y = 0f;
                float dist = diff.magnitude;
                if (Mathf.Abs(diff.x) > 0.05f) e.facingRight = diff.x > 0f;

                if (dist > e.attackRange)
                {
                    e.pos += diff.normalized * e.speed * dt;
                    e.pos = ClampInsideArena(e.pos, e.radius);
                }
                else if (e.cooldownTimer <= 0f)
                {
                    e.cooldownTimer = e.attackCooldown;
                    if (e.kind == EnemyKind.Skeleton)
                    {
                        SpawnProjectile(false, false, e.pos + Vector3.up * 0.6f, diff.normalized * 8.5f, e.damage, 0f);
                    }
                    else
                    {
                        DamageHero(tgt, e.damage);
                    }
                }

                // Boss Special Telegraphed AoE + Radial Volley
                if (e.kind == EnemyKind.Boss)
                {
                    e.specialTimer -= dt;
                    if (e.specialTimer <= 0f)
                    {
                        e.specialTimer = 4.2f;
                        SpawnTelegraph(tgt.pos, 3.0f, 1.35f, e.damage * 1.6f);
                        for (int k = 0; k < 6; k++)
                        {
                            float a = k * (Mathf.PI * 2f / 6f) + Time.time;
                            SpawnProjectile(false, true, e.pos + Vector3.up * 0.8f, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 6.8f, e.damage * 0.9f, 0f);
                        }
                    }
                }

                float bob = Mathf.Abs(Mathf.Sin(Time.time * 9f + i)) * 0.09f;
                e.root.transform.position = e.pos + Vector3.up * bob;
            }
        }

        private void SpawnTelegraph(Vector3 pos, float radius, float delay, float dmg)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Collider c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
            go.transform.SetParent(entitiesRoot.transform);
            go.transform.position = new Vector3(pos.x, 0.05f, pos.z);
            go.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
            Material m = new Material(matSolidUnlit);
            m.SetColor("_BaseColor", new Color(0.95f, 0.16f, 0.18f));
            go.GetComponent<Renderer>().sharedMaterial = m;
            telegraphs.Add(new DangerTelegraph { root = go, pos = pos, radius = radius, timer = delay, maxTime = delay, damage = dmg });
        }

        private void UpdateTelegraphs(float dt)
        {
            for (int i = telegraphs.Count - 1; i >= 0; i--)
            {
                DangerTelegraph t = telegraphs[i];
                t.timer -= dt;
                float pct = 1f - Mathf.Clamp01(t.timer / t.maxTime);
                t.root.transform.localScale = new Vector3(t.radius * 2f * (0.4f + 0.6f * pct), 0.02f, t.radius * 2f * (0.4f + 0.6f * pct));
                if (t.timer <= 0f)
                {
                    TriggerShake(0.25f, 0.25f);
                    PlaySfx(SfxId.Boom);
                    SpawnSlashEffect(t.pos + Vector3.up * 0.5f, t.radius * 1.4f, new Color(1f, 0.3f, 0.2f));
                    foreach (HeroMember h in heroes.ToArray())
                    {
                        if (Vector3.Distance(h.pos, t.pos) <= t.radius)
                            DamageHero(h, t.damage);
                    }
                    Destroy(t.root);
                    telegraphs.RemoveAt(i);
                }
            }
        }

        private void SpawnProjectile(bool isPlayer, bool isFireball, Vector3 pos, Vector3 vel, float dmg, float aoe)
        {
            Renderer r;
            string key = isPlayer ? (isFireball ? "fx_fireball" : "fx_arrow") : "fx_fireball";
            GameObject go = CreateBillboard(key, pos, isFireball ? 0.85f : 0.70f, out r);
            if (!isPlayer && r != null) r.sharedMaterial.SetColor("_BaseColor", new Color(1f, 0.32f, 0.85f));
            projectiles.Add(new Projectile { isPlayer = isPlayer, isFireball = isFireball, root = go, pos = pos, vel = vel, damage = dmg, aoeRadius = aoe, life = 2.2f });
        }

        private void UpdateProjectiles(float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Projectile pr = projectiles[i];
                pr.life -= dt;
                pr.pos += pr.vel * dt;
                pr.root.transform.position = pr.pos;

                bool hit = pr.life <= 0f || Mathf.Abs(pr.pos.x) > ArenaHalf || Mathf.Abs(pr.pos.z) > ArenaHalf;

                if (!hit && pr.isPlayer)
                {
                    foreach (EnemyUnit e in enemies.ToArray())
                    {
                        if (Vector3.Distance(new Vector3(pr.pos.x, 0, pr.pos.z), e.pos) <= e.radius + 0.45f)
                        {
                            hit = true;
                            if (pr.aoeRadius > 0f)
                            {
                                SpawnSlashEffect(pr.pos, pr.aoeRadius * 1.1f, new Color(1f, 0.52f, 0.15f));
                                foreach (EnemyUnit ae in enemies.ToArray())
                                    if (Vector3.Distance(ae.pos, e.pos) <= pr.aoeRadius) DamageEnemy(ae, pr.damage);
                            }
                            else DamageEnemy(e, pr.damage);
                            break;
                        }
                    }
                    if (!hit)
                    {
                        foreach (DungeonProp p in props.ToArray())
                        {
                            if (Vector3.Distance(new Vector3(pr.pos.x, 0, pr.pos.z), p.pos) <= p.radius + 0.4f)
                            {
                                hit = true;
                                DamageProp(p, pr.damage);
                                break;
                            }
                        }
                    }
                }
                else if (!hit && !pr.isPlayer)
                {
                    foreach (HeroMember h in heroes.ToArray())
                    {
                        if (Vector3.Distance(new Vector3(pr.pos.x, 0, pr.pos.z), h.pos) <= 0.72f)
                        {
                            hit = true;
                            DamageHero(h, pr.damage);
                            break;
                        }
                    }
                }

                if (hit)
                {
                    Destroy(pr.root);
                    projectiles.RemoveAt(i);
                }
            }
        }

        private void DamageEnemy(EnemyUnit e, float dmg)
        {
            e.hp -= dmg;
            e.hitFlash = 0.12f;
            if (showDmgText)
                AddFloatText(e.pos + Vector3.up * 1.3f, Mathf.RoundToInt(dmg).ToString(), e.kind == EnemyKind.Boss ? new Color(1f, 0.75f, 0.25f) : Color.white, false);

            if (e.hp <= 0f)
            {
                kills++;
                ultCharge = Mathf.Min(100f, ultCharge + (e.kind == EnemyKind.Boss ? 40f : 6.5f));
                if (vampiricPct > 0f && heroes.Count > 0)
                {
                    heroes[0].hp = Mathf.Min(heroes[0].maxHp, heroes[0].hp + heroes[0].maxHp * vampiricPct);
                }

                SpawnPickup(PickupKind.Gem, e.pos, e.kind == EnemyKind.Orc ? 24f : (e.kind == EnemyKind.Boss ? 100f : 10f));
                if (UnityEngine.Random.value < 0.45f) SpawnPickup(PickupKind.Coin, e.pos + new Vector3(0.4f, 0, 0.2f), 10f);
                if (UnityEngine.Random.value < 0.08f) SpawnPickup(PickupKind.Heart, e.pos + new Vector3(-0.4f, 0, 0.2f), 35f);

                bool wasBoss = e.kind == EnemyKind.Boss;
                Destroy(e.root);
                enemies.Remove(e);

                if (wasBoss)
                {
                    bossDefeated = true;
                    totalGold += runGold + 150;
                    bestFloor = Mathf.Max(bestFloor, currentFloor + 1);
                    PlayerPrefs.SetInt("DS_TotalGold", totalGold);
                    PlayerPrefs.SetInt("DS_BestFloor", bestFloor);
                    PlayerPrefs.Save();
                    PlaySfx(SfxId.Victory);
                    screen = GameScreen.Victory;
                }
            }
        }

        private void DamageProp(DungeonProp p, float dmg)
        {
            p.hp -= dmg;
            p.hitFlash = 0.12f;
            if (p.hp <= 0f)
            {
                if (p.kind == PropKind.Cage)
                {
                    if (heroes.Count < 5)
                    {
                        SpawnHero(p.captiveClass, p.pos);
                        AddFloatText(p.pos + Vector3.up * 1.8f, "동료 구출 성공! (+1 영웅)", new Color(0.35f, 1f, 0.65f), true);
                    }
                    else
                    {
                        foreach (HeroMember h in heroes) h.hp = h.maxHp;
                        AddFloatText(p.pos + Vector3.up * 1.8f, "전원 완치 & 보너스 골드!", new Color(1f, 0.85f, 0.25f), true);
                        SpawnPickup(PickupKind.Coin, p.pos, 40f);
                    }
                    PlaySfx(SfxId.Rescue);
                    TriggerShake(0.2f, 0.2f);
                }
                else if (p.kind == PropKind.Chest)
                {
                    for (int k = 0; k < 4; k++) SpawnPickup(PickupKind.Coin, p.pos + new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 0, UnityEngine.Random.Range(-0.8f, 0.8f)), 15f);
                    SpawnPickup(PickupKind.Gem, p.pos, 22f);
                    SpawnPickup(PickupKind.Heart, p.pos + Vector3.right * 0.5f, 45f);
                    AddFloatText(p.pos + Vector3.up * 1.6f, "보물 상자 개봉!", new Color(1f, 0.85f, 0.25f), false);
                    PlaySfx(SfxId.Pickup);
                }
                else
                {
                    SpawnPickup(PickupKind.Coin, p.pos, 10f);
                    if (UnityEngine.Random.value < 0.4f) SpawnPickup(PickupKind.Heart, p.pos, 25f);
                    PlaySfx(SfxId.Slash);
                }
                Destroy(p.root);
                props.Remove(p);
            }
        }

        private void DamageHero(HeroMember h, float dmg)
        {
            h.hp -= dmg;
            h.hitFlash = 0.16f;
            if (showDmgText)
                AddFloatText(h.pos + Vector3.up * 1.4f, "-" + Mathf.RoundToInt(dmg), new Color(1f, 0.32f, 0.32f), false);
            PlaySfx(SfxId.Hurt);

            if (h.hp <= 0f)
            {
                AddFloatText(h.pos + Vector3.up * 1.6f, h.name + " 쓰러짐!", new Color(1f, 0.2f, 0.2f), true);
                Destroy(h.root);
                heroes.Remove(h);
                if (heroes.Count == 0)
                {
                    totalGold += runGold;
                    PlayerPrefs.SetInt("DS_TotalGold", totalGold);
                    PlayerPrefs.Save();
                    screen = GameScreen.Defeat;
                }
            }
        }

        private void SpawnPickup(PickupKind kind, Vector3 pos, float val)
        {
            Renderer r;
            string key = kind == PickupKind.Coin ? "fx_coin" : (kind == PickupKind.Heart ? "fx_heart" : "fx_gem");
            GameObject go = CreateBillboard(key, pos, 0.72f, out r);
            pickups.Add(new PickupItem { kind = kind, root = go, pos = pos, value = val, bobOffset = UnityEngine.Random.Range(0f, 6f) });
        }

        private void UpdatePickups(float dt)
        {
            if (heroes.Count == 0) return;
            Vector3 leader = heroes[0].pos;
            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                PickupItem p = pickups[i];
                float d = Vector3.Distance(leader, p.pos);
                if (d < 4.2f)
                {
                    p.pos = Vector3.MoveTowards(p.pos, leader, dt * 11.5f);
                }
                p.root.transform.position = p.pos + Vector3.up * (0.25f + Mathf.Sin(Time.time * 5f + p.bobOffset) * 0.1f);

                if (d < 1.15f)
                {
                    if (p.kind == PickupKind.Coin)
                    {
                        runGold += Mathf.RoundToInt(p.value);
                        AddFloatText(leader + Vector3.up * 1.4f, "+" + Mathf.RoundToInt(p.value) + "G", new Color(1f, 0.88f, 0.25f), false);
                    }
                    else if (p.kind == PickupKind.Heart)
                    {
                        foreach (HeroMember h in heroes) h.hp = Mathf.Min(h.maxHp, h.hp + p.value);
                        AddFloatText(leader + Vector3.up * 1.5f, "+체력 회복", new Color(0.45f, 1f, 0.55f), false);
                    }
                    else
                    {
                        squadXp += p.value;
                        if (squadXp >= nextXp)
                        {
                            squadXp -= nextXp;
                            squadLevel++;
                            nextXp = Mathf.Round(nextXp * 1.38f + 12f);
                            OpenLevelUpOffers();
                        }
                    }
                    PlaySfx(SfxId.Pickup);
                    Destroy(p.root);
                    pickups.RemoveAt(i);
                }
            }
        }

        private void OpenLevelUpOffers()
        {
            List<UpgradeOffer> pool = new List<UpgradeOffer>
            {
                new UpgradeOffer { id = UpgradeId.Whirlwind, title = "회오리 검기", badge = "범위 회전 공격", desc = "원정대 주위를 도는 회전 칼날로 적을 자동 타격합니다.", accent = new Color(0.35f, 0.85f, 1f) },
                new UpgradeOffer { id = UpgradeId.ChainFire, title = "연쇄 화염구", badge = "자동 폭발 탄환", desc = "가까운 적에게 폭발하는 파이어볼을 주기적으로 발사합니다.", accent = new Color(1f, 0.55f, 0.20f) },
                new UpgradeOffer { id = UpgradeId.Thunder, title = "천둥 낙뢰", badge = "강적 단일 폭격", desc = "체력이 가장 높은 적에게 강력한 번개를 내리칩니다.", accent = new Color(0.55f, 0.72f, 1f) },
                new UpgradeOffer { id = UpgradeId.Haste, title = "전장의 질주", badge = "공속·이속 +22%", desc = "모든 영웅의 공격 속도와 이동 속도가 크게 빨라집니다.", accent = new Color(0.35f, 1f, 0.65f) },
                new UpgradeOffer { id = UpgradeId.SquadPower, title = "불굴의 원정대", badge = "공격·체력 +25% & 완치", desc = "전 영웅 공격력과 최대 체력이 증가하고 체력을 회복합니다.", accent = new Color(1f, 0.82f, 0.28f) },
                new UpgradeOffer { id = UpgradeId.Vampiric, title = "흡혈의 룬", badge = "처치 시 HP 4% 회복", desc = "몬스터를 쓰러뜨릴 때마다 선봉 영웅의 체력이 회복됩니다.", accent = new Color(0.95f, 0.35f, 0.52f) },
            };

            for (int i = 0; i < 3; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                currentOffers[i] = pool[idx];
                pool.RemoveAt(idx);
            }
            PlaySfx(SfxId.LevelUp);
            screen = GameScreen.LevelUp;
        }

        private void ApplyUpgrade(UpgradeOffer offer)
        {
            switch (offer.id)
            {
                case UpgradeId.Whirlwind: rankWhirlwind++; break;
                case UpgradeId.ChainFire: rankChainFire++; break;
                case UpgradeId.Thunder: rankThunder++; break;
                case UpgradeId.Haste: multHaste += 0.22f; break;
                case UpgradeId.SquadPower:
                    multDamage += 0.25f;
                    foreach (HeroMember h in heroes) { h.maxHp *= 1.25f; h.hp = h.maxHp; }
                    break;
                case UpgradeId.Vampiric: vampiricPct += 0.04f; break;
            }
            if (heroes.Count > 0)
                AddFloatText(heroes[0].pos + Vector3.up * 2f, offer.title + " 획득!", offer.accent, true);
            screen = GameScreen.Playing;
        }

        private void SpawnSlashEffect(Vector3 pos, float scale, Color col)
        {
            Renderer r;
            GameObject go = CreateBillboard("fx_slash", pos, scale, out r);
            if (r != null) r.sharedMaterial.SetColor("_BaseColor", col);
            effects.Add(new SlashEffect { root = go, timer = 0.18f, maxTime = 0.18f, startScale = Vector3.one * scale });
        }

        private void AddFloatText(Vector3 pos, string txt, Color col, bool big)
        {
            floatTexts.Add(new FloatingText { worldPos = pos, text = txt, color = col, timer = big ? 1.5f : 0.75f, maxTime = big ? 1.5f : 0.75f, big = big });
        }

        private void UpdateEffectsAndText(float dt)
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                effects[i].timer -= dt;
                if (effects[i].timer <= 0f)
                {
                    Destroy(effects[i].root);
                    effects.RemoveAt(i);
                }
            }
            for (int i = floatTexts.Count - 1; i >= 0; i--)
            {
                floatTexts[i].timer -= dt;
                floatTexts[i].worldPos += Vector3.up * dt * 1.4f;
                if (floatTexts[i].timer <= 0f) floatTexts.RemoveAt(i);
            }
        }

        private void UpdateSpriteAnimations()
        {
            foreach (HeroMember h in heroes)
            {
                if (h.spriteRend == null) continue;
                string key = GetHeroSpriteKey(h.hClass, animFrame);
                if (sprites.TryGetValue(key, out Texture2D tex)) h.spriteRend.sharedMaterial.SetTexture("_MainTex", tex);
                h.spriteRend.sharedMaterial.SetColor("_BaseColor", h.hitFlash > 0f ? new Color(1f, 0.35f, 0.35f) : Color.white);
                Vector3 sc = h.spriteRend.transform.localScale;
                sc.x = Mathf.Abs(sc.x) * (h.facingRight ? 1f : -1f);
                h.spriteRend.transform.localScale = sc;
            }
            foreach (EnemyUnit e in enemies)
            {
                if (e.spriteRend == null) continue;
                string key = GetEnemySpriteKey(e.kind, animFrame);
                if (sprites.TryGetValue(key, out Texture2D tex)) e.spriteRend.sharedMaterial.SetTexture("_MainTex", tex);
                e.spriteRend.sharedMaterial.SetColor("_BaseColor", e.hitFlash > 0f ? new Color(1f, 0.25f, 0.25f) : Color.white);
                Vector3 sc = e.spriteRend.transform.localScale;
                sc.x = Mathf.Abs(sc.x) * (e.facingRight ? 1f : -1f);
                e.spriteRend.transform.localScale = sc;
            }
            foreach (DungeonProp p in props)
            {
                if (p.spriteRend != null)
                    p.spriteRend.sharedMaterial.SetColor("_BaseColor", p.hitFlash > 0f ? new Color(1f, 0.65f, 0.35f) : Color.white);
                if (p.hitFlash > 0f) p.hitFlash -= Time.deltaTime;
            }
        }

        private void UpdateCamera(float dt)
        {
            if (mainCam == null || heroes.Count == 0) return;
            Vector3 tgt = heroes[0].pos + new Vector3(0f, 13.5f, -10.5f);
            Vector3 shake = Vector3.zero;
            if (shakeTimer > 0f)
            {
                shake = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0f) * shakePower;
            }
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, tgt, dt * 7f) + shake;
            mainCam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
        }

        private void TriggerShake(float time, float power)
        {
            shakeTimer = Mathf.Max(shakeTimer, time);
            shakePower = Mathf.Max(shakePower, power);
        }

        private Vector3 ClampInsideArena(Vector3 p, float rad)
        {
            p.x = Mathf.Clamp(p.x, -ArenaHalf + rad, ArenaHalf - rad);
            p.z = Mathf.Clamp(p.z, -ArenaHalf + rad, ArenaHalf - rad);
            foreach (Vector3 pil in pillarPositions)
            {
                Vector3 d = p - pil;
                d.y = 0f;
                float minD = 0.8f + rad;
                if (d.magnitude < minD && d.magnitude > 0.001f)
                    p = pil + d.normalized * minD;
            }
            p.y = 0f;
            return p;
        }

        private EnemyUnit FindNearestEnemy(Vector3 p)
        {
            EnemyUnit best = null;
            float bestD = 999f;
            foreach (EnemyUnit e in enemies)
            {
                float d = Vector3.Distance(p, e.pos);
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        private DungeonProp FindNearestProp(Vector3 p)
        {
            DungeonProp best = null;
            float bestD = 999f;
            foreach (DungeonProp pr in props)
            {
                float d = Vector3.Distance(p, pr.pos);
                if (d < bestD) { bestD = d; best = pr; }
            }
            return best;
        }

        private HeroMember FindNearestHero(Vector3 p)
        {
            HeroMember best = null;
            float bestD = 999f;
            foreach (HeroMember h in heroes)
            {
                float d = Vector3.Distance(p, h.pos);
                if (d < bestD) { bestD = d; best = h; }
            }
            return best;
        }

        private float GetSquadHpRatio()
        {
            if (heroes.Count == 0) return 0f;
            float cur = 0f, max = 0f;
            foreach (HeroMember h in heroes) { cur += h.hp; max += h.maxHp; }
            return max > 0f ? cur / max : 0f;
        }

        // ================= GUI (750 x 1334 Portrait Virtual Canvas) =================
        private void OnGUI()
        {
            if (uiFont != null) GUI.skin.font = uiFont;
            float rx = Screen.width / 750f;
            float ry = Screen.height / 1334f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(rx, ry, 1f));

            switch (screen)
            {
                case GameScreen.Title: DrawTitle(); break;
                case GameScreen.Playing: DrawHud(); break;
                case GameScreen.LevelUp: DrawHud(); DrawLevelUpModal(); break;
                case GameScreen.Codex: DrawCodex(); break;
                case GameScreen.Settings: DrawSettings(); break;
                case GameScreen.Victory: DrawHud(); DrawEndModal(true); break;
                case GameScreen.Defeat: DrawHud(); DrawEndModal(false); break;
            }
            DrawWorldBarsAndTexts(rx, ry);
        }

        private GUIStyle LabelStyle(int size, Color col, TextAnchor align = TextAnchor.MiddleCenter, bool bold = true)
        {
            GUIStyle s = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = align,
                wordWrap = true
            };
            if (uiFont != null) s.font = uiFont;
            s.normal.textColor = col;
            return s;
        }

        private GUIStyle BtnStyle(int size, Texture2D bg, Color col)
        {
            GUIStyle s = new GUIStyle(GUI.skin.button)
            {
                fontSize = size,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            if (uiFont != null) s.font = uiFont;
            s.normal.background = bg;
            s.hover.background = bg;
            s.active.background = bg;
            s.normal.textColor = col;
            s.hover.textColor = col;
            s.active.textColor = col;
            return s;
        }

        private void DrawTitle()
        {
            GUI.DrawTexture(new Rect(40, 100, 670, 260), texHeaderPanel);
            GUI.Label(new Rect(55, 128, 640, 68), "우르릉 던전 원정대", LabelStyle(48, new Color(1f, 0.88f, 0.35f)));
            GUI.Label(new Rect(55, 198, 640, 36), "DUNGEON SLASHER · v0.1.0", LabelStyle(20, new Color(0.50f, 0.85f, 1f)));
            GUI.Label(new Rect(55, 244, 640, 82), "감옥의 동료를 구출해 군집 원정대를 꾸리고\n심연의 마왕 발록을 토벌하라! (한 손 조이스틱 + AUTO)", LabelStyle(20, new Color(0.90f, 0.93f, 0.98f), TextAnchor.MiddleCenter, false));

            GUI.DrawTexture(new Rect(110, 385, 530, 74), texDarkPanel);
            GUI.Label(new Rect(120, 392, 510, 60), "최고 도달 층: " + bestFloor + "층   |   보유 골드: " + totalGold + " G", LabelStyle(22, new Color(1f, 0.85f, 0.35f)));

            if (GUI.Button(new Rect(85, 840, 580, 96), "⚔️  던전 출정하기 (" + bestFloor + "층 도전)", BtnStyle(28, texBtnGold, Color.white)))
            {
                StartRun(bestFloor);
            }
            if (GUI.Button(new Rect(85, 960, 580, 76), "📜  원정대 & 몬스터 도감", BtnStyle(24, texBtnDark, Color.white)))
            {
                screen = GameScreen.Codex;
                PlaySfx(SfxId.Pickup);
            }
            if (GUI.Button(new Rect(85, 1056, 580, 76), "⚙️  게임 설정", BtnStyle(24, texBtnDark, Color.white)))
            {
                screen = GameScreen.Settings;
                PlaySfx(SfxId.Pickup);
            }
        }

        private void DrawHud()
        {
            // Top Status Panel
            GUI.DrawTexture(new Rect(20, 48, 710, 128), texDarkPanel);

            // Floor & Wave & Squad Info
            string waveStr = bossSpawned ? "BOSS 결전!" : ("WAVE " + waveIndex + "/3");
            GUI.Label(new Rect(38, 56, 360, 34), "제 " + currentFloor + "층 · " + waveStr + "   영웅 " + heroes.Count + "/5", LabelStyle(20, Color.white, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(38, 92, 260, 30), "LV." + squadLevel + "   처치 " + kills + "   💰 " + runGold + "G", LabelStyle(18, new Color(1f, 0.86f, 0.35f), TextAnchor.MiddleLeft));

            // XP Progress Bar
            GUI.DrawTexture(new Rect(38, 134, 460, 22), texBarBg);
            GUI.DrawTexture(new Rect(38, 134, 460 * Mathf.Clamp01(squadXp / nextXp), 22), texXpCyan);
            GUI.Label(new Rect(38, 132, 460, 26), "EXP " + Mathf.FloorToInt(squadXp) + " / " + Mathf.FloorToInt(nextXp), LabelStyle(14, Color.white));

            // Smart Hybrid AUTO Toggle Button
            Texture2D autoTex = autoMode ? texBtnAutoOn : texBtnAutoOff;
            string autoLabel = autoMode ? "🤖 AUTO ON" : "🕹️ 수동 조작";
            if (GUI.Button(new Rect(520, 62, 192, 56), autoLabel, BtnStyle(20, autoTex, Color.white)))
            {
                autoMode = !autoMode;
                PlaySfx(SfxId.Pickup);
                if (heroes.Count > 0)
                    AddFloatText(heroes[0].pos + Vector3.up * 1.8f, autoMode ? "자동 전투 ON" : "수동 조작 전환", Color.cyan, false);
            }

            // Pause / Lobby small button
            if (GUI.Button(new Rect(520, 126, 192, 38), "🏠 로비 복귀", BtnStyle(16, texBtnDark, new Color(0.85f, 0.9f, 1f))))
            {
                StartTitleDiorama();
            }

            // Boss HP Bar if Boss is alive
            EnemyUnit boss = null;
            foreach (EnemyUnit e in enemies) if (e.kind == EnemyKind.Boss) { boss = e; break; }
            if (boss != null)
            {
                GUI.DrawTexture(new Rect(60, 192, 630, 52), texDarkPanel);
                GUI.DrawTexture(new Rect(74, 218, 602, 18), texBarBg);
                GUI.DrawTexture(new Rect(74, 218, 602 * Mathf.Clamp01(boss.hp / boss.maxHp), 18), texHpRed);
                GUI.Label(new Rect(74, 193, 602, 26), "👹 " + boss.name + "  (" + Mathf.CeilToInt(boss.hp) + " / " + Mathf.CeilToInt(boss.maxHp) + ")", LabelStyle(16, new Color(1f, 0.72f, 0.72f)));
            }

            // Bottom Squad HP Summary & Ultimate Button
            GUI.DrawTexture(new Rect(20, 1175, 500, 108), texDarkPanel);
            float hpPct = GetSquadHpRatio();
            GUI.Label(new Rect(38, 1184, 464, 30), "원정대 총 체력  " + Mathf.RoundToInt(hpPct * 100f) + "%   |   모드: " + (draggingJoy ? "직접 이동 중" : (autoMode ? "자동 탐험" : "대기")), LabelStyle(18, Color.white, TextAnchor.MiddleLeft));
            GUI.DrawTexture(new Rect(38, 1222, 464, 26), texBarBg);
            GUI.DrawTexture(new Rect(38, 1222, 464 * hpPct, 26), texHpGreen);
            GUI.Label(new Rect(38, 1254, 464, 24), "화면 아래를 드래그하면 즉시 수동 이동합니다", LabelStyle(14, new Color(0.72f, 0.80f, 0.92f), TextAnchor.MiddleLeft, false));

            // Ultimate Button
            bool ready = ultCharge >= 100f;
            if (GUI.Button(new Rect(536, 1175, 194, 108), ready ? "⚡ 일제 돌격\n[발동!]" : ("⚡ 필살기\n" + Mathf.FloorToInt(ultCharge) + "%"), BtnStyle(20, ready ? texBtnGold : texBtnDark, ready ? Color.white : new Color(0.75f, 0.8f, 0.9f))))
            {
                TriggerUltimate();
            }

            // Floating Virtual Joystick visual
            if (draggingJoy)
            {
                GUI.DrawTexture(new Rect(joyOriginGui.x - 85f, joyOriginGui.y - 85f, 170f, 170f), texJoyRing);
                GUI.DrawTexture(new Rect(joyCurrentGui.x - 42f, joyCurrentGui.y - 42f, 84f, 84f), texJoyKnob);
            }
        }

        private void DrawLevelUpModal()
        {
            GUI.DrawTexture(new Rect(0, 0, 750, 1334), texDimOverlay);
            GUI.DrawTexture(new Rect(45, 190, 660, 920), texHeaderPanel);
            GUI.Label(new Rect(60, 215, 630, 56), "✨ 원정대 레벨 업! (LV." + squadLevel + ")", LabelStyle(34, new Color(1f, 0.88f, 0.35f)));
            GUI.Label(new Rect(60, 272, 630, 32), "원하는 스킬 1종을 선택하세요", LabelStyle(20, new Color(0.85f, 0.92f, 1f), TextAnchor.MiddleCenter, false));

            for (int i = 0; i < 3; i++)
            {
                UpgradeOffer of = currentOffers[i];
                if (of == null) continue;
                float y = 335 + i * 245;
                GUI.DrawTexture(new Rect(75, y, 600, 215), texCardBg);
                GUI.Label(new Rect(100, y + 18, 550, 40), of.title, LabelStyle(28, of.accent, TextAnchor.MiddleLeft));
                GUI.Label(new Rect(100, y + 60, 550, 30), "[" + of.badge + "]", LabelStyle(18, new Color(0.95f, 0.95f, 1f), TextAnchor.MiddleLeft));
                GUI.Label(new Rect(100, y + 92, 550, 56), of.desc, LabelStyle(18, new Color(0.80f, 0.85f, 0.95f), TextAnchor.MiddleLeft, false));

                if (GUI.Button(new Rect(445, y + 152, 210, 48), "선택하기", BtnStyle(20, texBtnGold, Color.white)))
                {
                    ApplyUpgrade(of);
                }
            }
        }

        private void DrawCodex()
        {
            GUI.DrawTexture(new Rect(35, 70, 680, 1180), texDarkPanel);
            GUI.Label(new Rect(55, 95, 640, 50), "📜 원정대 영웅 & 던전 도감", LabelStyle(32, new Color(1f, 0.88f, 0.35f)));

            string[] entries = {
                "🛡️ 기사 아서  |  근접 범위 검기 · 높은 체력으로 선봉 유지",
                "🏹 궁수 레나  |  빠른 관통 화살 · 원거리 집중 사격",
                "🔮 마법사 루나 |  감옥 구출 합류 · 광역 폭발 화염구",
                "✨ 사제 세라  |  감옥 구출 합류 · 아군 전원 체력 치유",
                "👾 몬스터군  |  슬라임·고블린·해골궁수·오크광전사",
                "👹 마왕 발록  |  웨이브 3 보스 · 바닥 장판 예고 폭발 + 탄막"
            };
            for (int i = 0; i < entries.Length; i++)
            {
                float y = 175 + i * 135;
                GUI.DrawTexture(new Rect(60, y, 630, 112), texCardBg);
                GUI.Label(new Rect(85, y + 16, 580, 80), entries[i], LabelStyle(20, Color.white, TextAnchor.MiddleLeft));
            }

            if (GUI.Button(new Rect(150, 1130, 450, 76), "⬅️  타이틀로 돌아가기", BtnStyle(24, texBtnGold, Color.white)))
            {
                screen = GameScreen.Title;
            }
        }

        private void DrawSettings()
        {
            GUI.DrawTexture(new Rect(45, 180, 660, 860), texDarkPanel);
            GUI.Label(new Rect(65, 215, 620, 54), "⚙️ 게임 환경 설정", LabelStyle(34, new Color(1f, 0.88f, 0.35f)));

            if (GUI.Button(new Rect(95, 340, 560, 84), "효과음 (8비트 SFX): " + (sfxEnabled ? "켜짐 (ON)" : "꺼짐 (OFF)"), BtnStyle(22, sfxEnabled ? texBtnAutoOn : texBtnAutoOff, Color.white)))
            {
                sfxEnabled = !sfxEnabled;
                PlaySfx(SfxId.Pickup);
            }
            if (GUI.Button(new Rect(95, 455, 560, 84), "대미지 숫자 표기: " + (showDmgText ? "켜짐 (ON)" : "꺼짐 (OFF)"), BtnStyle(22, showDmgText ? texBtnAutoOn : texBtnAutoOff, Color.white)))
            {
                showDmgText = !showDmgText;
            }
            if (GUI.Button(new Rect(95, 570, 560, 84), "기본 자동 탐험(AUTO): " + (autoMode ? "켜짐 (ON)" : "꺼짐 (OFF)"), BtnStyle(22, autoMode ? texBtnAutoOn : texBtnAutoOff, Color.white)))
            {
                autoMode = !autoMode;
            }

            if (GUI.Button(new Rect(150, 910, 450, 80), "⬅️  돌아가기", BtnStyle(24, texBtnGold, Color.white)))
            {
                screen = GameScreen.Title;
            }
        }

        private void DrawEndModal(bool win)
        {
            GUI.DrawTexture(new Rect(0, 0, 750, 1334), texDimOverlay);
            GUI.DrawTexture(new Rect(60, 320, 630, 620), texHeaderPanel);
            GUI.Label(new Rect(80, 355, 590, 68), win ? "🏆 던전 토벌 성공!" : "💀 원정대 전멸...", LabelStyle(40, win ? new Color(1f, 0.86f, 0.30f) : new Color(1f, 0.35f, 0.35f)));
            GUI.Label(new Rect(80, 455, 590, 150),
                "도달 층수: 제 " + currentFloor + "층\n몬스터 처치: " + kills + "마리\n획득 골드: +" + (runGold + (win ? 150 : 0)) + " G",
                LabelStyle(24, Color.white));

            if (win)
            {
                if (GUI.Button(new Rect(105, 650, 540, 84), "🔥 다음 층 출정 (제 " + (currentFloor + 1) + "층)", BtnStyle(24, texBtnGold, Color.white)))
                {
                    StartRun(currentFloor + 1);
                }
            }
            else
            {
                if (GUI.Button(new Rect(105, 650, 540, 84), "🔄 제 " + currentFloor + "층 다시 도전", BtnStyle(24, texBtnGold, Color.white)))
                {
                    StartRun(currentFloor);
                }
            }

            if (GUI.Button(new Rect(105, 760, 540, 72), "🏠 타이틀 로비로", BtnStyle(22, texBtnDark, Color.white)))
            {
                StartTitleDiorama();
            }
        }

        private void DrawWorldBarsAndTexts(float rx, float ry)
        {
            if (mainCam == null) return;

            // Hero & Enemy & Cage mini HP bars
            foreach (HeroMember h in heroes)
            {
                DrawWorldBar(h.pos + Vector3.up * 1.35f, h.hp / h.maxHp, texHpGreen, 54f, rx, ry);
            }
            foreach (EnemyUnit e in enemies)
            {
                if (e.hp < e.maxHp)
                    DrawWorldBar(e.pos + Vector3.up * (e.kind == EnemyKind.Boss ? 2.6f : 1.3f), e.hp / e.maxHp, texHpRed, e.kind == EnemyKind.Boss ? 96f : 50f, rx, ry);
            }
            foreach (DungeonProp p in props)
            {
                if (p.kind == PropKind.Cage)
                {
                    DrawWorldBar(p.pos + Vector3.up * 1.75f, p.hp / p.maxHp, texUltOrange, 64f, rx, ry);
                    Vector3 sp = mainCam.WorldToScreenPoint(p.pos + Vector3.up * 2.15f);
                    if (sp.z > 0f)
                    {
                        float gx = sp.x / rx;
                        float gy = (Screen.height - sp.y) / ry;
                        GUI.Label(new Rect(gx - 60f, gy - 16f, 120f, 32f), "구출!", LabelStyle(16, new Color(1f, 0.92f, 0.35f)));
                    }
                }
            }

            // Floating Combat Texts
            foreach (FloatingText ft in floatTexts)
            {
                Vector3 sp = mainCam.WorldToScreenPoint(ft.worldPos);
                if (sp.z <= 0f) continue;
                float gx = sp.x / rx;
                float gy = (Screen.height - sp.y) / ry;
                GUI.Label(new Rect(gx - 180f, gy - 22f, 360f, 44f), ft.text, LabelStyle(ft.big ? 28 : 20, ft.color));
            }
        }

        private void DrawWorldBar(Vector3 worldPos, float pct, Texture2D fillTex, float w, float rx, float ry)
        {
            Vector3 sp = mainCam.WorldToScreenPoint(worldPos);
            if (sp.z <= 0f) return;
            float gx = sp.x / rx;
            float gy = (Screen.height - sp.y) / ry;
            GUI.DrawTexture(new Rect(gx - w * 0.5f - 1f, gy - 1f, w + 2f, 9f), texBarBg);
            GUI.DrawTexture(new Rect(gx - w * 0.5f, gy, w * Mathf.Clamp01(pct), 7f), fillTex);
        }
    }
}
