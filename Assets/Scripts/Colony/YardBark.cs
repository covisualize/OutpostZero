using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A bark bubble over a yard colonist: a small billboarded text line held above the body while
    /// <see cref="YardBubble"/> says it is their turn to speak. It is its own object so the body's squat and lean
    /// never stretch the text.
    /// </summary>
    public class YardBark : MonoBehaviour
    {
        private TextMesh text;
        private Transform follow;
        private float lift = YardBubble.Height;

        public static YardBark Raise(Transform body)
        {
            var holder = new GameObject("YardBark");
            var bark = holder.AddComponent<YardBark>();
            bark.follow = body;
            bark.text = holder.AddComponent<TextMesh>();
            bark.text.anchor = TextAnchor.LowerCenter;
            bark.text.alignment = TextAlignment.Center;
            bark.text.characterSize = 0.06f;
            bark.text.fontSize = 48;
            bark.text.color = new Color(0.96f, 0.92f, 0.8f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                bark.text.font = font;
                var renderer = holder.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = font.material;
            }
            holder.SetActive(false);
            return bark;
        }

        public void Say(bool up, string line, float height)
        {
            lift = height;
            if (!up || string.IsNullOrEmpty(line) || follow == null)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (text.text != line) text.text = line;
            Hold();
        }

        private void LateUpdate()
        {
            Hold();
        }

        private void Hold()
        {
            if (follow == null) return;
            transform.position = follow.position + Vector3.up * lift;
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
