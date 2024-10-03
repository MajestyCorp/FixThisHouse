using FixThisHouse.Profiles;
using FixThisHouse.Seasons;
using FixThisHouse.Shatter;
using FixThisHouse.Voxels;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FixThisHouse.Game
{
    public class LevelManager : MonoBehaviour, IInitializer
    {
        public delegate void VoidHandler();
        public event VoidHandler OnTilesAnimated;


        public static LevelManager Instance { get; private set; }
        public Vector3 CenterOfLevel { get; private set; }
        public Vector2 MaxBounds { get; private set; }
        public SeasonProfile SeasonProfile => _prefab.Profile;
        public IReadOnlyList<Level> Levels => levels;
        public Level CurrentLevel => _currentLevel;

        private const string keyLastLevel = "LastLevel";


        [SerializeField]
        private Level startLevel;
        [SerializeField]
        private List<Level> levels;

        private Level _prefab;
        private Level _currentLevel;

        public void InitializeAfter()
        {
        }

        public void InitializeSelf()
        {
            Instance = this;
        }

        public bool IsActiveLevel(Level prefab)
        {
            return prefab == _prefab;
        }

        public IEnumerator AnimateRestart()
        {
            yield return null;

            DisableBlocks(_currentLevel);

            SoundManager.Instance.MuteNatureMusic();
            var cor1 = StartCoroutine(AnimatePopInHouses(_currentLevel.PopInHouses, 0));//0.1f
            var cor2 = StartCoroutine(AnimatePopInPlatforms(_currentLevel.Platforms, 0));
            var cor3 = StartCoroutine(AnimatePopInBlocks(_currentLevel.Blocks, 0));

            yield return cor1;
            yield return cor2;
            yield return cor3;

            var oldLevel = _currentLevel;

            _currentLevel = Instantiate(_prefab);
            _currentLevel.gameObject.SetActive(false);


            yield return PrepareNewLevel(_currentLevel);

            OnTilesAnimated?.Invoke();

            yield return AnimateTiles(oldLevel, _currentLevel);
            yield return PostpareNewLevel(_currentLevel);

            SoundManager.Instance.UnmuteNatureMusic();

            oldLevel.BeforeDestroy();
            Destroy(oldLevel.gameObject);

            cor1 = StartCoroutine(AnimatePopHouses(_currentLevel, 0));
            cor2 = StartCoroutine(FallBlocks(_currentLevel, 0));

            yield return cor1;
            yield return cor2;
        }

        public bool IsReady()
        {
            return _currentLevel.IsReady();
        }

        public IEnumerator AnimateSelectedLevel(Level level)
        {
            yield return null;

            SharedSettings.Set(keyLastLevel, levels.IndexOf(level));
            DisableBlocks(_currentLevel);


            SoundManager.Instance.MuteNatureMusic();
            var cor1 = StartCoroutine(AnimatePopInHouses(_currentLevel.PopInHouses, 0));//0.1f
            var cor2 = StartCoroutine(AnimatePopInPlatforms(_currentLevel.Platforms, 0));
            var cor3 = StartCoroutine(AnimatePopInBlocks(_currentLevel.Blocks, 0));

            yield return cor1;
            yield return cor2;
            yield return cor3;

            //yield return new WaitForSeconds(1f);

            _prefab = level;

            var oldLevel = _currentLevel;
            _currentLevel = Instantiate(_prefab);
            _currentLevel.gameObject.SetActive(false);
            yield return PrepareNewLevel(_currentLevel);

            OnTilesAnimated?.Invoke();

            yield return AnimateTiles(oldLevel, _currentLevel);
            yield return PostpareNewLevel(_currentLevel);

            SoundManager.Instance.UnmuteNatureMusic();

            oldLevel.BeforeDestroy();
            Destroy(oldLevel.gameObject);

            cor1 = StartCoroutine(AnimatePopHouses(_currentLevel, 0));
            cor2 = StartCoroutine(FallBlocks(_currentLevel, 0));

            yield return cor1;
            yield return cor2;

            //yield return new WaitForSeconds(1f);
            //yield return FallBlocks(_currentLevel, 0);
        }

        private IEnumerator AnimatePopHouses(Level level, float delay = 0f)
        {
            var list = new List<PopHouse>(level.PopHouses);
            list.Shuffle();

            while (list.Count > 0)
            {
                var index = list.Count - 1;

                if (delay > 0)
                    yield return new WaitForSeconds(UnityEngine.Random.Range(0.01f, delay));
                else
                    yield return new WaitForEndOfFrame();

                var house = list[index];
                house.Animate();
                list.RemoveAt(index);
            }
        }

        private IEnumerator PostpareNewLevel(Level level)
        {
            var platforms = level.Platforms;
            platforms[0].transform.parent.gameObject.SetActive(true);

            level.SetKinematicTrees(false);

            yield return null;
        }

        private IEnumerator AnimateTiles(Level oldLevel, Level newLevel)
        {
            SeasonManager.Instance.PlayProfile(newLevel.Profile);
            yield return TransitionManager.Instance.AnimateLevels(oldLevel, newLevel);
        }

        private IEnumerator PrepareNewLevel(Level level)
        {
            yield return null;

            level.SetKinematicTrees(true);

            yield return null;

            var houses = level.PopHouses;
            for (var i = 0; i < houses.Count; i++)
                houses[i].PrepareBlocks();

            yield return null;

            var platforms = level.Platforms;
            platforms[0].transform.parent.gameObject.SetActive(false);

            yield return null;

            var blocks = level.Blocks;
            for (var i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if(block.IsRoot)
                    block.gameObject.SetActive(false);
            }

            yield return null;
        }

        private IEnumerator AnimatePopInBlocks(IReadOnlyList<Block> blocks, float delay = 0f)
        {
            var list = new List<Block>(blocks);
            list.Shuffle();

            while(list.Count > 0)
            {
                var index = list.Count - 1;
                var block = list[index];

                if (block != null && !block.IsAttached && block.IsRoot)
                {
                    if (delay > 0)
                        yield return new WaitForSeconds(UnityEngine.Random.Range(0.01f, delay));
                    else
                        yield return new WaitForEndOfFrame();

                    var popIn = block.gameObject.AddComponent<PopInHouse>();
                    popIn.Animate();
                }

                list.RemoveAt(index);
            }
        }

        private IEnumerator AnimatePopInPlatforms(IReadOnlyList<Platform> platforms, float delay = 0f)
        {
            var list = new List<PlatformItem>();
            float totalTimestamp = 0f;

            for (var i = 0; i < platforms.Count; i++)
            {
                var platform = platforms[i];
                list.Clear();
                list.AddRange(platform.Items);
                list.Shuffle();

                while (list.Count > 0)
                {
                    var index = list.Count - 1;
                    var item = list[index];
                    var popIn = item.gameObject.AddComponent<PopInHouse>();

                    if (delay > 0)
                        yield return new WaitForSeconds(UnityEngine.Random.Range(0.01f, delay));
                    else
                        yield return new WaitForEndOfFrame();

                    popIn.Animate();
                    totalTimestamp = Math.Max(totalTimestamp, Time.time + popIn.TotalAnimationTime);
                    list.RemoveAt(index);
                }

                if (delay > 0)
                    yield return new WaitForSeconds(UnityEngine.Random.Range(0.01f, delay));
                else
                    yield return new WaitForEndOfFrame();
            }

            if(totalTimestamp > Time.time)
                yield return new WaitForSeconds(totalTimestamp - Time.time);
        }

        private IEnumerator AnimatePopInHouses(IReadOnlyList<PopInHouse> items, float delay = 0f)
        {
            var list = new List<PopInHouse>(items);
            var totalTimestamp = 0f;
            list.Shuffle();

            while(list.Count > 0)
            {
                var index = list.Count - 1;

                if (delay > 0)
                    yield return new WaitForSeconds(UnityEngine.Random.Range(0.01f, delay));
                else
                    yield return new WaitForEndOfFrame();

                var house = list[index];
                house.Animate();
                totalTimestamp = Math.Max(totalTimestamp, Time.time + house.TotalAnimationTime);
                list.RemoveAt(index);
            }

            if(totalTimestamp > Time.time)
                yield return new WaitForSeconds(totalTimestamp - Time.time);
        }

        private void DisableBlocks(Level level)
        {
            var blocks = level.Blocks;

            for(var i=0;i<blocks.Count;i++)
            {
                blocks[i].enabled = false;
            }
        }

        public IEnumerator Initialize()
        {
            SeasonManager.Instance.SetInitialProfile();

            //yield return null;

            var index = SharedSettings.GetInt(keyLastLevel);
            if (index >= 0 && index < levels.Count)
                _prefab = levels[index];
            else
                _prefab = startLevel;
            
            _currentLevel = Instantiate(_prefab);

            //yield return null;

            yield return FallBlocks(_currentLevel);
        }

        private IEnumerator FallBlocks(Level level, float delay = 0f)
        {
            var bounds = GetMapBounds(level.Map);
            CenterOfLevel = new Vector3(bounds.center.x, 0f, bounds.center.z);
            MaxBounds = new Vector2(bounds.size.x + 3f, bounds.size.z + 3f);

            yield return null;

            var blocks = level.Blocks;
            var extents = bounds.extents;
            var center = bounds.center;

            for(var i=0;i<blocks.Count;i++)
            {
                var block = blocks[i];

                if (!block.IsRoot)
                    continue;

                block.transform.position = new Vector3(
                    center.x - UnityEngine.Random.Range(-extents.x, extents.x),
                    10f,//level.MaxHeight, 
                    center.z - UnityEngine.Random.Range(-extents.z, extents.z));
                block.transform.rotation = UnityEngine.Random.rotation;
                block.SetTurn((EBlockTurn)UnityEngine.Random.Range(0, 4));
                block.SetKinematic(false);

                if (!block.gameObject.activeSelf)
                    block.gameObject.SetActive(true);

                if (delay > 0)
                    yield return new WaitForSeconds(UnityEngine.Random.Range(0.01f, delay));
                else
                    yield return new WaitForEndOfFrame();
            }
        }

        private Bounds GetMapBounds(Transform map)
        {
            var min = Vector3.one * 10f;
            var max = Vector3.zero;

            for(var i=0;i<map.childCount;i++)
            {
                var child = map.GetChild(i);

                if (!child.TryGetComponent<Voxel>(out _))
                    continue;

                min.x = Mathf.Min(min.x, child.localPosition.x);
                min.z = Mathf.Min(min.z, child.localPosition.z);
                max.x = Mathf.Max(max.x, child.localPosition.x);
                max.z = Mathf.Max(max.z, child.localPosition.z);
            }

            return new Bounds((min + max - Vector3.one * 1.5f) * 0.5f, max - min - Vector3.one * 1.5f);
        }
    }
}
