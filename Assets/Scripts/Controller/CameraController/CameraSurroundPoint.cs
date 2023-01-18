using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Controller.CameraController
{
    public class CameraSurroundPoint : MonoBehaviour
    {
        [Header("Focus Point")]
        public Vector3 focusPosition = Vector3.zero;
        public GameObject focusTarget = null;
        [Tooltip("Whether to have the camera follow the movement if the focus is changed in the inspector")]
        public bool moveWithFoucs = false;

        [Space]
        [Header("Controller Key")]
        public MoveKeyCode moveKey = MoveKeyCode.MouseRight;
        public RotateKeyCode rotateKey = RotateKeyCode.MouseLeft;
        public ScaleKeyCode scaleKey = ScaleKeyCode.MouseScrollWhell;

        [Space]
        [Header("Transform Speed")]
        [Range(0.0f, 1.0f)] public float rotateSpeed = 0.5f;
        [Range(0.0f, 1.0f)] public float scaleSpeed = 0.5f;

        [Space]
        [Header("Follow Directional Light")]
        public Light followLight = null;
        public bool onlyRotateByY = false;

        #region Enum

        public enum MoveKeyCode
        {
            ShiftAndMouseMiddle,
            MouseRight
        }

        public enum RotateKeyCode
        {
            MouseMiddle,
            MouseLeft
        }

        public enum ScaleKeyCode
        {
            MouseScrollWhell
        }

        private enum CurrOption
        {
            Move,
            Rotate,
            Scale
        }

        #endregion

        private Camera m_Camera;
        private Vector3 m_MousePosition;
        private Vector3 m_OldFocusPosition;
        private CurrOption m_CurrOption;

        // Start is called before the first frame update
        void Start()
        {
            m_Camera = GetComponent<Camera>();
            m_MousePosition = Input.mousePosition;
            m_OldFocusPosition = focusPosition;
            m_CurrOption = CurrOption.Scale;

            if (focusTarget != null)
            {
                focusPosition = focusTarget.transform.position;
            }

            transform.LookAt(focusPosition);
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 newPosition = Input.mousePosition;
            EditorWindow window = EditorWindow.mouseOverWindow;
            float deltaX = newPosition.x - m_MousePosition.x;
            float deltaY = newPosition.y - m_MousePosition.y;
            m_MousePosition = newPosition;

            if (m_CurrOption == CurrOption.Move)
            {
                if (moveKey == MoveKeyCode.ShiftAndMouseMiddle)
                {
                    if (!Input.GetKey(KeyCode.Mouse2))
                    {
                        m_CurrOption = CurrOption.Scale;
                    }
                }
                else if (moveKey == MoveKeyCode.MouseRight)
                {
                    if (!Input.GetKey(KeyCode.Mouse1))
                    {
                        m_CurrOption = CurrOption.Scale;
                    }
                }
            }
            else if (m_CurrOption == CurrOption.Rotate)
            {
                if (rotateKey == RotateKeyCode.MouseMiddle)
                {
                    if (!Input.GetKey(KeyCode.Mouse2))
                    {
                        m_CurrOption = CurrOption.Scale;
                    }
                }
                else if (rotateKey == RotateKeyCode.MouseLeft)
                {
                    if (!Input.GetKey(KeyCode.Mouse0))
                    {
                        m_CurrOption = CurrOption.Scale;
                    }
                }
            }
            if (m_CurrOption == CurrOption.Scale)
            {
                if (rotateKey == RotateKeyCode.MouseMiddle)
                {
                    if (Input.GetKey(KeyCode.Mouse2))
                    {
                        m_CurrOption = CurrOption.Rotate;
                    }
                }
                else if (rotateKey == RotateKeyCode.MouseLeft)
                {
                    if (Input.GetKey(KeyCode.Mouse0))
                    {
                        m_CurrOption = CurrOption.Rotate;
                    }
                }
                if (moveKey == MoveKeyCode.ShiftAndMouseMiddle)
                {
                    if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        && Input.GetKey(KeyCode.Mouse2))
                    {
                        m_CurrOption = CurrOption.Move;
                    }
                }
                else if (moveKey == MoveKeyCode.MouseRight)
                {
                    if (Input.GetKey(KeyCode.Mouse1))
                    {
                        m_CurrOption = CurrOption.Move;
                    }
                }
            }

            switch (m_CurrOption)
            {
                case CurrOption.Move:
                    Move(deltaX, deltaY);
                    break;
                case CurrOption.Rotate:
                    Rotate(transform);
                    if (followLight != null)
                    {
                        Rotate(followLight.transform, onlyRotateByY);
                    }
                    break;
                case CurrOption.Scale:
                    if (window != null && window.titleContent.text == "Game")
                    {
                        Scale();
                    }
                    break;
                default:
                    break;
            }

            if (m_OldFocusPosition != focusPosition)
            {
                if (m_CurrOption != CurrOption.Move)
                {
                    if (moveWithFoucs)
                    {
                        transform.position += focusPosition - m_OldFocusPosition;
                    }
                    else
                    {
                        Vector3 up = m_Camera.transform.up.y >= 0.0f ? Vector3.up : -Vector3.up;
                        transform.LookAt(focusPosition, up);
                    }
                }
                m_OldFocusPosition = focusPosition;
            }
        }

        void OnValidate()
        {
            if (focusTarget != null)
            {
                focusPosition = focusTarget.transform.position;
            }
        }

        void Move(float deltaX, float deltaY)
        {
            Matrix4x4 P = m_Camera.projectionMatrix;
            Matrix4x4 IV = m_Camera.cameraToWorldMatrix;

            Vector4 deltaVS = Vector4.zero;
            float distance = (transform.position - focusPosition).magnitude;
            deltaVS.x = deltaX * distance * 2.0f / (P[0, 0] * m_Camera.scaledPixelWidth);
            deltaVS.y = deltaY * distance * 2.0f / (P[1, 1] * m_Camera.scaledPixelHeight);

            Vector3 deltaWS = IV * deltaVS;
            transform.position -= deltaWS;
            focusPosition -= deltaWS;
        }

        void Rotate(Transform trans, bool onlyRotateByAxisY = false)
        {
            float deltaX = Input.GetAxis("Mouse X");
            trans.RotateAround(focusPosition, Vector3.up, deltaX * rotateSpeed * 20.0f);

            if (!onlyRotateByAxisY)
            {
                float deltaY = Input.GetAxis("Mouse Y");
                trans.RotateAround(focusPosition, transform.right, -deltaY * rotateSpeed * 20.0f);
            }
        }

        void Scale()
        {
            float deltaScroll = Input.mouseScrollDelta.y;
            if (deltaScroll == 0.0f)
            {
                return;
            }
            deltaScroll *= scaleSpeed * 0.2f;

            Matrix4x4 IV = m_Camera.cameraToWorldMatrix;

            Vector4 deltaVS = Vector4.zero;
            float distance = (transform.position - focusPosition).magnitude;
            if (deltaScroll > 0)
            {
                deltaVS.z -= distance * deltaScroll / (deltaScroll + 1.0f);
            }
            else if (deltaScroll < 0)
            {
                deltaVS.z -= distance * deltaScroll;
            }

            Vector3 deltaWS = IV * deltaVS;
            transform.position += deltaWS;
        }
    }
}
