using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FixThisHouse.Shatter
{
    public class BreakHouses : MonoBehaviour, IInitializer
    {
        public static BreakHouses Instance { get; private set; }

        [SerializeField]
        private ShatterPiece piecePrefab;
        [SerializeField, Range(0f, 1f)]
        private float size = 0.2f;
        [SerializeField]
        private List<Transform> blocks;

        private Dictionary<Mesh, PrefabPool> _meshToPool = new();

        private List<Vector3> _dots = new();
        private int _maxX, _maxY, _maxZ;
        private Dictionary<int, List<Face>> _groupToFaces = new();

        public void InitializeAfter()
        {
        }

        public void InitializeSelf()
        {
            Instance = this;

            for (var i = 0; i < blocks.Count; i++)
                CacheBlock(blocks[i]);
        }

        public void Explode(Transform target)
        {
            if (!target.TryGetComponent(out MeshFilter filter))
                filter = target.transform.GetChild(0).GetComponent<MeshFilter>();

            var mesh = filter.sharedMesh;

            if(_meshToPool.TryGetValue(mesh, out var pool))
            {
                var block = pool.Take<ShatterBlock>();
                block.InitFrom(target, filter);
            }

            Destroy(target.gameObject);
        }

        private void CacheBlock(Transform block)
        {
            if (!block.TryGetComponent(out MeshFilter filter))
                filter = block.transform.GetChild(0).GetComponent<MeshFilter>();

            var mesh = filter.sharedMesh;

            if (!_meshToPool.TryGetValue(mesh, out var pool))
            {
                _maxX = _maxZ = 2;
                _maxY = mesh.bounds.size.y > 0.5f ? 2 : 1;

                var shatterBlock = CreateBlock(filter, mesh);
                shatterBlock.InitOffset(block.transform, filter.transform);

                pool = new PrefabPool(shatterBlock, transform);
                _meshToPool[mesh] = pool;
            }
        }

        private ShatterBlock CreateBlock(MeshFilter filter, Mesh mesh)
        {
            InitDots(mesh);
            SplitFaces(mesh);

            var block = CreateShatterBlock();

            ConstructFaces(block, mesh);

            return block;
        }

        private void ConstructFaces(ShatterBlock block, Mesh mesh)
        {
            var bounds = mesh.bounds;
            var extents = bounds.extents;
            var uvs = mesh.uv;
            var s = bounds.size;
            var extrudeSize = ((s.x + s.y + s.z) / 3) * 0.22f;

            var center = bounds.center + new Vector3(
                    Random.Range(-0.5f, 0.5f) * extents.x,
                    Random.Range(-0.5f, 0.5f) * extents.y,
                    Random.Range(-0.5f, 0.5f) * extents.z
                );

            var sideToCount = new Dictionary<Side, int>();

            for (var i = 0; i < _dots.Count; i++)
            {
                if (!_groupToFaces.TryGetValue(i, out var faces))
                    continue;

                FillSideCount(faces, sideToCount);

                var facesCount = faces.Count;
                var averageCenter = Vector3.zero;
                var totalArea = 0f;
                var min = Vector3.one * 10f;
                var max = Vector3.zero;
                var averageNormals = Vector3.zero;

                for (var j = 0; j < facesCount; j++)
                {
                    var face = faces[j];

                    var area = face.Area;
                    averageCenter += area * face.Center;
                    totalArea += area;
                    averageNormals += area * face.Normal;

                    min.x = Mathf.Min(min.x, face.A.x, face.B.x, face.C.x);
                    min.y = Mathf.Min(min.y, face.A.y, face.B.y, face.C.y);
                    min.z = Mathf.Min(min.z, face.A.z, face.B.z, face.C.z);
                    max.x = Mathf.Max(max.x, face.A.x, face.B.x, face.C.x);
                    max.y = Mathf.Max(max.y, face.A.y, face.B.y, face.C.y);
                    max.z = Mathf.Max(max.z, face.A.z, face.B.z, face.C.z);
                }

                var size = max - min;

                averageCenter /= totalArea;
                averageNormals /= totalArea;

                Vector3 backPoint = min + (max - min) * 0.5f;//averageCenter

                if (size.x < 0.01f || size.y < 0.01f || size.z < 0.01f)
                    backPoint += (center - averageCenter).normalized * extrudeSize;

                if ((faces[0].Normal - averageNormals.normalized).magnitude < 0.05f)
                {
                    var extrude = ((size.x + size.y + size.z) / 3) * 0.22f;
                    backPoint = averageCenter - averageNormals * extrude;
                }

                CreateMesh(block, faces, sideToCount, backPoint, uvs);
            }
        }

        private void CreateMesh(ShatterBlock block, List<Face> faces, Dictionary<Side, int> sideToCount, Vector3 backPoint, Vector2[] meshUV)
        {
            var piece = Instantiate(piecePrefab, block.transform);

            var mesh = new Mesh();
            List<Vector3> vertices = new();
            List<int> triangles = new();
            List<Vector2> uvs = new();
            List<Vector3> normals = new();

            FillFaces(faces, meshUV, vertices, triangles, normals, uvs);
            CompleteFaces(faces, sideToCount, backPoint, vertices, triangles, normals, uvs);

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();

            piece.SetMesh(mesh);

            block.AddPiece(piece);
        }

        private void CompleteFaces(List<Face> faces, Dictionary<Side, int> sideToCount, Vector3 backPoint, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs)
        {
            var facesCount = faces.Count;

            for (var j = 0; j < facesCount; j++)
            {
                var face = faces[j];
                var ab = new Side(face.A, face.B);
                var ac = new Side(face.A, face.C);
                var bc = new Side(face.B, face.C);
                var tris = face.Triangle;

                if (sideToCount[ab] == 1)
                {
                    AddTriangle(face.B, face.A, backPoint, tris.y, tris.x, vertices, triangles, normals, uvs);
                    AddTriangle(face.A, face.B, backPoint, tris.x, tris.y, vertices, triangles, normals, uvs);
                    faces.Add(new Face(face.A, face.B, backPoint, 0, 0, 0));
                }

                if (sideToCount[ac] == 1)
                {
                    AddTriangle(face.A, face.C, backPoint, tris.x, tris.z, vertices, triangles, normals, uvs);
                    AddTriangle(face.C, face.A, backPoint, tris.z, tris.x, vertices, triangles, normals, uvs);
                    faces.Add(new Face(face.A, face.C, backPoint, 0, 0, 0));
                }

                if (sideToCount[bc] == 1)
                {
                    AddTriangle(face.C, face.B, backPoint, tris.z, tris.y, vertices, triangles, normals, uvs);
                    AddTriangle(face.B, face.C, backPoint, tris.y, tris.z, vertices, triangles, normals, uvs);
                    faces.Add(new Face(face.B, face.C, backPoint, 0, 0, 0));
                }
            }
        }

        private void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2, int t0, int t1, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs)
        {
            var t2 = -1;
            var uv0 = uvs[t0];
            var uv1 = uvs[t1];
            var uv2 = (uv0 + uv1) * 0.5f;
            var newFace = new Face(v0, v1, v2);
            var normal = newFace.Normal;

            for (var j = 0; j < vertices.Count; j++)
            {
                var v = vertices[j];
                var uv = uvs[j];
                var n = normals[j];

                if (t2 < 0 && v == v2 && uv == uv2 && n == normal)
                {
                    t2 = j;
                    break;
                }
            }

            t0 = vertices.Count;
            vertices.Add(v0);
            uvs.Add(uv0);
            normals.Add(normal);

            t1 = vertices.Count;
            vertices.Add(v1);
            uvs.Add(uv1);
            normals.Add(normal);

            if (t2 < 0)
            {
                t2 = vertices.Count;
                vertices.Add(v2);
                uvs.Add(uv2);
                normals.Add(normal);
            }

            triangles.Add(t0);
            triangles.Add(t1);
            triangles.Add(t2);
        }

        private void FillFaces(List<Face> faces, Vector2[] meshUV, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs)
        {
            for (var i = 0; i < faces.Count; i++)
            {
                var face = faces[i];
                var t0 = -1;
                var t1 = -1;
                var t2 = -1;
                var uv0 = meshUV[face.Triangle.x];
                var uv1 = meshUV[face.Triangle.y];
                var uv2 = meshUV[face.Triangle.z];
                var normal = face.Normal;

                //find already exist vertices?
                for (var j = 0; j < vertices.Count; j++)
                {
                    var v = vertices[j];
                    var uv = uvs[j];
                    var n = normals[j];

                    if (t0 < 0 && v == face.A && uv == uv0 && n == normal)
                    {
                        t0 = j;
                        break;
                    }

                    if (t1 < 0 && v == face.B && uv == uv1 && n == normal)
                    {
                        t1 = j;
                        break;
                    }

                    if (t2 < 0 && v == face.C && uv == uv2 && n == normal)
                    {
                        t2 = j;
                        break;
                    }
                }

                if (t0 < 0)
                {
                    t0 = vertices.Count;
                    vertices.Add(face.A);
                    uvs.Add(uv0);
                    normals.Add(normal);
                }

                if (t1 < 0)
                {
                    t1 = vertices.Count;
                    vertices.Add(face.B);
                    uvs.Add(uv1);
                    normals.Add(normal);
                }

                if (t2 < 0)
                {
                    t2 = vertices.Count;
                    vertices.Add(face.C);
                    uvs.Add(uv2);
                    normals.Add(normal);
                }

                triangles.Add(t0);
                triangles.Add(t1);
                triangles.Add(t2);

                face.SetTriangle(t0, t1, t2);
                faces[i] = face;
            }
        }

        private void FillSideCount(List<Face> faces, Dictionary<Side, int> sideToCount)
        {
            sideToCount.Clear();

            for (var i = 0; i < faces.Count; i++)
            {
                var face = faces[i];
                var ab = new Side(face.A, face.B);
                var ac = new Side(face.A, face.C);
                var bc = new Side(face.B, face.C);

                if (sideToCount.TryGetValue(ab, out var value))
                    sideToCount[ab] = value + 1;
                else
                    sideToCount[ab] = 1;

                if (sideToCount.TryGetValue(ac, out value))
                    sideToCount[ac] = value + 1;
                else
                    sideToCount[ac] = 1;

                if (sideToCount.TryGetValue(bc, out value))
                    sideToCount[bc] = value + 1;
                else
                    sideToCount[bc] = 1;
            }
        }

        private ShatterBlock CreateShatterBlock()
        {
            var go = new GameObject("Shatter");
            var tr = go.transform;

            go.SetActive(false);
            tr.SetParent(transform);

            var block = go.AddComponent<ShatterBlock>();
            block.Initialize();

            return block;
        }

        private void SplitFaces(Mesh mesh)
        {
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;

            _groupToFaces.Clear();

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var t0 = triangles[i];
                var t1 = triangles[i + 1];
                var t2 = triangles[i + 2];

                var face = new Face(vertices[t0], vertices[t1], vertices[t2], t0, t1, t2);

                var center = face.Center;
                var index = 0;
                var dist = (center - _dots[0]).sqrMagnitude;

                for (var j = 1; j < _dots.Count; j++)
                {
                    var otherDist = (center - _dots[j]).sqrMagnitude;
                    if (otherDist < dist)
                    {
                        index = j;
                        dist = otherDist;
                    }
                }

                if (!_groupToFaces.TryGetValue(index, out var list))
                {
                    list = new();
                    _groupToFaces[index] = list;
                }
                list.Add(face);
            }
        }

        private void InitDots(Mesh mesh)
        {
            var bounds = mesh.bounds;
            var e = bounds.extents * size;

            _dots.Clear();

            for (var x = 0; x < _maxX; x++)
                for (var y = 0; y < _maxY; y++)
                    for (var z = 0; z < _maxZ; z++)
                    {
                        var tx = _maxX <= 1 ? 0.5f : x / (_maxX - 1);
                        var ty = _maxY <= 1 ? 0.5f : y / (_maxY - 1);
                        var tz = _maxZ <= 1 ? 0.5f : z / (_maxZ - 1);
                        var pos = new Vector3(
                                Mathf.Lerp(-e.x, e.x, tx),
                                Mathf.Lerp(-e.y, e.y, ty),
                                Mathf.Lerp(-e.z, e.z, tz)
                            );
                        _dots.Add(pos + bounds.center);
                    }
            
        }

