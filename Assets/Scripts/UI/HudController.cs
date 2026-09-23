using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Expedition;
using OutpostZero.Graphics;
using OutpostZero.Player;
using OutpostZero.Sensory;
using OutpostZero.Shell;

namespace OutpostZero.UI
{
    /// <summary>
    /// The street HUD on its own UI Toolkit panel, loaded from <c>Resources/HUD.uxml</c> (built from
    /// <see cref="HudTree"/> when the asset is missing). Text changes only when an event or a shown number changes;
    /// per-frame work is bar smoothing, markers, and fades, none of which allocates.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class HudController : MonoBehaviour
    {
        public const string ResourcePath = "HUD";
        public const int PopupPool = 24;
        private const float NoiseDecay = 3.5f;
        private const float CaptionLife = 2.4f;
        private const float SlowTick = 0.1f;

        public static HudController Instance { get; private set; }

        private UIDocument document;
        private PanelSettings panel;
        private VisualElement root;
        private readonly Dictionary<string, VisualElement> named = new Dictionary<string, VisualElement>();
        private readonly Label[] popups = new Label[PopupPool];
        private readonly Label[] toastLabels = new Label[HudTree.Toasts];
        private readonly VisualElement[] edges = new VisualElement[4];
        private readonly VisualElement[] slots = new VisualElement[HudTree.Slots];
        private readonly VisualElement[] slotIcons = new VisualElement[HudTree.Slots];
        private readonly Label[] slotNames = new Label[HudTree.Slots];
        private readonly Label[] wheel = new Label[HudTree.Slots];
        private readonly Label[] cardinals = new Label[4];
        private readonly VisualElement[] needFills = new VisualElement[3];
        private readonly Label[] status = new Label[HudTree.Status.Length];
        private readonly bool[] statusOn = new bool[HudTree.Status.Length];
        private readonly ToastStack toasts = new ToastStack();

        private VisualElement vitals, objectives, compass, feed, meters, loadout, veil, hitMarker, beat, radial, wheelBox;
        private VisualElement healthFill, healthGhost, staminaFill, noiseFill, exposureFill;
        private Label healthCaption, healthValue, healthMax, noiseLabel, noiseMark, exposureLabel, tensionLabel;
        private Label quota, poi, board, rescue, district, clock, timer;
        private Label killFeed, threats, watch, subtitle, hurt, tutorial, prompt;
        private Label mag, split, reserve, weaponName, belt, compassPoi, compassGate;

        private PlayerController player;
        private HealthSystem life;
        private SurvivalNeeds needs;
        private StatusEffectController effects;
        private PlayerVisibility visibility;
        private PlayerInventory inventory;
        private PlayerInteractor interactor;
        private FirearmWeapon gun;
        private HordeDirector horde;
        private ObjectiveTracker tracker;
        private NoiseManager noiseSource;
        private SettingsService settings;
        private WorldClock worldClock;
        private DistrictPoi poiSite;
        private Camera view;

        private float healthRatio = 1f, ghostRatio = 1f, staminaTarget = 1f, staminaShown = 1f;
        private float noiseLevel, exposureShown = -1f, ringSweep;
        private int shownHealth = -1, shownMax = -1, shownMag = -1, shownReserve = -1;
        private bool shownReloading;
        private int shownNeeds = -1, shownBangs = -1, shownQuestions = -1, shownTape = -1, shownRescue = -1;
        private int shownTutorial = int.MinValue, shownTimer = -1, shownWeapons = -1, shownActive = -1, shownHot = -2;
        private int shownLamp = -1, shownContacts = -1, shownInfection = -1, shownToasts = -1, shownLow = -1;
        private int vision;
        private string language = "";
        private string caption;
        private float captionUntil, hurtUntil, slowAt, promptAt, poiAt;
        private float hitAt = -10f, damageAt = -10f, damageStrength, damageIncoming;
        private bool hitKill, wheelWasOpen, peak;
        private Vector3 hitWorld;
        private object promptFor;
        private string lastDistrict;
        private GameState shownState = (GameState)(-1);
        private bool street, visible;

        public float NoiseLevel => noiseLevel;
        public VisualElement Root => root;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            var host = new GameObject("HUD");
            host.transform.SetParent(transform, false);
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.name = "HudPanel";
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int((int)HudFit.ReferenceWidth, (int)HudFit.ReferenceHeight);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 1f;
            panel.sortingOrder = 10;
            document = host.AddComponent<UIDocument>();
            document.panelSettings = panel;
            PanelScale.Track(panel);
            var surface = document.rootVisualElement;
            if (surface == null) return;
            surface.pickingMode = PickingMode.Ignore;
            root = Mount(surface);
            FontFallback.Dress(root);
            Collect();
            BuildPopups();
            BuildEdges();
            radial.generateVisualContent += DrawRing;
            GameplayFeedback.OnToast += Toast;
            CombatEvents.OnHit += HandleHit;
            CombatEvents.OnKill += HandleKill;
            ApplySettings();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameplayFeedback.OnToast -= Toast;
            CombatEvents.OnHit -= HandleHit;
            CombatEvents.OnKill -= HandleKill;
            BindPlayer(null);
            BindHorde(null);
            BindTracker(null);
            BindNoise(null);
            BindSettings(null);
            BindClock(null);
            if (radial != null) radial.generateVisualContent -= DrawRing;
        }

