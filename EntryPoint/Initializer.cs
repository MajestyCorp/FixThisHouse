using UnityEngine;

namespace FixThisHouse
{
    /// <summary>
    /// It's a first executed script in the game!
    /// </summary>
    public class Initializer : MonoBehaviour
    {
        private void Awake()
        {
            var array = GetComponentsInChildren<IInitializer>();

            if(array != null)
            {
                for (var i = 0; i < array.Length; i++)
                    array[i].InitializeSelf();

                for (var i = 0; i < array.Length; i++)
                    array[i].InitializeAfter();
            }
        }
    }
}
