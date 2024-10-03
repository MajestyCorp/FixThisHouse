using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FixThisHouse
{
    /// <summary>
    /// This script is used to copy front windows to other sides of a block.
    /// Used for asset: https://www.kenney.nl/assets/modular-buildings
    /// </summary>
    public class CopyFrontWallToOthers : MonoBehaviour
    {
        [SerializeField]
        private Mesh mesh;

        [SerializeField]
        private MeshFilter meshFilter;

        [SerializeField]
        private MeshRenderer meshRenderer;

        [SerializeField]
        private bool applyExtend;

        [SerializeField]
        private bool copyToRight;

        [SerializeField]
        private bool copyToBack;

        [SerializeField]
        private bool copyToLeft;

        private List<Vector3> _vertices = new();
        private List<Vector3> _normals = new();
        private List<int> _triangles = new();
        private List<Vector2> _uvs = new();
        private List<Vector2> _uvs4 = new();

        private const string folder = "Assets/3rd-party/Kenney/Modular Buildings/Meshes/";

#if UNITY_EDITOR
        [ContextMenu("Save Mesh")]
        private void SaveMesh()
        {
            var mesh = meshFilter.mesh;

            SaveMesh(mesh, this.mesh.name + GetCode(), true, true);
        }

        private string GetCode()
        {
            var result = "_";
            result += copyToLeft ? "L" : "";
            result += copyToBack ? "B" : "";
            result += copyToRight ? "R" : "";

            return result;
        }

        private void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
        {
            string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", folder, name, "asset");
            if (string.IsNullOrEmpty(path)) return;

            path = FileUtil.GetProjectRelativePath(path);

            Mesh meshToSave = (makeNewInstance) ? UnityEngine.Object.Instantiate(mesh) as Mesh : mesh;

            if (optimizeMesh)
                MeshUtility.Optimize(meshToSave);

            AssetDatabase.CreateAsset(meshToSave, path);
            AssetDatabase.SaveAssets();
        }

        [ContextMenu("Execute")]
        private void Execute()
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector2[] uvs = mesh.uv;
            Vector2[] uvs4 = mesh.uv4;

            var bounds = mesh.bounds;

            _vertices.Clear();
            _normals.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _uvs4.Clear();

            for (int i = 0, tris = triangles.Length; i < tris; i += 3)
            {
                var t0 = triangles[i];
                var t1 = triangles[i + 1];
                var t2 = triangles[i + 2];

                var v0 = vertices[t0];
                var v1 = vertices[t1];
                var v2 = vertices[t2];

                var uv0 = uvs[t0];
                var uv1 = uvs[t1];
                var uv2 = uvs[t2];

                var uv40 = uvs4[t0];
                var uv41 = uvs4[t1];
                var uv42 = uvs4[t2];

                var isFace = IsFace(bounds, v0, v1, v2);

                if (IsTop(bounds, v0, v1, v2) || IsBottom(bounds, v0, v1, v2) ||
                    (IsBack(bounds, v0, v1, v2) && !copyToBack) ||
                    (IsRight(bounds, v0, v1, v2) && !copyToRight) ||
                    (IsLeft(bounds, v0, v1, v2) && !copyToLeft) ||
                    isFace)
                    AddTris(v0, v1, v2, uv0, uv1, uv2, uv40, uv41, uv42);

                if (isFace && copyToLeft)
                    AddLeftTris(bounds, v0, v1, v2, uv0, uv1, uv2, uv40, uv41, uv42);

                if (isFace && copyToRight)
                    AddRightTris(bounds, v0, v1, v2, uv0, uv1, uv2, uv40, uv41, uv42);

                if (isFace && copyToBack)
                    AddBackTris(bounds, v0, v1, v2, uv0, uv1, uv2, uv40, uv41, uv42);
            }

            RebuildMesh();
        }

        private bool IsTop(Bounds bounds, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            var value = bounds.size.y;
            return v0.y == value && v1.y == value && v2.y == value;
        }

        private bool IsBottom(Bounds bounds, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            return v0.y <= 0 && v1.y <= 0 && v2.y <= 0;
        }

        private bool IsBack(Bounds bounds, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            var value = bounds.center.z;
            return v0.z < value && v1.z < value && v2.z < value;
        }

        private bool IsRight(Bounds bounds, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            var value = bounds.extents.x;
            return v0.x == value && v1.x == value && v2.x == value;
        }

        private bool IsLeft(Bounds bounds, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            var value = -bounds.extents.x;
            return v0.x == value && v1.x == value && v2.x == value;
        }

        private bool IsFace(Bounds bounds, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            var value = 0;
            return v0.z > value && v1.z > value && v2.z > value;
        }

        private void RebuildMesh()
        {
            var mesh = meshFilter.mesh;
            var vertices = _vertices.ToArray();
            var uvs = _uvs.ToArray();
            var uvs4 = _uvs4.ToArray();
            var triangles = _triangles.ToArray();

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(4, uvs4);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(_normals);
            mesh.RecalculateBounds();
        }

        private void AddLeftTris(Bounds bounds, Vector3 v0, Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv40, Vector2 uv41, Vector2 uv42)
        {
            var e = bounds.extents;
            var d = e.z - e.x;
            d = applyExtend ? d : 0f;
            var newV0 = new Vector3(-v0.z - d, v0.y, -v0.x - d);
            var newV1 = new Vector3(-v1.z - d, v1.y, -v1.x - d);
            var newV2 = new Vector3(-v2.z - d, v2.y, -v2.x - d);

            AddTris(newV2, newV1, newV0, uv2, uv1, uv0, uv42, uv41, uv40);
        }

        private void AddRightTris(Bounds bounds, Vector3 v0, Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv40, Vector2 uv41, Vector2 uv42)
        {
            var e = bounds.extents;
            var d = e.z - e.x;
            d = applyExtend ? d : 0f;
            var newV0 = new Vector3(v0.z + d, v0.y, v0.x - d);
            var newV1 = new Vector3(v1.z + d, v1.y, v1.x - d);
            var newV2 = new Vector3(v2.z + d, v2.y, v2.x - d);

            AddTris(newV2, newV1, newV0, uv2, uv1, uv0, uv42, uv41, uv40);
        }

        private void AddBackTris(Bounds bounds, Vector3 v0, Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv40, Vector2 uv41, Vector2 uv42)
        {
            var e = bounds.size;
            var d = e.z - e.x;
            d = applyExtend ? d : 0f;
            var newV0 = new Vector3(-v0.x, v0.y, -v0.z - d);
            var newV1 = new Vector3(-v1.x, v1.y, -v1.z - d);
            var newV2 = new Vector3(-v2.x, v2.y, -v2.z - d);

            AddTris(newV0, newV1, newV2, uv0, uv1, uv2, uv40, uv41, uv42);
        }

        private void AddTris(Vector3 v0, Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv40, Vector2 uv41, Vector2 uv42)
        {
            var t0 = -1;
            var t1 = -1;
            var t2 = -1;

            var calcN = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            for (var i = 0; i < _vertices.Count; i++)
            {
                var v = _vertices[i];
                var uv = _uvs[i];
                var uv4 = _uvs4[i];
                var n = _normals[i];

                if (t0 < 0 && v == v0 && uv == uv0 && uv4 == uv40 && n == calcN)
                    t0 = i;

                if (t1 < 0 && v == v1 && uv == uv1 && uv4 == uv41 && n == calcN)
                    t1 = i;

                if (t2 < 0 && v == v2 && uv == uv2 && uv4 == uv42 && n == calcN)
                    t2 = i;
            }

            if (t0 < 0)
            {
                t0 = _vertices.Count;
                _vertices.Add(v0);
                _uvs.Add(uv0);
                _uvs4.Add(uv40);
                _normals.Add(calcN);
            }

            if (t1 < 0)
            {
                t1 = _vertices.Count;
                _vertices.Add(v1);
                _uvs.Add(uv1);
                _uvs4.Add(uv41);
                _normals.Add(calcN);
            }

            if (t2 < 0)
            {
                t2 = _vertices.Count;
                _vertices.Add(v2);
                _uvs.Add(uv2);
                _uvs4.Add(uv42);
                _normals.Add(calcN);
            }

            _triangles.Add(t0);
            _triangles.Add(t1);
            _triangles.Add(t2);
        }

#endif
    }
}