        private VisualElement Mount(VisualElement surface)
        {
            var tree = Resources.Load<VisualTreeAsset>(ResourcePath);
            if (tree != null)
            {
                tree.CloneTree(surface);
                var cloned = surface.Q(HudTree.Root);
                if (cloned != null)
                {
                    StretchContainers(surface, cloned);
                    return cloned;
                }
            }
            var sheet = Resources.Load<StyleSheet>(ResourcePath);
            if (sheet != null) surface.styleSheets.Add(sheet);
            var built = new VisualElement { name = HudTree.Root, pickingMode = PickingMode.Ignore };
            built.AddToClassList("hud");
            surface.Add(built);
            var made = new Dictionary<string, VisualElement> { { HudTree.Root, built } };
            foreach (var node in HudTree.Nodes)
            {
                VisualElement element = node.Kind == HudTree.Kind.Text ? new Label() : new VisualElement();
                element.name = node.Name;
                element.pickingMode = PickingMode.Ignore;
                foreach (var cls in node.Classes.Split(' ')) if (cls.Length > 0) element.AddToClassList(cls);
                made[node.Parent].Add(element);
                made[node.Name] = element;
            }
            return built;
        }

        private static void StretchContainers(VisualElement surface, VisualElement cloned)
        {
            for (var at = cloned.parent; at != null && at != surface.parent; at = at.parent)
            {
                at.pickingMode = PickingMode.Ignore;
                at.style.flexGrow = 1;
                at.style.position = Position.Absolute;
                at.style.left = 0;
                at.style.top = 0;
                at.style.right = 0;
                at.style.bottom = 0;
            }
        }

        private void Collect()
        {
            named.Clear();
            named[HudTree.Root] = root;
            foreach (var node in HudTree.Nodes)
            {
                var element = root.Q(node.Name);
                if (element != null) named[node.Name] = element;
            }
            vitals = E("vitals");
            objectives = E("objectives");
            compass = E("compass");
            feed = E("feed");
            meters = E("meters");
            loadout = E("loadout");
            veil = E("veil");
            hitMarker = E("hit-marker");
            beat = E("tension-beat");
            radial = E("reload-radial");
            wheelBox = E("wheel");
            healthFill = E("health-fill");
            healthGhost = E("health-ghost");
            staminaFill = E("stamina-fill");
            noiseFill = E("noise-fill");
            exposureFill = E("exposure-fill");
            healthCaption = L("health-caption");
            healthValue = L("health-value");
            healthMax = L("health-max");
            noiseLabel = L("noise-label");
            noiseMark = L("noise-mark");
            exposureLabel = L("exposure-label");
            tensionLabel = L("tension-label");
            quota = L("objective-quota");
            poi = L("objective-poi");
            board = L("objective-board");
            rescue = L("objective-rescue");
            district = L("objective-district");
            clock = L("objective-clock");
            timer = L("objective-timer");
            killFeed = L("kill-feed");
            threats = L("threats");
            watch = L("watch");
            subtitle = L("subtitle");
            hurt = L("hurt");
            tutorial = L("tutorial");
            prompt = L("prompt");
            mag = L("ammo-mag");
            split = L("ammo-split");
            reserve = L("ammo-reserve");
            weaponName = L("weapon-name");
            belt = L("belt");
            compassPoi = L("compass-poi");
            compassGate = L("compass-gate");
            for (int i = 0; i < HudTree.Toasts; i++) toastLabels[i] = L(HudTree.ToastName(i));
            for (int i = 0; i < 4; i++)
            {
                edges[i] = E("edge-" + HudTree.Edges[i]);
                cardinals[i] = L("compass-" + HudTree.Cardinals[i]);
            }
            for (int i = 0; i < HudTree.Slots; i++)
            {
                slots[i] = E(HudTree.SlotName(i));
                slotIcons[i] = E(HudTree.SlotIcon(i));
                slotNames[i] = L(HudTree.SlotLabel(i));
                wheel[i] = L(HudTree.WheelName(i));
                L(HudTree.SlotKey(i)).text = HudNumbers.Of(i + 1);
            }
            for (int i = 0; i < 3; i++) needFills[i] = E("need-" + HudTree.Needs[i] + "-fill");
            for (int i = 0; i < status.Length; i++) status[i] = L("status-" + HudTree.Status[i]);
            split.text = "/";
            compassPoi.text = "◆";
            compassGate.text = "▲";
        }

        private VisualElement E(string name)
        {
            if (named.TryGetValue(name, out var element)) return element;
            element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            named[name] = element;
            return element;
        }

        private Label L(string name)
        {
            if (named.TryGetValue(name, out var element) && element is Label label) return label;
            label = new Label { name = name, pickingMode = PickingMode.Ignore };
            named[name] = label;
            return label;
        }

