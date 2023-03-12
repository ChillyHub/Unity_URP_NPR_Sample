using System;
using UnityEngine;

namespace Controller
{
    public class CameraFreelook : MonoBehaviour
    {

        public Transform target;


        [Header("鼠标左键 旋转")]
        public float RotateSpeed = 2f;
        public float rotationMin = 0f;
        public float rotationMax = 60f;

        [Header("鼠标中键 缩放")]
        public float ZoomSpeed = 2;
        public float distanceMin = 3f;
        public float distanceMax = 50f;

        [Header("鼠标右键 移动")]
        public float translateSpeed = 3;

        [Header("围绕目标自动旋转")]
        public bool autoRotate = false;
        public float autoRotateSpeed = 2;

        // private bool mousemove = false;//检测鼠标是否移动

        [Header("鼠标超过多少秒静止，自动旋转(最少5秒)")]
        public bool MouseStopCheck = false;
        public int MouseStopSecond = 5;
        private DateTime timeOld;

        void Start()
        {
            timeOld = DateTime.Now;
            if(MouseStopCheck == true)
                InvokeRepeating("CheckMouseStopSecond", 2.0f, 1.0f);

        }

        void CheckMouseStopSecond()//检测鼠标静止时长
        {
            TimeSpan ts = DateTime.Now - timeOld;
            if(ts.Seconds> MouseStopSecond && autoRotate==false)
            {
                autoRotate = true;
            }
            else if(ts.Seconds < MouseStopSecond && autoRotate==true)
            {
                autoRotate = false;
            }
        }

        void Update()
        {
            //if (EventSystem.current.IsPointerOverGameObject())
            //{
            //    return;
            //}

            if (autoRotate)
                transform.RotateAround(target.position, Vector3.up, autoRotateSpeed * Time.deltaTime); //摄像机围绕目标旋转

            var mouse_x = Input.GetAxis("Mouse X");//获取鼠标X轴移动
            var mouse_y = -Input.GetAxis("Mouse Y");//获取鼠标Y轴移动

            //鼠标右键移动
            if (Input.GetKey(KeyCode.Mouse1))
            {
                transform.Translate(Vector3.left * (mouse_x * translateSpeed) * Time.deltaTime);
                transform.Translate(Vector3.up * (mouse_y * translateSpeed) * Time.deltaTime);
                timeOld = DateTime.Now;
            }

            //鼠标左键旋转
            if (Input.GetKey(KeyCode.Mouse0))
            {
                timeOld = DateTime.Now;

                transform.RotateAround(target.transform.position, Vector3.up, mouse_x * RotateSpeed);

                //预设角度（当前角度加上将要增加/减少的角度）
                float rotatedAngle = transform.eulerAngles.x + mouse_y * RotateSpeed;

                if (rotatedAngle >= rotationMin && rotatedAngle <= rotationMax)
                    transform.RotateAround(target.transform.position, transform.right, mouse_y * RotateSpeed);
            }

            //鼠标中键缩放
            zoom();

        }

        private void zoom() //摄像机滚轮缩放
        {
            var distance = Vector3.Distance(Vector3.zero, transform.position);
            if (Input.GetAxis("Mouse ScrollWheel") > 0 && distance > distanceMin)
            {
                transform.Translate(Vector3.forward * ZoomSpeed);
                timeOld = DateTime.Now;
            }

            if (Input.GetAxis("Mouse ScrollWheel") < 0 && distance < distanceMax)
            {
                transform.Translate(Vector3.forward * -ZoomSpeed);
                timeOld = DateTime.Now;
            }
        }


    }
}