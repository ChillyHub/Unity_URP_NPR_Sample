using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Controller
{
    public class SunAndTimeController : MonoBehaviour
    {
        public Light sun;
        public List<Material> materials;

        public bool autoRotateSun = false;
        [UnityEngine.Range(0.0f, 1.0f)] public float autoRotateSpeed = 1.0f;

        private Vector3 _rightDir;
        private Color _sunColor;

        private static readonly int DayTimeId = Shader.PropertyToID("_DayTime");

        private void Start()
        {
            if (sun == null)
            {
                return;
            }
            
            Vector3 lightDir = -sun.transform.forward;
            _rightDir = Vector3.Cross(lightDir, Vector3.up);
            if (_rightDir.z < 0.0f)
            {
                _rightDir = -_rightDir;
            }

            _sunColor = sun.color;
        }

        private void Update()
        {
            if (sun == null)
            {
                Start();
                return;
            }

            Vector3 lightDir = -sun.transform.forward;
            Vector3 upDir = Vector3.up;
            
            Vector3 rightDir = Vector3.Cross(lightDir, upDir);
            float arcCos = Mathf.Acos(Vector3.Dot(lightDir, upDir));

            float time = 12.0f;
            float delta = arcCos * 12.0f / Mathf.PI;
            if (rightDir.z < 0.0f)
            {
                rightDir = -rightDir;
                time += delta;
            }
            else
            {
                time -= delta;
            }

            sun.color = _sunColor * Mathf.SmoothStep(1.0f, 0.0f, delta / 12.0f);

            foreach (var material in materials)
            {
                material.SetFloat(DayTimeId, time);
            }

            if (autoRotateSun)
            {
                float rotate = Time.deltaTime * autoRotateSpeed * 10.0f;
                sun.transform.RotateAround(transform.position, _rightDir, rotate);
            }

            _rightDir = rightDir;
        }

        private void OnDestroy()
        {
            foreach (var material in materials)
            {
                material.SetFloat(DayTimeId, 12.0f);
            }

            if (sun != null)
            {
                sun.color = _sunColor;
            }
        }
    }
}