        private void BuildPopups()
        {
            var layer = E("popups");
            for (int i = 0; i < PopupPool; i++)
            {
                var label = new Label { pickingMode = PickingMode.Ignore };
                label.AddToClassList("hud-popup");
                label.style.display = DisplayStyle.None;
                layer.Add(label);
                popups[i] = label;
            }
        }

        private void BuildEdges()
        {
            for (int i = 0; i < 4; i++)
            {
                edges[i].style.backgroundImage = new StyleBackground(EdgeRamp(i));
            }
        }

        private static Texture2D EdgeRamp(int edge)
        {
            const int size = 64;
            bool vertical = edge == 0 || edge == 2;
            var ramp = new Texture2D(vertical ? 1 : size, vertical ? size : 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "HudEdge" + edge
            };
            for (int i = 0; i < size; i++)
            {
                float t = i / (size - 1f);
                float fromEdge = edge == 0 || edge == 1 ? t : 1f - t;
                float alpha = fromEdge * fromEdge;
                var colour = new Color(1f, 1f, 1f, alpha);
                if (vertical) ramp.SetPixel(0, i, colour);
                else ramp.SetPixel(i, 0, colour);
            }
            ramp.Apply(false, true);
            return ramp;
        }

        private void Update()
        {
            if (root == null) return;
            Rebind();
            var flow = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.ExpeditionActive;
            if (flow != shownState) ApplyState(flow);
            if (!visible) return;

            float now = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;
            Smooth(dt);
            Meters(now);
            Weapon(now);
            Wheel();
            Compass();
            Markers(now);
            Toasts(now);
            Prompt(now);
            Popups();
            if (now >= slowAt)
            {
                slowAt = now + SlowTick;
                Slow();
            }
        }

        private void Rebind()
        {
            if (PlayerRegistry.Current != player) BindPlayer(PlayerRegistry.Current);
            if (HordeDirector.Instance != horde) BindHorde(HordeDirector.Instance);
            if (ObjectiveTracker.Instance != tracker) BindTracker(ObjectiveTracker.Instance);
            if (NoiseManager.Instance != noiseSource) BindNoise(NoiseManager.Instance);
            if (SettingsService.Instance != settings) BindSettings(SettingsService.Instance);
            if (WorldClock.Instance != worldClock) BindClock(WorldClock.Instance);
            if (view == null || !view.isActiveAndEnabled) view = Camera.main;
        }

        private void ApplyState(GameState flow)
        {
            shownState = flow;
            street = flow == GameState.ExpeditionActive || flow == GameState.RaidActive;
            visible = street || flow == GameState.CampManagement || flow == GameState.Paused;
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            Show(compass, street);
            Show(objectives, street);
            Show(meters, street);
            Show(feed, street);
            if (!street)
            {
                Show(hitMarker, false);
                for (int i = 0; i < 4; i++) edges[i].style.opacity = 0f;
            }
            shownTutorial = int.MinValue;
        }

        private void BindPlayer(PlayerController next)
        {
            if (player != null)
            {
                player.OnStaminaChanged -= HandleStamina;
                player.OnActiveWeaponChanged -= HandleWeapon;
            }
            if (life != null)
            {
                life.OnHealthChanged -= HandleHealth;
                life.OnDamaged -= HandleDamaged;
            }
            if (inventory != null) inventory.OnInventoryChanged -= HandleInventory;
            if (needs != null) needs.OnNeedsChanged -= HandleNeeds;
            BindGun(null);

            player = next;
            life = next != null ? next.GetComponent<HealthSystem>() : null;
            needs = next != null ? next.GetComponent<SurvivalNeeds>() : null;
            effects = next != null ? next.GetComponent<StatusEffectController>() : null;
            visibility = next != null ? next.GetComponent<PlayerVisibility>() : null;
            inventory = next != null ? next.GetComponent<PlayerInventory>() : null;
            interactor = next != null ? next.GetComponent<PlayerInteractor>() : null;

            if (player != null)
            {
                player.OnStaminaChanged += HandleStamina;
                player.OnActiveWeaponChanged += HandleWeapon;
                staminaTarget = player.MaxStamina > 0f ? player.CurrentStamina / player.MaxStamina : 1f;
                staminaShown = staminaTarget;
            }
            if (life != null)
            {
                life.OnHealthChanged += HandleHealth;
                life.OnDamaged += HandleDamaged;
                HandleHealth(life.CurrentHealth, life.MaxHealth);
                ghostRatio = healthRatio;
            }
            if (inventory != null) inventory.OnInventoryChanged += HandleInventory;
            if (needs != null) needs.OnNeedsChanged += HandleNeeds;
            Show(vitals, player != null);
            Show(loadout, player != null);
            HandleWeapon(player != null ? player.ActiveWeapon : null);
            HandleInventory();
            shownNeeds = -1;
            promptFor = null;
            damageAt = -10f;
        }

        private void BindGun(FirearmWeapon next)
        {
            if (gun != null)
            {
                gun.OnAmmoChanged -= HandleAmmo;
                gun.OnReloadStarted -= HandleReload;
                gun.OnReloadCompleted -= HandleReload;
            }
            gun = next;
            if (gun != null)
            {
                gun.OnAmmoChanged += HandleAmmo;
                gun.OnReloadStarted += HandleReload;
                gun.OnReloadCompleted += HandleReload;
            }
        }

