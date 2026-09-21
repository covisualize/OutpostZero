using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Graphics
{
    public class LightSource : MonoBehaviour
    {
        public static readonly List<LightSource> All = new List<LightSource>();

        [SerializeField] private float radius = 9f;
        public float Radius => radius;

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        public void Configure(float lightRadius)
        {
            radius = lightRadius;
        }
    }
}