#if UNITY_EDITOR
        [ContextMenu("Load Prefabs")]
        private void LoadPrefabs()
        {
            blocks.Clear();

            var b = Directory.GetFiles("Assets/Prefabs/Buildings", "*.prefab", SearchOption.AllDirectories);
            AddAssets(b, blocks);
            b = Directory.GetFiles("Assets/Prefabs/CornerBuildings", "*.prefab", SearchOption.AllDirectories);
            AddAssets(b, blocks);
            b = Directory.GetFiles("Assets/Prefabs/Generated", "*.prefab", SearchOption.AllDirectories);
            AddAssets(b, blocks);

            b = Directory.GetFiles("Assets/Prefabs/Flats", "*.prefab", SearchOption.AllDirectories);
            AddAssets(b, blocks);

            b = Directory.GetFiles("Assets/Prefabs/FlatDetails", "*.prefab", SearchOption.AllDirectories);
            AddAssets(b, blocks);

            b = Directory.GetFiles("Assets/Prefabs/Roofs", "*.prefab", SearchOption.AllDirectories);
            AddAssets(b, blocks);
        }

        private void AddAssets(string[] files, List<Transform> list)
        {
            if (files == null)
                return;

            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var go = (GameObject)AssetDatabase.LoadAssetAtPath(file, typeof(GameObject));

                if (go.TryGetComponent<Block>(out _))
                    list.Add(go.transform);
            }
        }