        private void BindHorde(HordeDirector next)
        {
            if (horde != null) horde.OnTensionStateChanged -= HandleTension;
            horde = next;
            if (horde != null) horde.OnTensionStateChanged += HandleTension;
            HandleTension(horde != null ? horde.State : TensionState.Calm);
        }

        private void BindTracker(ObjectiveTracker next)
        {
            if (tracker != null) tracker.OnObjectivesChanged -= HandleObjectives;
            tracker = next;
            if (tracker != null) tracker.OnObjectivesChanged += HandleObjectives;
            HandleObjectives();
        }

        private void BindNoise(NoiseManager next)
        {
            if (noiseSource != null) noiseSource.OnNoiseEmitted -= HandleNoise;
            noiseSource = next;
            if (noiseSource != null) noiseSource.OnNoiseEmitted += HandleNoise;
        }

        private void BindSettings(SettingsService next)
        {
            if (settings != null) settings.OnChanged -= ApplySettings;
            settings = next;
            if (settings != null) settings.OnChanged += ApplySettings;
            ApplySettings();
        }

        private void BindClock(WorldClock next)
        {
            if (worldClock != null) worldClock.OnClockChanged -= HandleClock;
            worldClock = next;
            if (worldClock != null) worldClock.OnClockChanged += HandleClock;
            HandleClock();
        }

        private void ApplySettings()
        {
            if (root == null) return;
            float text = settings != null ? settings.TextScale : 1f;
            root.style.opacity = settings != null ? settings.HudOpacity : 1f;
            root.style.fontSize = HudFit.BaseFont * text;
            foreach (var node in HudTree.Nodes)
            {
                if (node.Font <= 0f || !named.TryGetValue(node.Name, out var element)) continue;
                element.style.fontSize = HudFit.BaseFont * node.Font * text;
            }
            vision = settings != null ? settings.ColorblindMode : 0;
            healthFill.style.backgroundColor = HudPalette.Health(vision);
            healthGhost.style.backgroundColor = HudPalette.Ghost(vision);
            string next = settings != null ? settings.Language : "en";
            if (next != language)
            {
                language = next;
                Relabel();
            }
            shownLow = -1;
        }

        private void Relabel()
        {
            if (root == null) return;
            healthCaption.text = Loc.T("hud.health");
            noiseLabel.text = Loc.T("hud.noise");
            exposureLabel.text = Loc.T("hud.exposure");
            cardinals[0].text = Loc.T("hud.north");
            cardinals[1].text = Loc.T("hud.east");
            cardinals[2].text = Loc.T("hud.south");
            cardinals[3].text = Loc.T("hud.west");
            L("need-hunger-label").text = Loc.T("hud.hunger");
            L("need-thirst-label").text = Loc.T("hud.thirst");
            L("need-fatigue-label").text = Loc.T("hud.fatigue");
            status[0].text = Loc.T("hud.bleeding");
            status[1].text = Loc.T("hud.poison");
            status[3].text = Loc.T("hud.adrenaline");
            status[4].text = Loc.T("hud.burn");
            status[5].text = Loc.T("hud.hungry");
            status[6].text = Loc.T("hud.thirsty");
            status[7].text = Loc.T("hud.exhausted");
            shownInfection = -1;
            shownLamp = -1;
            shownContacts = -1;
            shownTape = -1;
            shownRescue = -1;
            shownTutorial = int.MinValue;
            lastDistrict = null;
            shownBangs = -1;
            shownTimer = -1;
            promptFor = null;
            HandleTension(horde != null ? horde.State : TensionState.Calm);
            HandleObjectives();
            HandleClock();
            HandleWeapon(player != null ? player.ActiveWeapon : null);
            HandleInventory();
        }

        private void HandleHealth(float current, float max)
        {
            healthRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            int shown = Mathf.CeilToInt(current);
            int top = Mathf.CeilToInt(max);
            if (shown != shownHealth)
            {
                shownHealth = shown;
                healthValue.text = HudNumbers.Of(shown);
            }
            if (top != shownMax)
            {
                shownMax = top;
                healthMax.text = "/ " + HudNumbers.Of(top);
            }
            healthFill.style.width = Length.Percent(healthRatio * 100f);
        }

        private void HandleDamaged(float amount, Vector3 point)
        {
            if (life == null || player == null) return;
            var from = life.LastHitDirection;
            if (from.sqrMagnitude < 0.0001f) from = player.transform.position - point;
            var up = view != null ? view.transform.forward : player.transform.forward;
            damageIncoming = StreetHeading.Incoming(up.x, up.z, from.x, from.z);
            damageStrength = DamageEdges.Strength(amount, life.MaxHealth);
            damageAt = Time.unscaledTime;
            var face = player.transform.forward;
            string sector = StreetHeading.Sector(StreetHeading.Incoming(face.x, face.z, from.x, from.z));
            if (settings == null || settings.Subtitles)
            {
                hurt.text = StreetHud.Hit(sector, null);
                hurt.style.backgroundColor = HudPalette.Hurt(vision);
                hurtUntil = Time.unscaledTime + 1.2f;
            }
        }

