using UnityEngine;

namespace FixThisHouse
{
    /// <summary>
    /// Added to freshly instantiated objects, so we can link back
    /// to the correct pool on despawn.
    /// </summary>
    public class PoolMember : MonoBehaviour
    {
        public Component Obj;
        public PrefabPool MyPool;

        public void Release()
        {
            MyPool.Release(Obj);
        }
    }
}
