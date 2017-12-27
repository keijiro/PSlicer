using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;

namespace PSlicer
{
    [ExecuteInEditMode]
    [AddComponentMenu("Effects/PSlicer/Effector")]
    class Effector : MonoBehaviour, ITimeControl, IPropertyPreview
    {
        #region Editable attributes

        [SerializeField] float _range = 1;
        [SerializeField] float _offset = 0;

        [Space]
        [SerializeField] float _density = 20;
        [SerializeField] float _speed = 5;

        [SerializeField, ColorUsage(false, true, 0, 8, 0.125f, 3)]
        Color _color;

        [Space]
        [SerializeField] Renderer[] _linkedRenderers;

        #endregion

        #region Utility properties for internal use

        float LocalTime {
            get {
                if (_controlTime < 0)
                    return Application.isPlaying ? Time.time : 0;
                else
                    return _controlTime;
            }
        }

        #endregion

        #region ITimeControl implementation

        float _controlTime = -1;

        public void OnControlTimeStart()
        {
        }

        public void OnControlTimeStop()
        {
            _controlTime = -1;
        }

        public void SetTime(double time)
        {
            _controlTime = (float)time;
        }

        #endregion

        #region IPropertyPreview implementation

        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
        }

        #endregion

        #region MonoBehaviour implementation

        MaterialPropertyBlock _sheet;

        void LateUpdate()
        {
            if (_linkedRenderers == null || _linkedRenderers.Length == 0) return;

            if (_sheet == null) _sheet = new MaterialPropertyBlock();

            var mtx = transform.worldToLocalMatrix;
            var time = LocalTime + 10;

            foreach (var renderer in _linkedRenderers)
            {
                renderer.GetPropertyBlock(_sheet);

                _sheet.SetFloat("_Density", _density);
                _sheet.SetFloat("_Speed", _speed);

                _sheet.SetFloat("_EffectorRange", _range);
                _sheet.SetFloat("_EffectorOffset", _offset);
                _sheet.SetMatrix("_EffectorMatrix", mtx);
                _sheet.SetColor("_EffectorColor", _color);

                _sheet.SetFloat("_LocalTime", time);

                renderer.SetPropertyBlock(_sheet);
            }
        }

        #endregion

        #region Editor gizmo implementation

        #if UNITY_EDITOR

        Mesh _gridMesh;

        void OnDestroy()
        {
            if (_gridMesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_gridMesh);
                else
                    DestroyImmediate(_gridMesh);
            }
        }

        void OnDrawGizmos()
        {
            if (_gridMesh == null) InitGridMesh();

            Gizmos.matrix = transform.localToWorldMatrix;

            var p1 = Vector3.forward * _offset;
            var p2 = Vector3.forward * (_offset + _range);

            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawWireMesh(_gridMesh, p1);
            Gizmos.DrawWireMesh(_gridMesh, p2);

            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawWireCube((p1 + p2) / 2, new Vector3(0.02f, 0.02f, _range));
        }

        void InitGridMesh()
        {
            const float ext = 0.5f;
            const int columns = 10;

            var vertices = new List<Vector3>();
            var indices = new List<int>();

            for (var i = 0; i < columns + 1; i++)
            {
                var x = ext * (2.0f * i / columns - 1);

                indices.Add(vertices.Count);
                vertices.Add(new Vector3(x, -ext, 0));

                indices.Add(vertices.Count);
                vertices.Add(new Vector3(x, +ext, 0));

                indices.Add(vertices.Count);
                vertices.Add(new Vector3(-ext, x, 0));

                indices.Add(vertices.Count);
                vertices.Add(new Vector3(+ext, x, 0));
            }

            _gridMesh = new Mesh();
            _gridMesh.hideFlags = HideFlags.DontSave;
            _gridMesh.SetVertices(vertices);
            _gridMesh.SetNormals(vertices);
            _gridMesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
            _gridMesh.UploadMeshData(true);
        }

        #endif

        #endregion
    }
}