        private void HandleStamina(float current, float max)
        {
            staminaTarget = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        private void HandleWeapon(WeaponBase weapon)
        {
            if (root == null) return;
            BindGun(weapon as FirearmWeapon);
            if (weapon == null) weaponName.text = StreetHud.None(null);
            else weaponName.text = FightSay.Gun(gun != null ? gun.CardId : WeaponCard.IdFor(weapon.Type), weapon.WeaponName, null);
            Show(mag, gun != null);
            Show(split, gun != null);
            Show(reserve, gun != null);
            shownMag = -1;
            shownReserve = -1;
            shownWeapons = -1;
            if (gun != null) HandleAmmo(gun.CurrentAmmo, gun.ReserveAmmo);
            HandleReload();
        }

        private void HandleAmmo(int current, int spare)
        {
            if (current != shownMag)
            {
                shownMag = current;
                mag.text = HudNumbers.Of(current);
            }
            if (spare != shownReserve)
            {
                shownReserve = spare;
                reserve.text = HudNumbers.Of(spare);
            }
        }

        private void HandleReload()
        {
            bool reloading = gun != null && gun.IsReloading;
            shownReloading = reloading;
            Show(radial, reloading);
            ringSweep = 0f;
            radial.MarkDirtyRepaint();
        }

        private void HandleInventory()
        {
            if (root == null) return;
            belt.text = inventory != null ? inventory.BeltLine : "";
            Show(belt, !string.IsNullOrEmpty(belt.text));
            shownWeapons = -1;
        }

        private void HandleNeeds()
        {
            shownNeeds = -1;
        }

        private void HandleTension(TensionState state)
        {
            if (root == null) return;
            peak = state == TensionState.Peak;
            tensionLabel.text = Loc.T("hud.tension") + "  " + StreetHud.Mood(state.ToString(), null);
            beat.style.backgroundColor = peak ? HudPalette.Alarm(vision) : state == TensionState.BuildUp ? HudPalette.Ask(vision) : HudPalette.Safe(vision);
            if (!peak) beat.style.scale = new Scale(Vector3.one);
        }

        private void HandleObjectives()
        {
            if (root == null) return;
            if (tracker != null)
            {
                quota.text = StreetHud.Quota(tracker.Kills, tracker.KillGoal, tracker.Scrap, tracker.ScrapGoal, null);
                poi.text = tracker.PoiLine();
                if (board != null) board.text = tracker.BoardLines(null);
            }
            else
            {
                quota.text = "";
                poi.text = "";
                if (board != null) board.text = "";
            }
            Show(quota, quota.text.Length > 0);
            Show(poi, !string.IsNullOrEmpty(poi.text));
            if (board != null) Show(board, !string.IsNullOrEmpty(board.text));
        }

        private void HandleClock()
        {
            if (root == null) return;
            clock.text = worldClock != null ? worldClock.Label : "";
            Show(clock, clock.text.Length > 0);
        }

        private void HandleNoise(Vector3 origin, float radius, NoiseType type)
        {
            if (player == null) return;
            Vector3 at = player.transform.position;
            float distance = Vector3.Distance(origin, at);
            if (distance < 1f) noiseLevel = Mathf.Clamp01(radius / 30f);
            if (settings != null && !settings.Subtitles) return;
            bool wall = Physics.Linecast(origin + Vector3.up * 1.2f, at + Vector3.up * 1.2f, GameLayers.EnvironmentMask);
            bool rain = WeatherController.Instance != null && SkyBand.Rains(WeatherController.Instance.Kind);
            if (!CaptionGate.Show(distance, radius, wall, type, rain)) return;
            Vector3 from = origin - at;
            string line = Presentation.Caption(type, from.x, from.z, language);
            if (string.IsNullOrEmpty(line)) return;
            caption = line;
            captionUntil = Time.unscaledTime + CaptionLife;
            subtitle.text = caption;
        }

        private void Toast(string message)
        {
            toasts.Push(message, Time.unscaledTime);
        }

        private void HandleHit(Vector3 point, Vector3 normal, GameObject target)
        {
            if (!CombatEvents.FromWeapon || target == null || player == null) return;
            if (target.transform.IsChildOf(player.transform)) return;
            hitWorld = point;
            hitAt = Time.unscaledTime;
            hitKill = false;
        }

        private void HandleKill(GameObject victim, GameObject killer)
        {
            if (player == null || killer == null || killer != player.gameObject) return;
            if (victim != null) hitWorld = victim.transform.position + Vector3.up;
            hitAt = Time.unscaledTime;
            hitKill = true;
        }

        private void Smooth(float dt)
        {
            ghostRatio = HealthGhost.Follow(ghostRatio, healthRatio, dt);
            healthGhost.style.width = Length.Percent(ghostRatio * 100f);
            staminaShown = Mathf.MoveTowards(staminaShown, staminaTarget, dt * 3f);
            staminaFill.style.width = Length.Percent(staminaShown * 100f);
            if (noiseLevel > 0f) noiseLevel = Mathf.Max(0f, noiseLevel - NoiseDecay * dt);
            if (visibility != null)
            {
                float target = Mathf.Clamp01(visibility.Exposure);
                exposureShown = exposureShown < 0f ? target : Mathf.MoveTowards(exposureShown, target, dt * 2f);
                exposureFill.style.width = Length.Percent(exposureShown * 100f);
            }
            if (veil != null) Show(veil, needs != null && NeedsPressure.Tired(needs.Fatigue));
        }

        private void Meters(float now)
        {
            noiseFill.style.width = Length.Percent(noiseLevel * 100f);
            noiseFill.style.backgroundColor = HudPalette.Noise(vision, noiseLevel);
            noiseFill.parent.style.height = NoiseCue.Height(vision, noiseLevel);
            string mark = NoiseCue.Mark(vision, noiseLevel);
            noiseMark.text = mark;
            Show(noiseMark, mark.Length > 0);
            beat.style.scale = new Scale(Vector3.one * Heartbeat.Scale(now, peak));

            if (needs != null)
            {
                int hunger = Mathf.RoundToInt(needs.Hunger);
                int thirst = Mathf.RoundToInt(needs.Thirst);
                int fatigue = Mathf.RoundToInt(needs.Fatigue);
                int key = hunger * 1000000 + thirst * 1000 + fatigue;
                if (key != shownNeeds)
                {
                    shownNeeds = key;
                    needFills[0].style.width = Length.Percent(Mathf.Clamp(hunger, 0, 100));
                    needFills[1].style.width = Length.Percent(Mathf.Clamp(thirst, 0, 100));
                    needFills[2].style.width = Length.Percent(Mathf.Clamp(100 - fatigue, 0, 100));
                    Flag(5, NeedsPressure.Hungry(needs.Hunger));
                    Flag(6, NeedsPressure.Dry(needs.Thirst));
                    Flag(7, NeedsPressure.Tired(needs.Fatigue));
                }
            }
            if (effects != null)
            {
                Flag(0, effects.IsBleeding);
                Flag(1, effects.IsPoisoned);
                int stage = effects.InfectionStage;
                if (stage != shownInfection)
                {
                    shownInfection = stage;
                    status[2].text = StreetHud.Infection(stage, null);
                    Flag(2, stage > 0);
                }
                Flag(3, effects.SprintBonus > 1f);
                Flag(4, effects.IsBurning);
            }
        }

        private void Flag(int index, bool on)
        {
            if (statusOn[index] == on) return;
            statusOn[index] = on;
            Show(status[index], on);
        }

        private void Weapon(float now)
        {
            if (gun != null)
            {
                bool low = MagPulse.Low(gun.CurrentAmmo, gun.MaxMagazine, gun.IsReloading);
                if ((low ? 1 : 0) != shownLow)
                {
                    shownLow = low ? 1 : 0;
                    mag.style.color = low ? HudPalette.Warn(vision) : new StyleColor(StyleKeyword.Null);
                }
                mag.style.opacity = MagPulse.Alpha(now, low);
                if (gun.IsReloading != shownReloading) HandleReload();
                if (shownReloading && ReloadArc.Moved(ringSweep, gun.ReloadFill))
                {
                    ringSweep = ReloadArc.Sweep(gun.ReloadFill);
                    radial.MarkDirtyRepaint();
                }
            }
            if (player == null) return;
            int count = player.WeaponCount;
            int active = player.ActiveSlot;
            if (count == shownWeapons && active == shownActive) return;
            shownWeapons = count;
            shownActive = active;
            for (int i = 0; i < HudTree.Slots; i++)
            {
                var held = player.WeaponAt(i);
                slots[i].EnableInClassList("hud-slot--active", held != null && i == active);
                slots[i].EnableInClassList("hud-slot--empty", held == null);
                Texture2D icon = null;
                string id = "";
                if (held != null)
                {
                    id = held is FirearmWeapon firearm ? firearm.CardId : WeaponCard.IdFor(held.Type);
                    icon = WeaponSet.Icon(id, held.Type);
                }
                slotIcons[i].style.backgroundImage = icon != null ? new StyleBackground(icon) : new StyleBackground(StyleKeyword.None);
                slotNames[i].text = held == null ? "" : icon != null ? "" : FightSay.Gun(id, held.WeaponName, null);
                wheel[i].text = WeaponWheel.Row(i, held == null ? "" : FightSay.Gun(id, held.WeaponName, null), false, null);
            }
        }

        private void Wheel()
        {
            bool open = player != null && player.WheelOpen;
            if (open != wheelWasOpen)
            {
                wheelWasOpen = open;
                Show(wheelBox, open);
                shownHot = -2;
            }
            if (!open) return;
            int hot = player.WheelSlot;
            if (hot == shownHot) return;
            shownHot = hot;
            for (int i = 0; i < HudTree.Slots; i++) wheel[i].EnableInClassList("hud-wheel-slot--hot", i == hot);
        }

        private void Compass()
        {
            if (!street || player == null) return;
            var face = player.transform.forward;
            var at = player.transform.position;
            float yaw = StreetHeading.Degrees(face.x, face.z);
            float half = HudFit.Compass * 0.5f - 24f;
            for (int i = 0; i < 4; i++)
            {
                float delta = CompassMarks.Cardinal(i, yaw);
                CompassMarks.Place(delta, half, out float x);
                cardinals[i].style.translate = new Translate(x, 0f);
                cardinals[i].style.opacity = CompassMarks.Fade(delta);
            }
            bool showPoi = poiSite != null && (tracker == null || !tracker.PoiFound);
            Show(compassPoi, showPoi);
            if (showPoi) Pin(compassPoi, StreetHeading.Delta(face.x, face.z, at.x, at.z, poiSite.transform.position.x, poiSite.transform.position.z), half);
            var gate = ExtractionZone.Current;
            Show(compassGate, gate != null);
            if (gate != null) Pin(compassGate, StreetHeading.Delta(face.x, face.z, at.x, at.z, gate.transform.position.x, gate.transform.position.z), half);
        }

        private static void Pin(VisualElement marker, float delta, float half)
        {
            bool on = CompassMarks.Place(delta, half, out float x);
            marker.style.translate = new Translate(x, 0f);
            marker.style.opacity = on ? 1f : 0.45f;
        }

        private void Markers(float now)
        {
            float damage = street ? DamageEdges.Fade(now - damageAt, damageStrength) : 0f;
            for (int i = 0; i < 4; i++)
            {
                float weight = damage > 0f ? DamageEdges.Weight(i, damageIncoming) * damage : 0f;
                edges[i].style.opacity = weight;
                if (weight > 0f) edges[i].style.unityBackgroundImageTintColor = HudPalette.Hurt(vision);
            }
            Show(hurt, now < hurtUntil);

            float alpha = street ? HitPip.Alpha(now - hitAt, hitKill) : 0f;
            Show(hitMarker, alpha > 0f);
            if (alpha <= 0f || view == null || root.panel == null) return;
            Vector2 spot = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, hitWorld, view);
            hitMarker.style.left = spot.x;
            hitMarker.style.top = spot.y;
            hitMarker.style.opacity = alpha;
            float spread = HitPip.Spread(now - hitAt, hitKill);
            var tint = hitKill ? HudPalette.Alarm(vision) : Color.white;
            for (int i = 0; i < 4; i++)
            {
                var tick = hitMarker[i];
                float sx = i == 1 || i == 2 ? spread : -spread;
                float sy = i == 2 || i == 3 ? spread : -spread;
                tick.style.translate = new Translate(sx * 0.7f, sy * 0.7f);
                tick.style.backgroundColor = tint;
            }
        }