#endif
    }

    public struct Side
    {
        public readonly Vector3 A, B;

        public Side(Vector3 a, Vector3 b)
        {
            A = a;
            B = b;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj is not Side otherSide)
                return false;

            return (otherSide.A == A && otherSide.B == B) ||
                (otherSide.B == A && otherSide.A == B);
        }
    }

    public struct Face
    {
        public readonly Vector3 A, B, C;
        public Vector3Int Triangle;
        public Vector3 Center => (A + B + C) * 0.333f;
        //public Vector3 Normal => new Vector3(A.y * B.z - A.z * B.y, A.z * B.x - A.x * B.z, A.x * B.y - A.y * B.x);
        public Vector3 Normal => Vector3.Cross(B - A, C - A).normalized;
        public float Area => Mathf.Abs(A.x * (B.y - C.y) + B.x * (C.y - A.y) + C.x * (A.y - B.y)) * 0.5f;

        public Face(Vector3 a, Vector3 b, Vector3 c)
        {
            A = a;
            B = b;
            C = c;
            Triangle = Vector3Int.zero;
        }

        public Face(Vector3 a, Vector3 b, Vector3 c, int t0, int t1, int t2)
        {
            A = a;
            B = b;
            C = c;

            Triangle = new Vector3Int(t0, t1, t2);
        }

        public void SetTriangle(int t0, int t1, int t2)
        {
            Triangle = new Vector3Int(t0, t1, t2);
        }
    }
}
