using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FixThisHouse.Shatter
{
    /// <summary>
    /// Add BreakHouse script to a house and it will shatter child meshes with Rigidbodies
    /// </summary>
    public class BreakHouse : MonoBehaviour
    {
        private Rigidbody[] arrayRb;
        private List<BreakBlock> _blocks = new();

        private void Awake()
        {
            arrayRb = GetComponentsInChildren<Rigidbody>();

            for(var i=0;i< arrayRb.Length; i++)
            {
                var rb = arrayRb[i];
                if (rb.TryGetComponent<Block>(out var block))
                    block.enabled = false;
            }
        }

        [ContextMenu("Explode")]
        public void Explode()
        {
            ExplodeAt(null);
        }

        public void ExplodeAt(Transform hit)
        {
            if (arrayRb == null || arrayRb.Length == 0)
                return;

            var index = Random.Range(0, arrayRb.Length - 1);

            for (var i = 0; i < arrayRb.Length; i++)
            {
                var rb = arrayRb[i];
                var block = rb.gameObject.AddComponent<BreakBlock>();
                block.SetFixed(i == 0);

                if (rb.transform == hit)
                    index = i;

                _blocks.Add(block);
            }

            _blocks[index].Explode();

            StartCoroutine(DelayExplosion());
        }

        private IEnumerator DelayExplosion()
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 1f));

            if (_blocks.Count > 0 && _blocks[0] != null && !_blocks[0].IsExploded)
                _blocks[0].Explode();
        }
    }
}