        private void Toasts(float now)
        {
            toasts.Prune(now);
            bool changed = toasts.Version != shownToasts;
            shownToasts = toasts.Version;
            for (int i = 0; i < HudTree.Toasts; i++)
            {
                string line = toasts.Line(i);
                if (changed)
                {
                    toastLabels[i].text = line ?? "";
                    Show(toastLabels[i], line != null);
                }
                if (line != null) toastLabels[i].style.opacity = toasts.Alpha(i, now);
            }

            bool captions = settings == null || settings.Subtitles;
            Show(subtitle, captions && caption != null && now <= captionUntil);
        }

        private void Prompt(float now)
        {
            var focus = interactor != null ? interactor.Current : null;
            if (!ReferenceEquals(focus, promptFor) || (focus != null && now >= promptAt))
            {
                promptFor = focus;
                promptAt = now + 0.5f;
                string text = focus != null ? focus.Prompt : "";
                prompt.text = string.IsNullOrEmpty(text) ? "" : KeyPrompt.Interact(text);
            }
            bool on = focus != null && prompt.text.Length > 0 && root.panel != null && view != null;
            Show(prompt, on);
            if (!on) return;
            Vector2 spot;
            if (!InputGlyphs.UsingPad)
            {
                Vector2 pointer = ExpeditionInput.Pointer;
                spot = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(pointer.x, Screen.height - pointer.y));
                spot += new Vector2(22f, 18f);
            }
            else
            {
                var anchor = focus as Component;
                Vector3 world = anchor != null ? anchor.transform.position + Vector3.up * 1.6f : player.transform.position + Vector3.up * 2f;
                spot = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, world, view);
                spot += new Vector2(-40f, -30f);
            }
            prompt.style.left = spot.x;
            prompt.style.top = spot.y;
        }

        private void Popups()
        {
            var feedback = HitFeedback.Instance;
            int used = 0;
            if (feedback != null && view != null && root.panel != null)
            {
                feedback.PrunePopups();
                var list = feedback.Popups;
                for (int i = 0; i < list.Count && used < PopupPool; i++)
                {
                    var popup = list[i];
                    Vector3 screen = view.WorldToScreenPoint(popup.World);
                    if (screen.z < 0f) continue;
                    float rise = (0.7f - (popup.Until - Time.time)) * 40f;
                    Vector2 spot = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, popup.World, view);
                    var label = popups[used++];
                    label.text = popup.Text;
                    label.style.left = spot.x;
                    label.style.top = spot.y - rise;
                    label.style.color = popup.Crit ? HudPalette.Crit(vision) : Color.white;
                    label.style.display = DisplayStyle.Flex;
                }
            }
            for (int i = used; i < PopupPool; i++) popups[i].style.display = DisplayStyle.None;
        }

        private void Slow()
        {
            if (street && player != null)
            {
                ZombieAI.CountAlerts(player.transform.position.x, player.transform.position.z, out int bangs, out int questions);
                if (bangs != shownBangs || questions != shownQuestions)
                {
                    shownBangs = bangs;
                    shownQuestions = questions;
                    threats.text = ThreatMark.Line(bangs, questions);
                    threats.style.color = bangs > 0 ? HudPalette.Alarm(vision) : HudPalette.Ask(vision);
                    Show(threats, threats.text.Length > 0);
                }
            }

            var gm = GameManager.Instance;
            int tape = gm != null ? gm.KillTapeVersion : 0;
            if (tape != shownTape)
            {
                shownTape = tape;
                killFeed.text = gm != null ? gm.KillFeed : "";
                Show(killFeed, killFeed.text.Length > 0);
            }

            int rescueKey = RescueFollower.StatusKey();
            if (rescueKey != shownRescue)
            {
                shownRescue = rescueKey;
                rescue.text = RescueFollower.Status();
                Show(rescue, rescue.text.Length > 0);
            }

            var map = WorldMapService.Instance;
            var here = map != null ? map.Current : null;
            string hereId = here != null ? here.id : "";
            if (!ReferenceEquals(hereId, lastDistrict))
            {
                lastDistrict = hereId;
                district.text = here != null ? StreetAsk.Place(here.id, here.displayName, here.encounter, null) : "";
                Show(district, district.text.Length > 0);
            }

            Timers();
            Tutorial();
            Lamp();

            if (street && Time.unscaledTime >= poiAt)
            {
                poiAt = Time.unscaledTime + 1f;
                if (poiSite == null) poiSite = FindFirstObjectByType<DistrictPoi>();
            }

            Show(watch, AiWatch.Open);
            if (AiWatch.Open)
            {
                var rows = new string[12];
                int count = ZombieAI.CopyWatch(rows, Time.time);
                var shown = new string[count];
                for (int i = 0; i < count; i++) shown[i] = rows[i];
                watch.text = AiWatch.Page(shown, 6, null) + "\n" + VfxLedger.Line(null);
            }
        }

        private void Timers()
        {
            var raid = NightRaidController.Instance;
            var gate = ExtractionZone.Current;
            int key = -1;
            if (raid != null && raid.Running) key = 100000 + Mathf.CeilToInt(raid.Remaining);
            else if (gate != null && gate.Holding) key = 200000 + Mathf.CeilToInt(ExtractWatch.HoldSeconds - gate.Hold);
            if (key == shownTimer) return;
            shownTimer = key;
            if (key < 0) timer.text = "";
            else if (key < 200000) timer.text = StreetHud.Raid(key - 100000, null);
            else timer.text = StreetHud.Hold(key - 200000, null);
            Show(timer, timer.text.Length > 0);
        }

        private void Tutorial()
        {
            var guide = TutorialDirector.Instance;
            bool captions = settings == null || settings.Subtitles;
            int key = guide == null || guide.Finished || !captions || !street ? -1 : guide.Index;
            if (key == shownTutorial) return;
            shownTutorial = key;
            tutorial.text = key >= 0 ? guide.Current : "";
            Show(tutorial, tutorial.text.Length > 0);
        }

        private void Lamp()
        {
            int lamp = -1;
            if (player != null && (player.FlashlightOn || player.LampCellCharge < LampCell.Full - 0.1f)) lamp = Mathf.CeilToInt(player.LampCellCharge);
            if (lamp != shownLamp)
            {
                shownLamp = lamp;
                if (lamp >= 0) status[8].text = Loc.T("hud.lamp") + " " + HudNumbers.Of(lamp);
                Flag(8, lamp >= 0);
            }
            var services = CampServices.Instance;
            int contacts = services != null ? services.Contacts : 0;
            if (contacts != shownContacts)
            {
                shownContacts = contacts;
                if (contacts > 0) status[9].text = StreetHud.Watch(contacts, null);
                Flag(9, contacts > 0);
            }
        }

        private void DrawRing(MeshGenerationContext context)
        {
            var box = context.visualElement.contentRect;
            float radius = Mathf.Min(box.width, box.height) * 0.5f - 3f;
            if (radius <= 0f) return;
            var paint = context.painter2D;
            paint.lineWidth = 4f;
            paint.lineCap = LineCap.Butt;
            paint.strokeColor = new Color(1f, 1f, 1f, 0.16f);
            paint.BeginPath();
            paint.Arc(box.center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            paint.Stroke();
            if (ringSweep <= 0f) return;
            paint.strokeColor = new Color(0.44f, 0.8f, 0.75f, 1f);
            paint.BeginPath();
            paint.Arc(box.center, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + ringSweep));
            paint.Stroke();
        }

        private static void Show(VisualElement element, bool on)
        {
            if (element == null) return;
            var want = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (element.style.display.value != want || element.style.display.keyword != StyleKeyword.Undefined)
                element.style.display = want;
        }
    }
}
