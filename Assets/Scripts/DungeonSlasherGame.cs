using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlasher
{
    public enum GameLang
    {
        KR,
        EN
    }

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
        public string nameKr;
        public string nameEn;
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
        public string nameKr;
        public string nameEn;
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
        public float radius;
        public float hitFlash;
        public bool facingRight = true;
    }

    [Serializable]
    public class DungeonProp
    {
        public PropKind kind;
        public GameObject root;
        public Renderer spriteRend;
        public Renderer captiveRend;
        public Vector3 pos;
        public float hp;
        public float maxHp;
        public HeroClass captiveHero;
        public float hitFlash;
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
        public string titleKr;
        public string titleEn;
        public string descKr;
        public string descEn;
        public string badgeKr;
        public string badgeEn;
        public Color accent;
    }

    public class DungeonSlasherGame : MonoBehaviour
    {
        private static DungeonSlasherGame instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            GameObject go = new GameObject("GoinRaidGameManager");
            instance = go.AddComponent<DungeonSlasherGame>();
            DontDestroyOnLoad(go);
        }

        // Language & State
        public static GameLang curLang = GameLang.KR;
        public GameScreen screen = GameScreen.Title;
        public int currentFloor = 1;
        public int bestFloor = 1;
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
        public int kills = 0;
        public float ultCharge = 35f;
        public float hasteBuffTimer = 0f;

        // Upgrade Ranks
        public int rankWhirlwind = 0;
        public int rankChainFire = 0;
        public int rankThunder = 0;
        public float multHaste = 1f;
        public float multDamage = 1f;
        public float vampiricPct = 0f;

        private float whirlwindTimer = 0f;
        private float thunderTimer = 0f;
        private GameObject whirlwindOrbRoot;

        // Audio
        private AudioSource audioSrc;
        private bool sfxEnabled = true;
        private bool showDmgText = true;

        // Camera & Shake
        private Camera mainCam;
        private float shakeTimer = 0f;
        private float shakePower = 0f;
        private const float ArenaHalf = 13.5f;

        // Input / Mobile Virtual Joystick
        private bool draggingJoy = false;
        private Vector2 joyOriginGui = new Vector2(180f, 1080f);
        private Vector2 joyCurrentGui = new Vector2(180f, 1080f);
        private Vector2 manualDir = Vector2.zero;

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

        private string Loc(string kr, string en)
        {
            return curLang == GameLang.KR ? kr : en;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Time.timeScale = 1f;
            curLang = PlayerPrefs.GetInt("GR_Language", 0) == 0 ? GameLang.KR : GameLang.EN;
            totalGold = PlayerPrefs.GetInt("GR_TotalGold", 0);
            bestFloor = PlayerPrefs.GetInt("GR_BestFloor", 1);
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
            mainCam.fieldOfView = 48f;

            Light sun = FindAnyObjectByType<Light>();
            if (sun == null)
            {
                GameObject lg = new GameObject("DungeonLight");
                sun = lg.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(55f, -20f, 0f);
            sun.color = new Color(0.88f, 0.90f, 1.0f);
            sun.intensity = 1.15f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.54f);
        }

        private void LoadResourcesAndShaders()
        {
            // Load Galmuri11 TrueType font
            uiFont = Resources.Load<Font>("Fonts/Galmuri11");

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
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                matSolidUnlit = new Material(sh);
            }

            matLit = Resources.Load<Material>("Materials/OfficeLit");
            if (matLit == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
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
                    t.wrapMode = (nm == "tile_floor") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                    sprites[nm] = t;
                }
            }

            GameObject tmpQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadMesh = tmpQuad.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmpQuad);
        }

        private void BuildUiTextures()
        {
            texDarkPanel = MakeBox(64, 64, new Color(0.08f, 0.10f, 0.16f, 0.94f), new Color(0.32f, 0.38f, 0.52f, 0.95f), 2);
            texHeaderPanel = MakeBox(64, 64, new Color(0.12f, 0.14f, 0.22f, 0.96f), new Color(0.96f, 0.74f, 0.22f, 0.98f), 3);
            texBtnGold = MakeBox(64, 64, new Color(0.88f, 0.52f, 0.12f, 1f), new Color(1f, 0.88f, 0.38f, 1f), 3);
            texBtnDark = MakeBox(64, 64, new Color(0.18f, 0.22f, 0.32f, 0.96f), new Color(0.42f, 0.50f, 0.68f, 1f), 2);
            texBtnAutoOn = MakeBox(64, 64, new Color(0.10f, 0.68f, 0.42f, 1f), new Color(0.48f, 1f, 0.72f, 1f), 2);
            texBtnAutoOff = MakeBox(64, 64, new Color(0.24f, 0.26f, 0.34f, 0.95f), new Color(0.58f, 0.62f, 0.72f, 1f), 2);
            texBarBg = MakeSolid(new Color(0.04f, 0.05f, 0.08f, 0.96f));
            texHpGreen = MakeSolid(new Color(0.16f, 0.88f, 0.44f, 1f));
            texHpRed = MakeSolid(new Color(0.94f, 0.22f, 0.24f, 1f));
            texXpCyan = MakeSolid(new Color(0.20f, 0.82f, 1.0f, 1f));
            texUltOrange = MakeSolid(new Color(1.0f, 0.64f, 0.16f, 1f));
            texCardBg = MakeBox(64, 64, new Color(0.14f, 0.17f, 0.26f, 0.98f), new Color(0.55f, 0.75f, 1.0f, 1f), 3);
            texJoyRing = MakeCircle(110, new Color(1f, 1f, 1f, 0.12f), new Color(0.45f, 0.85f, 1f, 0.65f), 4);
            texJoyKnob = MakeCircle(64, new Color(0.35f, 0.82f, 1f, 0.82f), new Color(1f, 1f, 1f, 0.95f), 2);
            texDimOverlay = MakeSolid(new Color(0f, 0f, 0f, 0.72f));
        }

        private Texture2D MakeSolid(Color c)
        {
            Texture2D t = new Texture2D(2, 2);
            t.SetPixels(new[] { c, c, c, c });
            t.Apply();
            return t;
        }

        private Texture2D MakeBox(int w, int h, Color fill, Color border, int bWidth)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] cols = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x < bWidth || x >= w - bWidth || y < bWidth || y >= h - bWidth);
                    cols[y * w + x] = isBorder ? border : fill;
                }
            }
            t.SetPixels(cols);
            t.Apply();
            return t;
        }

        private Texture2D MakeCircle(int size, Color fill, Color border, int bWidth)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] cols = new Color[size * size];
            float r = size * 0.5f;
            float rInner = r - bWidth;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    if (d > r) cols[y * size + x] = Color.clear;
                    else if (d > rInner) cols[y * size + x] = border;
                    else cols[y * size + x] = fill;
                }
            }
            t.SetPixels(cols);
            t.Apply();
            return t;
        }

        private void BuildAudio()
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
        }

        private void PlaySfx(SfxId sfx)
        {
            if (!sfxEnabled || audioSrc == null) return;
            AudioClip clip = CreateSynthSfx(sfx);
            if (clip != null) audioSrc.PlayOneShot(clip, 0.72f);
        }

        private AudioClip CreateSynthSfx(SfxId sfx)
        {
            int rate = 22050;
            float len = 0.14f;
            switch (sfx)
            {
                case SfxId.Slash: len = 0.12f; break;
                case SfxId.Bow: len = 0.10f; break;
                case SfxId.Boom: len = 0.28f; break;
                case SfxId.Heal: len = 0.24f; break;
                case SfxId.Pickup: len = 0.10f; break;
                case SfxId.Rescue: len = 0.35f; break;
                case SfxId.LevelUp: len = 0.42f; break;
                case SfxId.Hurt: len = 0.16f; break;
                case SfxId.Victory: len = 0.55f; break;
            }

            int count = (int)(rate * len);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float env = 1f - (float)i / count;
                float freq = 440f;
                float val = 0f;

                switch (sfx)
                {
                    case SfxId.Slash:
                        freq = Mathf.Lerp(800f, 180f, t / len);
                        val = (Mathf.Sin(2 * Mathf.PI * freq * t) > 0f ? 0.35f : -0.35f) * env;
                        break;
                    case SfxId.Bow:
                        freq = Mathf.Lerp(420f, 750f, t / len);
                        val = Mathf.Sin(2 * Mathf.PI * freq * t) * env;
                        break;
                    case SfxId.Boom:
                        val = (UnityEngine.Random.value * 2f - 1f) * env * 0.65f;
                        break;
                    case SfxId.Heal:
                        freq = 520f + Mathf.Sin(t * 35f) * 120f;
                        val = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.45f;
                        break;
                    case SfxId.Pickup:
                        freq = Mathf.Lerp(580f, 1120f, t / len);
                        val = Mathf.Sin(2 * Mathf.PI * freq * t) * env;
                        break;
                    case SfxId.Rescue:
                    case SfxId.LevelUp:
                        freq = (t < len * 0.33f) ? 523.25f : ((t < len * 0.66f) ? 659.25f : 783.99f);
                        val = (Mathf.Sin(2 * Mathf.PI * freq * t) > 0f ? 0.3f : -0.3f) * env;
                        break;
                    case SfxId.Hurt:
                        freq = Mathf.Lerp(220f, 80f, t / len);
                        val = (Mathf.Sin(2 * Mathf.PI * freq * t) > 0f ? 0.4f : -0.4f) * env;
                        break;
                    case SfxId.Victory:
                        freq = (t < len * 0.25f) ? 523f : ((t < len * 0.5f) ? 659f : ((t < len * 0.75f) ? 784f : 1046f));
                        val = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.45f;
                        break;
                }
                samples[i] = Mathf.Clamp(val, -1f, 1f);
            }

            AudioClip c = AudioClip.Create("sfx_" + sfx, count, 1, rate, false);
            c.SetData(samples, 0);
            return c;
        }

        private void BuildDungeonArena()
        {
            if (worldRoot != null) Destroy(worldRoot);
            worldRoot = new GameObject("DungeonWorldRoot");
            entitiesRoot = new GameObject("DungeonEntities");
            entitiesRoot.transform.SetParent(worldRoot.transform);

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
            fMat.SetTextureScale("_MainTex", new Vector2(14f, 14f));
            fr.sharedMaterial = fMat;

            // Surrounding Stone Dungeon Walls
            Material wallMat = MakeSolidMat(new Color(0.18f, 0.20f, 0.28f));
            Material trimMat = MakeSolidMat(new Color(0.28f, 0.32f, 0.42f));
            float wDist = ArenaHalf + 0.8f;
            CreateBox(worldRoot.transform, new Vector3(0, 1.2f, wDist), new Vector3(ArenaHalf * 2.2f + 2f, 2.4f, 1.4f), wallMat);
            CreateBox(worldRoot.transform, new Vector3(0, 1.2f, -wDist), new Vector3(ArenaHalf * 2.2f + 2f, 2.4f, 1.4f), wallMat);
            CreateBox(worldRoot.transform, new Vector3(-wDist, 1.2f, 0), new Vector3(1.4f, 2.4f, ArenaHalf * 2.2f + 2f), wallMat);
            CreateBox(worldRoot.transform, new Vector3(wDist, 1.2f, 0), new Vector3(1.4f, 2.4f, ArenaHalf * 2.2f + 2f), wallMat);

            // Wall Torches with Warm Flickering Lights
            Material torchFlameMat = MakeSolidMat(new Color(1.0f, 0.60f, 0.16f));
            Vector3[] torchCoords = {
                new Vector3(-7f, 1.6f, wDist - 0.8f), new Vector3(7f, 1.6f, wDist - 0.8f),
                new Vector3(-7f, 1.6f, -wDist + 0.8f), new Vector3(7f, 1.6f, -wDist + 0.8f),
                new Vector3(-wDist + 0.8f, 1.6f, -7f), new Vector3(-wDist + 0.8f, 1.6f, 7f),
                new Vector3(wDist - 0.8f, 1.6f, -7f), new Vector3(wDist - 0.8f, 1.6f, 7f)
            };
            torchLights.Clear();
            foreach (Vector3 tc in torchCoords)
            {
                GameObject torch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Collider c = torch.GetComponent<Collider>(); if (c != null) Destroy(c);
                torch.transform.SetParent(worldRoot.transform);
                torch.transform.position = tc;
                torch.transform.localScale = Vector3.one * 0.42f;
                torch.GetComponent<Renderer>().sharedMaterial = torchFlameMat;

                GameObject lt = new GameObject("TorchLight");
                lt.transform.SetParent(torch.transform);
                lt.transform.localPosition = Vector3.zero;
                Light l = lt.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.65f, 0.22f);
                l.range = 8.5f;
                l.intensity = 1.65f;
                torchLights.Add(l);
            }

            // Dungeon Stone Pillars
            pillarPositions.Clear();
            Vector3[] pillars = {
                new Vector3(-6.5f, 0, -6.5f), new Vector3(6.5f, 0, -6.5f),
                new Vector3(-6.5f, 0, 6.5f), new Vector3(6.5f, 0, 6.5f)
            };
            foreach (Vector3 p in pillars)
            {
                pillarPositions.Add(p);
                CreateBox(worldRoot.transform, p + Vector3.up * 1.5f, new Vector3(1.3f, 3.0f, 1.3f), wallMat);
                CreateBox(worldRoot.transform, p + Vector3.up * 3.1f, new Vector3(1.6f, 0.4f, 1.6f), trimMat);
                CreateBox(worldRoot.transform, p + Vector3.up * 0.15f, new Vector3(1.6f, 0.3f, 1.6f), trimMat);
            }
        }

        private GameObject CreateBox(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Collider c = b.GetComponent<Collider>(); if (c != null) Destroy(c);
            b.transform.SetParent(parent);
            b.transform.position = pos;
            b.transform.localScale = scale;
            b.GetComponent<Renderer>().sharedMaterial = mat;
            return b;
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
            if (entitiesRoot != null)
            {
                foreach (Transform child in entitiesRoot.transform)
                {
                    Destroy(child.gameObject);
                }
            }
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
            ultCharge = 40f;
            hasteBuffTimer = 0f;

            rankWhirlwind = 0;
            rankChainFire = 0;
            rankThunder = 0;
            multHaste = 1f;
            multDamage = 1f;
            vampiricPct = 0f;

            // Start with Knight + Archer duo (instant 우르릉 squad feel!)
            SpawnHero(HeroClass.Knight, new Vector3(0f, 0f, -1.5f));
            SpawnHero(HeroClass.Archer, new Vector3(-1.2f, 0f, -2.8f));

            // Initial Dungeon Props: 2 Prison Cages + 3 Chests + Crates
            SpawnProp(PropKind.Cage, new Vector3(-6.5f, 0f, 3.0f), HeroClass.Mage);
            SpawnProp(PropKind.Cage, new Vector3(6.5f, 0f, 4.5f), HeroClass.Priest);
            SpawnProp(PropKind.Chest, new Vector3(-3.2f, 0f, 7.5f), HeroClass.Knight);
            SpawnProp(PropKind.Chest, new Vector3(3.5f, 0f, -7.0f), HeroClass.Knight);
            SpawnProp(PropKind.Crate, new Vector3(1.8f, 0f, 2.0f), HeroClass.Knight);
            SpawnProp(PropKind.Crate, new Vector3(-2.2f, 0f, -4.5f), HeroClass.Knight);

            // Initial Wave
            for (int i = 0; i < 5; i++)
            {
                SpawnEnemyNearEdge(i < 3 ? EnemyKind.Slime : EnemyKind.Goblin);
            }

            screen = GameScreen.Playing;
            AddFloatText(Vector3.zero + Vector3.up * 2f, Loc("제 " + currentFloor + "층 레이드 시작!", "FLOOR " + currentFloor + " RAID START!"), new Color(1f, 0.88f, 0.25f), true);
            PlaySfx(SfxId.Rescue);
        }

        private void SpawnHero(HeroClass hc, Vector3 pos)
        {
            HeroMember h = new HeroMember { hClass = hc, pos = pos };
            float scaleMult = 1f + (currentFloor - 1) * 0.15f;
            switch (hc)
            {
                case HeroClass.Knight:
                    h.nameKr = "기사 아서";
                    h.nameEn = "Knight Arthur";
                    h.maxHp = 280f * scaleMult;
                    h.attackDamage = 35f;
                    h.attackRange = 2.4f;
                    h.attackCooldown = 0.68f;
                    break;
                case HeroClass.Archer:
                    h.nameKr = "궁수 레나";
                    h.nameEn = "Archer Lena";
                    h.maxHp = 180f * scaleMult;
                    h.attackDamage = 26f;
                    h.attackRange = 8.5f;
                    h.attackCooldown = 0.52f;
                    break;
                case HeroClass.Mage:
                    h.nameKr = "마법사 루나";
                    h.nameEn = "Mage Luna";
                    h.maxHp = 165f * scaleMult;
                    h.attackDamage = 45f;
                    h.attackRange = 7.5f;
                    h.attackCooldown = 0.95f;
                    break;
                case HeroClass.Priest:
                    h.nameKr = "사제 세라";
                    h.nameEn = "Priestess Sera";
                    h.maxHp = 195f * scaleMult;
                    h.attackDamage = 24f;
                    h.attackRange = 6.8f;
                    h.attackCooldown = 1.10f;
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
                float dist = UnityEngine.Random.Range(6.5f, 11.0f);
                p = new Vector3(
                    Mathf.Clamp(leader.x + Mathf.Cos(ang) * dist, -ArenaHalf + 1.5f, ArenaHalf - 1.5f),
                    0f,
                    Mathf.Clamp(leader.z + Mathf.Sin(ang) * dist, -ArenaHalf + 1.5f, ArenaHalf - 1.5f)
                );
                if (Vector3.Distance(p, leader) > 5.0f) break;
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
                    e.nameKr = "초록 슬라임"; e.nameEn = "Green Slime"; e.maxHp = 45f * fm; e.speed = 2.8f; e.damage = 9f * fm;
                    e.attackRange = 1.35f; e.attackCooldown = 0.95f; e.radius = 0.6f;
                    break;
                case EnemyKind.Goblin:
                    e.nameKr = "고블린 자객"; e.nameEn = "Goblin Assassin"; e.maxHp = 65f * fm; e.speed = 3.6f; e.damage = 14f * fm;
                    e.attackRange = 1.4f; e.attackCooldown = 0.75f; e.radius = 0.65f;
                    break;
                case EnemyKind.Skeleton:
                    e.nameKr = "해골 궁수"; e.nameEn = "Skeleton Archer"; e.maxHp = 55f * fm; e.speed = 2.4f; e.damage = 16f * fm;
                    e.attackRange = 7.5f; e.attackCooldown = 1.35f; e.radius = 0.65f;
                    break;
                case EnemyKind.Orc:
                    e.nameKr = "오크 광전사"; e.nameEn = "Orc Berserker"; e.maxHp = 135f * fm; e.speed = 2.2f; e.damage = 25f * fm;
                    e.attackRange = 1.8f; e.attackCooldown = 1.25f; e.radius = 0.95f; size = 1.75f;
                    break;
                case EnemyKind.Boss:
                    e.nameKr = "심연의 마왕 발록"; e.nameEn = "Abyss Lord Balrog"; e.maxHp = 1100f * fm; e.speed = 1.9f; e.damage = 38f * fm;
                    e.attackRange = 3.2f; e.attackCooldown = 1.4f; e.radius = 1.8f; size = 2.6f;
                    break;
            }
            e.hp = e.maxHp;
            string key = GetEnemySpriteKey(kind, 0);
            e.root = CreateBillboard(key, pos, size, out e.spriteRend);
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
            DungeonProp p = new DungeonProp { kind = kind, pos = pos, captiveHero = captive };
            float size = (kind == PropKind.Cage) ? 1.75f : 1.35f;
            string key = (kind == PropKind.Cage) ? "prop_cage" : (kind == PropKind.Chest ? "prop_chest" : "prop_crate");
            p.maxHp = (kind == PropKind.Cage) ? 120f : (kind == PropKind.Chest ? 80f : 50f);
            p.hp = p.maxHp;
            p.root = CreateBillboard(key, pos, size, out p.spriteRend);

            if (kind == PropKind.Cage)
            {
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

            if (screen != GameScreen.Playing) return;

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
                if (heroes[i].root != null)
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

            Vector2 screenPos = Vector2.zero;
            bool isDown = false;
            bool isHeld = false;
            bool isUp = false;

            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                screenPos = t.position;
                if (t.phase == TouchPhase.Began) isDown = true;
                else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) isHeld = true;
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) isUp = true;
            }
            else
            {
                screenPos = Input.mousePosition;
                if (Input.GetMouseButtonDown(0)) isDown = true;
                else if (Input.GetMouseButton(0)) isHeld = true;
                else if (Input.GetMouseButtonUp(0)) isUp = true;
            }

            Vector2 gPos = new Vector2(screenPos.x / rx, (Screen.height - screenPos.y) / ry);

            // Exclude top HUD buttons and bottom Ultimate button
            bool inTopHud = gPos.y < 180f;
            bool inUltBtn = gPos.x > 530f && gPos.y > 1160f;

            if (isDown && !inTopHud && !inUltBtn)
            {
                draggingJoy = true;
                joyOriginGui = gPos;
                joyCurrentGui = gPos;
                manualDir = Vector2.zero;
            }
            else if (isHeld && draggingJoy)
            {
                Vector2 delta = gPos - joyOriginGui;
                float maxR = 90f;
                if (delta.magnitude > maxR) delta = delta.normalized * maxR;
                joyCurrentGui = joyOriginGui + delta;
                manualDir = delta.magnitude > 12f ? (delta / maxR) : Vector2.zero;
            }
            else if (isUp)
            {
                draggingJoy = false;
                manualDir = Vector2.zero;
            }
        }

        private void UpdateWavesAndSpawning(float dt)
        {
            if (runTimer < 25f) waveIndex = 1;
            else if (runTimer < 55f) waveIndex = 2;
            else waveIndex = 3;

            if (waveIndex == 3 && !bossSpawned)
            {
                bossSpawned = true;
                SpawnEnemyNearEdge(EnemyKind.Boss);
                SpawnProp(PropKind.Cage, new Vector3(0f, 0f, 7.0f), heroes.Count < 4 ? HeroClass.Mage : HeroClass.Knight);
                SpawnProp(PropKind.Chest, new Vector3(-6.0f, 0f, -6.0f), HeroClass.Knight);
                AddFloatText(heroes[0].pos + Vector3.up * 2.5f, Loc("경고! 심연의 마왕 발록 등장!", "WARNING! Abyss Lord Balrog Spawned!"), new Color(1f, 0.25f, 0.25f), true);
                TriggerShake(0.38f, 0.38f);
                PlaySfx(SfxId.Hurt);
            }

            spawnTimer -= dt;
            if (spawnTimer <= 0f && enemies.Count < 22 && !bossDefeated)
            {
                spawnTimer = (waveIndex == 1) ? 2.2f : ((waveIndex == 2) ? 1.8f : 3.0f);
                int batch = (waveIndex == 1) ? 2 : 3;
                for (int i = 0; i < batch; i++)
                {
                    float r = UnityEngine.Random.value;
                    EnemyKind k = EnemyKind.Slime;
                    if (waveIndex == 1) k = (r < 0.65f) ? EnemyKind.Slime : EnemyKind.Goblin;
                    else if (waveIndex == 2) k = (r < 0.35f) ? EnemyKind.Goblin : ((r < 0.72f) ? EnemyKind.Skeleton : EnemyKind.Orc);
                    else k = (r < 0.45f) ? EnemyKind.Skeleton : ((r < 0.80f) ? EnemyKind.Goblin : EnemyKind.Orc);
                    SpawnEnemyNearEdge(k);
                }
            }
        }

        private Vector3 ComputeAutoMoveDir(Vector3 leaderPos)
        {
            // 1. Dodge incoming danger telegraphs
            foreach (DangerTelegraph dt in telegraphs)
            {
                if (Vector3.Distance(leaderPos, dt.pos) <= dt.radius + 1.2f)
                {
                    Vector3 away = leaderPos - dt.pos;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                    return away.normalized;
                }
            }

            // 2. Rescue closest trapped companion in cage
            DungeonProp bestCage = null;
            float minCageDist = 999f;
            foreach (DungeonProp p in props)
            {
                if (p.kind == PropKind.Cage)
                {
                    float d = Vector3.Distance(leaderPos, p.pos);
                    if (d < minCageDist) { minCageDist = d; bestCage = p; }
                }
            }
            if (bestCage != null)
            {
                Vector3 toProp = bestCage.pos - leaderPos;
                if (toProp.magnitude > 1.8f) return toProp.normalized;
                return Vector3.zero;
            }

            // 3. Attack closest enemy
            EnemyUnit nearestE = FindNearestEnemy(leaderPos);
            if (nearestE != null)
            {
                Vector3 toE = nearestE.pos - leaderPos;
                float idealStop = 2.0f;
                if (toE.magnitude > idealStop) return toE.normalized;
                if (nearestE.kind == EnemyKind.Boss)
                    return new Vector3(-toE.z, 0f, toE.x).normalized * 0.45f;
                return Vector3.zero;
            }

            // 4. Break chests or crates
            DungeonProp bestLoot = null;
            float minLootDist = 999f;
            foreach (DungeonProp p in props)
            {
                float d = Vector3.Distance(leaderPos, p.pos);
                if (d < minLootDist) { minLootDist = d; bestLoot = p; }
            }
            if (bestLoot != null)
            {
                Vector3 toLoot = bestLoot.pos - leaderPos;
                if (toLoot.magnitude > 1.8f) return toLoot.normalized;
                return Vector3.zero;
            }

            // 5. Patrol toward center if nothing else
            if (leaderPos.magnitude > 3.0f)
            {
                return (-leaderPos).normalized * 0.5f;
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
            float moveSpeed = 5.5f * multHaste * (hasteBuffTimer > 0f ? 1.45f : 1f);

            // Movement: Manual drag overrides AUTO cleanly
            Vector3 moveDir = Vector3.zero;
            if (draggingJoy && manualDir.sqrMagnitude > 0.01f)
            {
                moveDir = new Vector3(manualDir.x, 0f, -manualDir.y); // Screen Y down -> world -Z
            }
            else if (autoMode)
            {
                moveDir = ComputeAutoMoveDir(leader.pos);
                if (ultCharge >= 100f && enemies.Count >= 3)
                {
                    TriggerUltimate();
                }
            }

            if (moveDir.sqrMagnitude > 0.001f)
            {
                leader.pos += moveDir * moveSpeed * dt;
                leader.pos = ClampInsideArena(leader.pos, 0.7f);
                if (Mathf.Abs(moveDir.x) > 0.08f) leader.facingRight = (moveDir.x > 0f);
            }

            // Formation follow for squadmates (우르릉 군집 대열!)
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
                    if (Mathf.Abs(diff.x) > 0.05f) hm.facingRight = (diff.x > 0f);
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
                if (hm.root != null)
                    hm.root.transform.position = hm.pos + Vector3.up * bob;

                if (hm.cooldownTimer <= 0f)
                {
                    // Priest prioritizes healing if anyone < 85% HP
                    if (hm.hClass == HeroClass.Priest && GetSquadHpRatio() < 0.85f)
                    {
                        foreach (HeroMember ally in heroes)
                        {
                            ally.hp = Mathf.Min(ally.maxHp, ally.hp + 24f * multDamage);
                        }
                        AddFloatText(hm.pos + Vector3.up * 1.5f, Loc("+치유!", "+HEAL!"), new Color(0.42f, 1f, 0.55f), false);
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
                        hm.cooldownTimer = hm.attackCooldown;
                        bool hitEnemy = (dE <= hm.attackRange && dE <= dP + 0.5f);
                        PerformHeroAttack(hm, hitEnemy, tgtE, tgtP, aimPos.Value);
                    }
                }
            }
        }

        private void PerformHeroAttack(HeroMember hm, bool hitEnemy, EnemyUnit tgtE, DungeonProp tgtP, Vector3 aimPos)
        {
            Vector3 diff = aimPos - hm.pos;
            diff.y = 0f;
            Vector3 dir = diff.sqrMagnitude > 0.01f ? diff.normalized : Vector3.forward;
            if (Mathf.Abs(dir.x) > 0.05f) hm.facingRight = (dir.x > 0f);

            float dmg = hm.attackDamage * multDamage;

            if (hm.hClass == HeroClass.Knight)
            {
                Vector3 slashPos = hm.pos + dir * 1.4f + Vector3.up * 0.6f;
                SpawnSlashEffect(slashPos, 1.6f, new Color(1f, 0.95f, 0.55f));
                PlaySfx(SfxId.Slash);

                foreach (EnemyUnit e in enemies.ToArray())
                {
                    if (Vector3.Distance(slashPos, e.pos) <= 2.2f) DamageEnemy(e, dmg);
                }
                foreach (DungeonProp p in props.ToArray())
                {
                    if (Vector3.Distance(slashPos, p.pos) <= 2.1f) DamageProp(p, dmg);
                }
            }
            else if (hm.hClass == HeroClass.Archer)
            {
                SpawnProjectile(true, false, hm.pos + Vector3.up * 0.6f, dir * 15f, dmg, 0f);
                PlaySfx(SfxId.Bow);
            }
            else if (hm.hClass == HeroClass.Mage)
            {
                SpawnProjectile(true, true, hm.pos + Vector3.up * 0.7f, dir * 12f, dmg, 2.4f);
                PlaySfx(SfxId.Boom);
            }
            else if (hm.hClass == HeroClass.Priest)
            {
                if (hitEnemy && tgtE != null)
                {
                    SpawnSlashEffect(tgtE.pos + Vector3.up * 0.6f, 1.4f, new Color(1f, 0.92f, 0.45f));
                    DamageEnemy(tgtE, dmg);
                }
                else if (tgtP != null)
                {
                    SpawnSlashEffect(tgtP.pos + Vector3.up * 0.6f, 1.3f, new Color(1f, 0.92f, 0.45f));
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
                    whirlwindOrbRoot = CreateBillboard("fx_slash", center, 1.35f, out r);
                }
                float ang = Time.time * (5.5f + rankWhirlwind);
                whirlwindOrbRoot.transform.position = center + new Vector3(Mathf.Cos(ang) * 2.4f, 0.5f, Mathf.Sin(ang) * 2.4f);
                whirlwindTimer -= dt;
                if (whirlwindTimer <= 0f)
                {
                    whirlwindTimer = 0.40f;
                    foreach (EnemyUnit e in enemies.ToArray())
                    {
                        if (Vector3.Distance(whirlwindOrbRoot.transform.position, e.pos) <= 1.6f)
                        {
                            DamageEnemy(e, 22f * rankWhirlwind * multDamage);
                        }
                    }
                }
            }

            // 2. Thunder Strike
            if (rankThunder > 0)
            {
                thunderTimer -= dt;
                if (thunderTimer <= 0f)
                {
                    thunderTimer = Mathf.Max(1.2f, 3.2f - rankThunder * 0.6f);
                    EnemyUnit tgt = FindNearestEnemy(center);
                    if (tgt != null)
                    {
                        SpawnSlashEffect(tgt.pos + Vector3.up * 1.2f, 2.2f, new Color(0.45f, 0.85f, 1f));
                        DamageEnemy(tgt, 55f * rankThunder * multDamage);
                        PlaySfx(SfxId.Boom);
                    }
                }
            }
        }

        public void TriggerUltimate()
        {
            if (ultCharge < 100f || heroes.Count == 0) return;
            ultCharge = 0f;
            hasteBuffTimer = 5.0f;
            Vector3 center = heroes[0].pos;
            TriggerShake(0.42f, 0.48f);
            PlaySfx(SfxId.Boom);
            AddFloatText(center + Vector3.up * 2.4f, Loc("⚡ 일제 돌격 메테오!!", "⚡ ALL-OUT METEOR RAID!!"), new Color(1f, 0.80f, 0.20f), true);

            foreach (EnemyUnit e in enemies.ToArray())
            {
                if (Vector3.Distance(center, e.pos) <= 11f)
                {
                    SpawnSlashEffect(e.pos + Vector3.up * 0.6f, 2.4f, new Color(1f, 0.55f, 0.15f));
                    DamageEnemy(e, 120f * multDamage);
                }
            }
            foreach (DungeonProp p in props.ToArray())
            {
                if (Vector3.Distance(center, p.pos) <= 10f) DamageProp(p, 95f);
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
                if (Mathf.Abs(diff.x) > 0.05f) e.facingRight = (diff.x > 0f);

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
                    else if (e.kind == EnemyKind.Boss)
                    {
                        SpawnTelegraph(tgt.pos, 2.6f, 1.25f, e.damage * 1.5f);
                        for (int k = -1; k <= 1; k++)
                        {
                            Vector3 dir = Quaternion.Euler(0, k * 28f, 0) * diff.normalized;
                            SpawnProjectile(false, true, e.pos + Vector3.up * 1.1f, dir * 7.5f, e.damage, 1.8f);
                        }
                    }
                    else
                    {
                        DamageHero(tgt, e.damage);
                        SpawnSlashEffect(tgt.pos + Vector3.up * 0.5f, 1.2f, new Color(1f, 0.25f, 0.25f));
                        PlaySfx(SfxId.Hurt);
                    }
                }

                float bob = Mathf.Abs(Mathf.Sin(Time.time * 9f + i)) * 0.09f;
                if (e.root != null)
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
                if (pr.root != null) pr.root.transform.position = pr.pos;

                bool hit = (pr.life <= 0f || Mathf.Abs(pr.pos.x) > ArenaHalf || Mathf.Abs(pr.pos.z) > ArenaHalf);

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
                                foreach (EnemyUnit aoeE in enemies.ToArray())
                                {
                                    if (Vector3.Distance(pr.pos, aoeE.pos) <= pr.aoeRadius) DamageEnemy(aoeE, pr.damage);
                                }
                            }
                            else
                            {
                                DamageEnemy(e, pr.damage);
                            }
                            break;
                        }
                    }
                    if (!hit)
                    {
                        foreach (DungeonProp p in props.ToArray())
                        {
                            if (Vector3.Distance(new Vector3(pr.pos.x, 0, pr.pos.z), p.pos) <= 1.2f)
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
                        if (Vector3.Distance(new Vector3(pr.pos.x, 0, pr.pos.z), h.pos) <= 0.85f)
                        {
                            hit = true;
                            DamageHero(h, pr.damage);
                            SpawnSlashEffect(h.pos + Vector3.up * 0.5f, 1.2f, new Color(1f, 0.25f, 0.25f));
                            PlaySfx(SfxId.Hurt);
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
            ultCharge = Mathf.Min(100f, ultCharge + 4.5f);
            if (showDmgText)
            {
                bool crit = UnityEngine.Random.value < 0.22f;
                float finalDmg = crit ? dmg * 1.6f : dmg;
                string t = Mathf.RoundToInt(finalDmg).ToString();
                AddFloatText(e.pos + Vector3.up * 1.2f, crit ? (t + " CRIT!") : t, crit ? new Color(1f, 0.82f, 0.18f) : Color.white, crit);
            }

            if (vampiricPct > 0f && heroes.Count > 0)
            {
                HeroMember lead = heroes[0];
                lead.hp = Mathf.Min(lead.maxHp, lead.hp + dmg * vampiricPct);
            }

            if (e.hp <= 0f)
            {
                KillEnemy(e);
            }
        }

        private void KillEnemy(EnemyUnit e)
        {
            kills++;
            float xpVal = (e.kind == EnemyKind.Boss) ? 140f : ((e.kind == EnemyKind.Orc) ? 35f : 15f);
            float goldVal = (e.kind == EnemyKind.Boss) ? 80f : ((e.kind == EnemyKind.Orc) ? 20f : 8f);

            SpawnPickup(PickupKind.Gem, e.pos, xpVal);
            SpawnPickup(PickupKind.Coin, e.pos + new Vector3(0.4f, 0, 0), goldVal);
            if (UnityEngine.Random.value < 0.25f)
                SpawnPickup(PickupKind.Heart, e.pos + new Vector3(-0.4f, 0, 0), 30f);

            PlaySfx(SfxId.Pickup);

            if (e.kind == EnemyKind.Boss)
            {
                bossDefeated = true;
                TriggerShake(0.6f, 0.6f);
                PlaySfx(SfxId.Victory);
                AddFloatText(e.pos + Vector3.up * 2f, Loc("마왕 발록 토벌 완료!!", "LORD BALROG DEFEATED!!"), new Color(1f, 0.88f, 0.25f), true);
                Invoke(nameof(OnVictory), 1.8f);
            }

            Destroy(e.root);
            enemies.Remove(e);
        }

        private void DamageProp(DungeonProp p, float dmg)
        {
            p.hp -= dmg;
            p.hitFlash = 0.12f;
            if (p.hp <= 0f)
            {
                BreakProp(p);
            }
        }

        private void BreakProp(DungeonProp p)
        {
            TriggerShake(0.2f, 0.2f);
            PlaySfx(SfxId.Boom);
            SpawnSlashEffect(p.pos + Vector3.up * 0.6f, 1.8f, new Color(1f, 0.82f, 0.35f));

            if (p.kind == PropKind.Cage)
            {
                if (heroes.Count < 5)
                {
                    SpawnHero(p.captiveHero, p.pos);
                    string hName = (p.captiveHero == HeroClass.Mage) ? Loc("마법사 루나", "Mage Luna") : Loc("사제 세라", "Priestess Sera");
                    AddFloatText(p.pos + Vector3.up * 2.2f, Loc("동료 구출! " + hName + " 합류!", "RESCUED! " + hName + " Joined!"), new Color(0.42f, 1f, 0.65f), true);
                    PlaySfx(SfxId.Rescue);
                }
                else
                {
                    SpawnPickup(PickupKind.Heart, p.pos, 50f);
                    SpawnPickup(PickupKind.Coin, p.pos, 40f);
                }
            }
            else if (p.kind == PropKind.Chest)
            {
                SpawnPickup(PickupKind.Gem, p.pos, 45f);
                SpawnPickup(PickupKind.Coin, p.pos + Vector3.left * 0.4f, 35f);
                SpawnPickup(PickupKind.Heart, p.pos + Vector3.right * 0.4f, 35f);
                AddFloatText(p.pos + Vector3.up * 1.5f, Loc("보물상자 획득!", "TREASURE CHEST!"), Color.yellow, false);
            }
            else
            {
                SpawnPickup(PickupKind.Coin, p.pos, 15f);
                if (UnityEngine.Random.value < 0.4f)
                    SpawnPickup(PickupKind.Heart, p.pos, 25f);
            }

            Destroy(p.root);
            props.Remove(p);
        }

        private void DamageHero(HeroMember h, float dmg)
        {
            h.hp -= dmg;
            h.hitFlash = 0.14f;
            TriggerShake(0.18f, 0.18f);

            if (h.hp <= 0f)
            {
                AddFloatText(h.pos + Vector3.up * 1.5f, Loc(h.nameKr + " 전사!", h.nameEn + " Fallen!"), Color.red, true);
                Destroy(h.root);
                heroes.Remove(h);
                if (heroes.Count == 0)
                {
                    totalGold += runGold;
                    PlayerPrefs.SetInt("GR_TotalGold", totalGold);
                    PlayerPrefs.Save();
                    screen = GameScreen.Defeat;
                    PlaySfx(SfxId.Hurt);
                }
            }
        }

        private void SpawnPickup(PickupKind kind, Vector3 pos, float val)
        {
            Renderer r;
            string key = (kind == PickupKind.Coin) ? "fx_coin" : ((kind == PickupKind.Heart) ? "fx_heart" : "fx_gem");
            GameObject go = CreateBillboard(key, pos, 0.75f, out r);
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
                if (d < 4.5f)
                {
                    p.pos = Vector3.MoveTowards(p.pos, leader, dt * 12.5f);
                }
                if (p.root != null)
                    p.root.transform.position = p.pos + Vector3.up * (0.28f + Mathf.Sin(Time.time * 5f + p.bobOffset) * 0.1f);

                if (d < 1.15f)
                {
                    if (p.kind == PickupKind.Coin)
                    {
                        runGold += Mathf.RoundToInt(p.value);
                        AddFloatText(leader + Vector3.up * 1.4f, "+" + Mathf.RoundToInt(p.value) + "G", new Color(1f, 0.88f, 0.25f), false);
                    }
                    else if (p.kind == PickupKind.Heart)
                    {
                        foreach (HeroMember h in heroes)
                        {
                            h.hp = Mathf.Min(h.maxHp, h.hp + p.value);
                        }
                        AddFloatText(leader + Vector3.up * 1.4f, Loc("체력 회복!", "HP HEALED!"), new Color(0.35f, 1f, 0.55f), false);
                        PlaySfx(SfxId.Heal);
                    }
                    else if (p.kind == PickupKind.Gem)
                    {
                        squadXp += p.value;
                        if (squadXp >= nextXp)
                        {
                            squadXp -= nextXp;
                            squadLevel++;
                            nextXp = Mathf.RoundToInt(nextXp * 1.4f + 10f);
                            OpenLevelUp();
                        }
                    }
                    Destroy(p.root);
                    pickups.RemoveAt(i);
                }
            }
        }

        private void OpenLevelUp()
        {
            PlaySfx(SfxId.LevelUp);
            UpgradeOffer[] pool = {
                new UpgradeOffer {
                    id = UpgradeId.Whirlwind,
                    titleKr = "회오리 검기", titleEn = "Whirlwind Slash",
                    descKr = "영웅 주위를 회전하며 적을 지속 타격하는 칼바람 소환", descEn = "Summons spinning wind blades that orbit squad and damage foes",
                    badgeKr = "공격 스킬", badgeEn = "ACTIVE SKILL", accent = new Color(0.42f, 0.82f, 1f)
                },
                new UpgradeOffer {
                    id = UpgradeId.ChainFire,
                    titleKr = "연쇄 화염구", titleEn = "Chain Fireball",
                    descKr = "마법사의 화염구 범위 +40% 및 관통 폭발 연쇄 반응", descEn = "Mage fireball AoE +40% with explosive chain blast",
                    badgeKr = "마법 강화", badgeEn = "MAGIC BUFF", accent = new Color(1f, 0.55f, 0.20f)
                },
                new UpgradeOffer {
                    id = UpgradeId.Thunder,
                    titleKr = "심판의 벼락", titleEn = "Holy Thunder",
                    descKr = "가장 강한 적에게 주기적으로 벼락을 내리쳐 55 광역 피해", descEn = "Periodically calls down divine lightning on toughest enemies",
                    badgeKr = "자동 낙뢰", badgeEn = "AUTO THUNDER", accent = new Color(1f, 0.88f, 0.28f)
                },
                new UpgradeOffer {
                    id = UpgradeId.Haste,
                    titleKr = "신속의 오라", titleEn = "Haste Aura",
                    descKr = "원정대 이동 속도 및 공격 속도 영구 15% 가속", descEn = "Permanently increases squad move and attack speed by 15%",
                    badgeKr = "속도 강화", badgeEn = "SPEED BUFF", accent = new Color(0.40f, 1f, 0.65f)
                },
                new UpgradeOffer {
                    id = UpgradeId.SquadPower,
                    titleKr = "원정대 무력", titleEn = "Squad Might",
                    descKr = "모든 영웅의 기본 공격력 +20% 영구 증가", descEn = "Permanently increases all hero attack damage by +20%",
                    badgeKr = "공격력 증가", badgeEn = "ATTACK BUFF", accent = new Color(1f, 0.35f, 0.35f)
                },
                new UpgradeOffer {
                    id = UpgradeId.Vampiric,
                    titleKr = "흡혈의 손길", titleEn = "Vampiric Touch",
                    descKr = "적에게 가한 피해의 4%만큼 선봉 대장 체력 지속 흡혈", descEn = "Heals leader for 4% of all damage dealt to enemies",
                    badgeKr = "생존 패시브", badgeEn = "SURVIVAL", accent = new Color(0.85f, 0.45f, 1f)
                }
            };

            List<UpgradeOffer> l = new List<UpgradeOffer>(pool);
            for (int i = 0; i < 3; i++)
            {
                int r = UnityEngine.Random.Range(0, l.Count);
                currentOffers[i] = l[r];
                l.RemoveAt(r);
            }
            screen = GameScreen.LevelUp;
        }

        private void ApplyUpgrade(UpgradeOffer offer)
        {
            switch (offer.id)
            {
                case UpgradeId.Whirlwind: rankWhirlwind++; break;
                case UpgradeId.ChainFire: rankChainFire++; break;
                case UpgradeId.Thunder: rankThunder++; break;
                case UpgradeId.Haste: multHaste += 0.15f; break;
                case UpgradeId.SquadPower: multDamage += 0.20f; break;
                case UpgradeId.Vampiric: vampiricPct += 0.04f; break;
            }
            if (heroes.Count > 0)
                AddFloatText(heroes[0].pos + Vector3.up * 2f, Loc(offer.titleKr + " 획득!", offer.titleEn + " ACQUIRED!"), offer.accent, true);
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
            return p;
        }

        private EnemyUnit FindNearestEnemy(Vector3 p)
        {
            EnemyUnit best = null;
            float minD = 999f;
            foreach (EnemyUnit e in enemies)
            {
                float d = Vector3.Distance(p, e.pos);
                if (d < minD) { minD = d; best = e; }
            }
            return best;
        }

        private DungeonProp FindNearestProp(Vector3 p)
        {
            DungeonProp best = null;
            float minD = 999f;
            foreach (DungeonProp pr in props)
            {
                float d = Vector3.Distance(p, pr.pos);
                if (d < minD) { minD = d; best = pr; }
            }
            return best;
        }

        private HeroMember FindNearestHero(Vector3 p)
        {
            HeroMember best = null;
            float minD = 999f;
            foreach (HeroMember h in heroes)
            {
                float d = Vector3.Distance(p, h.pos);
                if (d < minD) { minD = d; best = h; }
            }
            return best;
        }

        private void OnVictory()
        {
            totalGold += runGold + 150;
            if (currentFloor >= bestFloor) bestFloor = currentFloor + 1;
            PlayerPrefs.SetInt("GR_TotalGold", totalGold);
            PlayerPrefs.SetInt("GR_BestFloor", bestFloor);
            PlayerPrefs.Save();
            screen = GameScreen.Victory;
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
            // Language Toggle (Top Right)
            string langBtnLabel = (curLang == GameLang.KR) ? "🌐 한국어 (KR)" : "🌐 English (EN)";
            if (GUI.Button(new Rect(480, 42, 230, 48), langBtnLabel, BtnStyle(18, texBtnDark, Color.white)))
            {
                curLang = (curLang == GameLang.KR) ? GameLang.EN : GameLang.KR;
                PlayerPrefs.SetInt("GR_Language", (int)curLang);
                PlayerPrefs.Save();
                PlaySfx(SfxId.Pickup);
            }

            GUI.DrawTexture(new Rect(40, 100, 670, 260), texHeaderPanel);
            GUI.Label(new Rect(55, 122, 640, 68), Loc("고잉레이드", "GOIN RAID"), LabelStyle(52, new Color(1f, 0.88f, 0.35f)));
            GUI.Label(new Rect(55, 195, 640, 36), Loc("GOIN RAID · v0.2.0 원핸드 군집 슬래셔", "GOIN RAID · v0.2.0 Squad Action RPG"), LabelStyle(22, new Color(0.50f, 0.85f, 1f)));
            GUI.Label(new Rect(55, 240, 640, 82),
                Loc("감옥의 동료를 구출해 5인 원정대를 꾸리고\n심연의 마왕 발록을 토벌하라! (한 손 조작 + AUTO)",
                    "Rescue captive allies, build a 5-hero squad\nand defeat Lord Balrog! (One-Hand + AUTO)"),
                LabelStyle(20, new Color(0.90f, 0.93f, 0.98f), TextAnchor.MiddleCenter, false));

            GUI.DrawTexture(new Rect(110, 385, 530, 74), texDarkPanel);
            GUI.Label(new Rect(120, 392, 510, 60),
                Loc("최고 도달: 제 " + bestFloor + "층   |   골드: " + totalGold + " G",
                    "Best Floor: " + bestFloor + "   |   Gold: " + totalGold + " G"),
                LabelStyle(22, new Color(1f, 0.85f, 0.35f)));

            if (GUI.Button(new Rect(85, 840, 580, 96), Loc("⚔️  레이드 출정하기 (" + bestFloor + "층)", "⚔️  START RAID (Floor " + bestFloor + ")"), BtnStyle(28, texBtnGold, Color.white)))
            {
                StartRun(bestFloor);
            }
            if (GUI.Button(new Rect(85, 960, 580, 76), Loc("📜  영웅 & 몬스터 도감", "📜  HERO & MONSTER CODEX"), BtnStyle(24, texBtnDark, Color.white)))
            {
                screen = GameScreen.Codex;
                PlaySfx(SfxId.Pickup);
            }
            if (GUI.Button(new Rect(85, 1056, 580, 76), Loc("⚙️  게임 설정", "⚙️  SETTINGS"), BtnStyle(24, texBtnDark, Color.white)))
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
            string waveStr = bossSpawned ? Loc("BOSS 결전!", "BOSS FIGHT!") : ("WAVE " + waveIndex + "/3");
            GUI.Label(new Rect(38, 56, 360, 34),
                Loc("제 " + currentFloor + "층 · " + waveStr + "   영웅 " + heroes.Count + "/5",
                    "Floor " + currentFloor + " · " + waveStr + "   Heroes " + heroes.Count + "/5"),
                LabelStyle(20, Color.white, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(38, 92, 260, 30),
                "LV." + squadLevel + "   " + Loc("처치 ", "Kills ") + kills + "   💰 " + runGold + "G",
                LabelStyle(18, new Color(1f, 0.86f, 0.35f), TextAnchor.MiddleLeft));

            // XP Progress Bar
            GUI.DrawTexture(new Rect(38, 134, 460, 22), texBarBg);
            GUI.DrawTexture(new Rect(38, 134, 460 * Mathf.Clamp01(squadXp / nextXp), 22), texXpCyan);
            GUI.Label(new Rect(38, 132, 460, 26), "EXP " + Mathf.FloorToInt(squadXp) + " / " + Mathf.FloorToInt(nextXp), LabelStyle(14, Color.white));

            // Smart Hybrid AUTO Toggle Button
            Texture2D autoTex = autoMode ? texBtnAutoOn : texBtnAutoOff;
            string autoLabel = autoMode ? Loc("🤖 AUTO ON", "🤖 AUTO ON") : Loc("🕹️ 수동 조작", "🕹️ MANUAL");
            if (GUI.Button(new Rect(520, 62, 192, 56), autoLabel, BtnStyle(20, autoTex, Color.white)))
            {
                autoMode = !autoMode;
                PlaySfx(SfxId.Pickup);
                if (heroes.Count > 0)
                    AddFloatText(heroes[0].pos + Vector3.up * 1.8f, autoMode ? Loc("자동 전투 ON", "AUTO RAID ON") : Loc("수동 조작 전환", "MANUAL CONTROL"), Color.cyan, false);
            }

            // Pause / Lobby button
            if (GUI.Button(new Rect(520, 126, 192, 38), Loc("🏠 로비 복귀", "🏠 LOBBY"), BtnStyle(16, texBtnDark, new Color(0.85f, 0.9f, 1f))))
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
                GUI.Label(new Rect(74, 194, 602, 24), Loc("👹 마왕 발록 HP: ", "👹 BALROG HP: ") + Mathf.CeilToInt(boss.hp) + " / " + Mathf.CeilToInt(boss.maxHp), LabelStyle(16, new Color(1f, 0.85f, 0.85f)));
            }

            // Bottom Squad HP Summary & Ultimate Button
            GUI.DrawTexture(new Rect(20, 1175, 500, 108), texDarkPanel);
            float hpPct = GetSquadHpRatio();
            string modeStr = draggingJoy ? Loc("직접 이동 중", "Manual Drag") : (autoMode ? Loc("자동 탐험", "Auto Raid") : Loc("대기", "Standby"));
            GUI.Label(new Rect(38, 1184, 464, 30),
                Loc("원정대 총 체력  ", "Squad HP  ") + Mathf.RoundToInt(hpPct * 100f) + "%   |   " + Loc("모드: ", "Mode: ") + modeStr,
                LabelStyle(18, Color.white, TextAnchor.MiddleLeft));
            GUI.DrawTexture(new Rect(38, 1222, 464, 26), texBarBg);
            GUI.DrawTexture(new Rect(38, 1222, 464 * hpPct, 26), texHpGreen);
            GUI.Label(new Rect(38, 1254, 464, 24), Loc("화면을 드래그하면 즉시 수동 이동합니다", "Drag anywhere to control squad movement"), LabelStyle(14, new Color(0.72f, 0.80f, 0.92f), TextAnchor.MiddleLeft, false));

            // Ultimate Button
            bool ready = (ultCharge >= 100f);
            string ultLabel = ready ? Loc("⚡ 일제 돌격\n[발동!]", "⚡ METEOR\n[READY!]") : (Loc("⚡ 필살기\n", "⚡ ULTIMATE\n") + Mathf.FloorToInt(ultCharge) + "%");
            if (GUI.Button(new Rect(536, 1175, 194, 108), ultLabel, BtnStyle(20, ready ? texBtnGold : texBtnDark, ready ? Color.white : new Color(0.75f, 0.8f, 0.9f))))
            {
                TriggerUltimate();
            }

            // Floating Virtual Joystick visual
            if (draggingJoy)
            {
                GUI.DrawTexture(new Rect(joyOriginGui.x - 85f, joyOriginGui.y - 85f, 170f, 170f), texJoyRing);
                GUI.DrawTexture(new Rect(joyCurrentGui.x - 42f, joyCurrentGui.y - 42f, 84f, 84f), texJoyKnob);
            }
            else
            {
                // Semi-transparent idle joystick hint
                GUI.DrawTexture(new Rect(180f - 65f, 1020f - 65f, 130f, 130f), texJoyRing);
                GUI.DrawTexture(new Rect(180f - 28f, 1020f - 28f, 56f, 56f), texJoyKnob);
            }
        }

        private void DrawLevelUpModal()
        {
            GUI.DrawTexture(new Rect(0, 0, 750, 1334), texDimOverlay);
            GUI.DrawTexture(new Rect(45, 190, 660, 920), texHeaderPanel);
            GUI.Label(new Rect(60, 215, 630, 56), Loc("✨ 원정대 레벨 업! (LV." + squadLevel + ")", "✨ SQUAD LEVEL UP! (LV." + squadLevel + ")"), LabelStyle(34, new Color(1f, 0.88f, 0.35f)));
            GUI.Label(new Rect(60, 272, 630, 32), Loc("원하는 스킬 1종을 선택하세요", "Choose 1 Skill Upgrade"), LabelStyle(20, new Color(0.85f, 0.92f, 1f), TextAnchor.MiddleCenter, false));

            for (int i = 0; i < 3; i++)
            {
                UpgradeOffer of = currentOffers[i];
                if (of == null) continue;
                float y = 335 + i * 245;
                GUI.DrawTexture(new Rect(75, y, 600, 215), texCardBg);
                string title = Loc(of.titleKr, of.titleEn);
                string badge = Loc(of.badgeKr, of.badgeEn);
                string desc = Loc(of.descKr, of.descEn);

                GUI.Label(new Rect(100, y + 18, 550, 40), title, LabelStyle(28, of.accent, TextAnchor.MiddleLeft));
                GUI.Label(new Rect(100, y + 60, 550, 30), "[" + badge + "]", LabelStyle(18, new Color(0.95f, 0.95f, 1f), TextAnchor.MiddleLeft));
                GUI.Label(new Rect(100, y + 92, 550, 56), desc, LabelStyle(18, new Color(0.80f, 0.85f, 0.95f), TextAnchor.MiddleLeft, false));

                if (GUI.Button(new Rect(445, y + 152, 210, 48), Loc("선택하기", "SELECT"), BtnStyle(20, texBtnGold, Color.white)))
                {
                    ApplyUpgrade(of);
                }
            }
        }

        private void DrawCodex()
        {
            GUI.DrawTexture(new Rect(40, 80, 670, 1160), texDarkPanel);
            GUI.Label(new Rect(50, 100, 650, 50), Loc("📜 영웅 및 몬스터 도감", "📜 HERO & MONSTER CODEX"), LabelStyle(34, Color.white));

            string[] entries = {
                Loc("🛡️ 기사 아서  |  근접 범위 검기 · 높은 체력으로 선봉 유지", "🛡️ Knight Arthur  |  Melee cleave slash · High HP vanguard tank"),
                Loc("🏹 궁수 레나  |  빠른 관통 화살 · 원거리 집중 사격", "🏹 Archer Lena    |  Rapid piercing arrows · High ranged DPS"),
                Loc("🔮 마법사 루나 |  감옥 구출 합류 · 광역 폭발 화염구", "🔮 Mage Luna      |  Rescued from cage · Giant AoE explosive fireball"),
                Loc("✨ 사제 세라  |  감옥 구출 합류 · 아군 전원 체력 치유", "✨ Priestess Sera |  Rescued from cage · Holy aura heals all allies"),
                Loc("👾 몬스터군  |  슬라임·고블린·해골궁수·오크광전사", "👾 Monsters       |  Slime, Goblin, Skeleton Archer, Orc Berserker"),
                Loc("👹 마왕 발록  |  웨이브 3 보스 · 바닥 장판 예고 폭발 + 탄막", "👹 Lord Balrog    |  Wave 3 Boss · Telegraphed lava blast + fire barrage")
            };
            for (int i = 0; i < entries.Length; i++)
            {
                float y = 175 + i * 135;
                GUI.DrawTexture(new Rect(60, y, 630, 112), texCardBg);
                GUI.Label(new Rect(85, y + 16, 580, 80), entries[i], LabelStyle(18, Color.white, TextAnchor.MiddleLeft));
            }

            if (GUI.Button(new Rect(150, 1130, 450, 76), Loc("⬅️  타이틀로 돌아가기", "⬅️  BACK TO MENU"), BtnStyle(24, texBtnGold, Color.white)))
            {
                screen = GameScreen.Title;
            }
        }

        private void DrawSettings()
        {
            GUI.DrawTexture(new Rect(60, 240, 630, 800), texDarkPanel);
            GUI.Label(new Rect(80, 270, 590, 50), Loc("⚙️ 게임 환경 설정", "⚙️ GAME SETTINGS"), LabelStyle(34, Color.white));

            // Language Toggle in Settings
            string langStr = (curLang == GameLang.KR) ? "언어 (Language): 한국어 [KR]" : "Language: English [EN]";
            if (GUI.Button(new Rect(95, 360, 560, 80), langStr, BtnStyle(22, texBtnDark, Color.yellow)))
            {
                curLang = (curLang == GameLang.KR) ? GameLang.EN : GameLang.KR;
                PlayerPrefs.SetInt("GR_Language", (int)curLang);
                PlayerPrefs.Save();
                PlaySfx(SfxId.Pickup);
            }

            // SFX Toggle
            string sfxStr = Loc("사운드 효과음: ", "Sound Effects: ") + (sfxEnabled ? Loc("켜짐 (ON)", "ON") : Loc("꺼짐 (OFF)", "OFF"));
            if (GUI.Button(new Rect(95, 460, 560, 80), sfxStr, BtnStyle(22, sfxEnabled ? texBtnAutoOn : texBtnAutoOff, Color.white)))
            {
                sfxEnabled = !sfxEnabled;
                PlaySfx(SfxId.Pickup);
            }

            // Damage Text Toggle
            string dmgStr = Loc("대미지 숫자 표기: ", "Damage Numbers: ") + (showDmgText ? Loc("켜짐 (ON)", "ON") : Loc("꺼짐 (OFF)", "OFF"));
            if (GUI.Button(new Rect(95, 560, 560, 80), dmgStr, BtnStyle(22, showDmgText ? texBtnAutoOn : texBtnAutoOff, Color.white)))
            {
                showDmgText = !showDmgText;
            }

            // Default AUTO Toggle
            string defAutoStr = Loc("기본 자동 탐험(AUTO): ", "Default Auto Mode: ") + (autoMode ? Loc("켜짐 (ON)", "ON") : Loc("꺼짐 (OFF)", "OFF"));
            if (GUI.Button(new Rect(95, 660, 560, 80), defAutoStr, BtnStyle(22, autoMode ? texBtnAutoOn : texBtnAutoOff, Color.white)))
            {
                autoMode = !autoMode;
            }

            if (GUI.Button(new Rect(150, 890, 450, 80), Loc("⬅️  설정 완료 및 복귀", "⬅️  SAVE & RETURN"), BtnStyle(24, texBtnGold, Color.white)))
            {
                screen = GameScreen.Title;
            }
        }

        private void DrawEndModal(bool win)
        {
            GUI.DrawTexture(new Rect(0, 0, 750, 1334), texDimOverlay);
            GUI.DrawTexture(new Rect(60, 320, 630, 620), texHeaderPanel);
            GUI.Label(new Rect(80, 355, 590, 68),
                win ? Loc("🏆 레이드 토벌 성공!", "🏆 RAID CLEARED!") : Loc("💀 원정대 전멸...", "💀 SQUAD DEFEATED..."),
                LabelStyle(42, win ? new Color(1f, 0.88f, 0.30f) : new Color(1f, 0.35f, 0.35f)));

            string endSummary = Loc(
                "도달 층수: 제 " + currentFloor + "층\n몬스터 처치: " + kills + "마리\n획득 골드: +" + (runGold + (win ? 150 : 0)) + " G",
                "Floor: " + currentFloor + "\nKills: " + kills + " monsters\nGold: +" + (runGold + (win ? 150 : 0)) + " G"
            );
            GUI.Label(new Rect(80, 455, 590, 150), endSummary, LabelStyle(24, Color.white));

            if (win)
            {
                if (GUI.Button(new Rect(105, 650, 540, 84), Loc("🔥 다음 층 출정 (제 " + (currentFloor + 1) + "층)", "🔥 NEXT RAID (Floor " + (currentFloor + 1) + ")"), BtnStyle(24, texBtnGold, Color.white)))
                {
                    StartRun(currentFloor + 1);
                }
            }
            else
            {
                if (GUI.Button(new Rect(105, 650, 540, 84), Loc("🔄 제 " + currentFloor + "층 다시 도전", "🔄 RETRY FLOOR " + currentFloor), BtnStyle(24, texBtnGold, Color.white)))
                {
                    StartRun(currentFloor);
                }
            }

            if (GUI.Button(new Rect(105, 760, 540, 72), Loc("🏠 타이틀 로비로", "🏠 MAIN MENU"), BtnStyle(22, texBtnDark, Color.white)))
            {
                StartTitleDiorama();
            }
        }

        private void DrawWorldBarsAndTexts(float rx, float ry)
        {
            if (mainCam == null) return;

            // Mini HP bars over heroes
            foreach (HeroMember h in heroes)
            {
                Vector3 sp = mainCam.WorldToScreenPoint(h.pos + Vector3.up * 1.55f);
                if (sp.z > 0)
                {
                    float vx = sp.x / rx;
                    float vy = (Screen.height - sp.y) / ry;
                    float pct = Mathf.Clamp01(h.hp / h.maxHp);
                    GUI.DrawTexture(new Rect(vx - 24, vy, 48, 6), texBarBg);
                    GUI.DrawTexture(new Rect(vx - 24, vy, 48 * pct, 6), texHpGreen);
                }
            }

            // Mini HP bars over props
            foreach (DungeonProp p in props)
            {
                Vector3 sp = mainCam.WorldToScreenPoint(p.pos + Vector3.up * 1.55f);
                if (sp.z > 0)
                {
                    float vx = sp.x / rx;
                    float vy = (Screen.height - sp.y) / ry;
                    float pct = Mathf.Clamp01(p.hp / p.maxHp);
                    GUI.DrawTexture(new Rect(vx - 22, vy, 44, 5), texBarBg);
                    GUI.DrawTexture(new Rect(vx - 22, vy, 44 * pct, 5), Color.yellow != null ? texUltOrange : texBarBg);
                }
            }

            // Floating Combat Damage Numbers
            foreach (FloatingText ft in floatTexts)
            {
                Vector3 sp = mainCam.WorldToScreenPoint(ft.worldPos);
                if (sp.z > 0)
                {
                    float vx = sp.x / rx;
                    float vy = (Screen.height - sp.y) / ry;
                    float alpha = Mathf.Clamp01(ft.timer / ft.maxTime);
                    Color c = ft.color;
                    c.a = alpha;
                    GUI.Label(new Rect(vx - 120, vy - 15, 240, 30), ft.text, LabelStyle(ft.big ? 24 : 18, c, TextAnchor.MiddleCenter, ft.big));
                }
            }
        }
    }
}
