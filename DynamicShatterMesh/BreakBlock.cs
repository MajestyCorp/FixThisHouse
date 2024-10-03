using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FixThisHouse.Shatter
{
    [RequireComponent(typeof(Rigidbody))]
    public class BreakBlock : MonoBehaviour
    {
        public bool IsExploded => _exploded;
        public Rigidbody Rigidbody => _rb;

        private LayerMask voxelMask = (1 << Layers.Voxel) | (1 << Layers.Default);
        private LayerMask buildingMask = 1 << Layers.Building;

        private Rigidbody _rb;
        private bool _exploded;

        public void SetFixed(bool value)
        {
            _rb.isKinematic = value;
        }

        public void Explode()
        {
            if (_exploded)
                return;

            SoundManager.Instance.Play2D(SoundManager.Instance.StoneBreak);
            _exploded = true;

            BreakHouses.Instance.Explode(transform);
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _exploded = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            var hittedLayer = 1 << collision.gameObject.layer;
            var otherRb = collision.rigidbody;

            if ( !_rb.isKinematic && !_exploded && ( 
                    ( _rb.velocity.sqrMagnitude > 1f && ((buildingMask.value & hittedLayer) > 0) ) ||
                    ((voxelMask.value & hittedLayer) > 0) ) )
            {
                Explode();
            }
        }
    }
}
