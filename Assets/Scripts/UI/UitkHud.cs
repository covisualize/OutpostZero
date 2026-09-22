using UnityEngine;
using UnityEngine.UIElements;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.UI
{
    public class UitkHud : MonoBehaviour
    {
        public static bool Live { get; private set; }

        private UIDocument document;
        private Label health;
        private Label ammo;
        private VisualElement healthFill;

        private void Start()
        {
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            var root = document.rootVisualElement;
            if (root == null)
            {
                Live = false;
                return;
            }

            root.pickingMode = PickingMode.Ignore;
            var column = new VisualElement();
            column.style.position = Position.Absolute;
            column.style.left = 16;
            column.style.top = 140;
            column.style.width = 280;
            health = new Label("Health");
            health.style.color = Color.white;
            health.style.unityFontStyleAndWeight = FontStyle.Bold;
            ammo = new Label("Ammo");
            ammo.style.color = Color.white;
            healthFill = new VisualElement();
            healthFill.style.height = 8;
            healthFill.style.backgroundColor = new Color(0.7f, 0.15f, 0.12f);
            column.Add(health);
            column.Add(healthFill);
            column.Add(ammo);
            root.Add(column);
            Live = true;
        }

        private void Update()
        {
            if (!Live || health == null) return;
            var player = PlayerRegistry.Current;
            if (player == null) return;
            var life = player.GetComponent<HealthSystem>();
            if (life != null)
            {
                health.text = "HP " + Mathf.CeilToInt(life.CurrentHealth);
                healthFill.style.width = Length.Percent(100f * life.CurrentHealth / Mathf.Max(1f, life.MaxHealth));
            }
            if (player.ActiveWeapon is FirearmWeapon gun)
            {
                ammo.text = FightSay.Gun(gun.CardId, gun.WeaponName, null) + "  " + gun.CurrentAmmo + " / " + gun.ReserveAmmo;
            }
            else if (player.ActiveWeapon != null)
            {
                ammo.text = FightSay.Gun(WeaponCard.IdFor(player.ActiveWeapon.Type), player.ActiveWeapon.WeaponName, null);
            }
        }

        private void OnDestroy()
        {
            Live = false;
        }
    }
}
