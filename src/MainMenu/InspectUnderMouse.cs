﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public class InspectUnderMouse
    {
        public static bool EnableInspect { get; set; } = false;

        private static string m_objUnderMouseName = "";        

        public static void Update()
        {
            if (CppExplorer.ShowMenu)
            {
                if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(1))
                {
                    EnableInspect = !EnableInspect;
                }

                if (EnableInspect)
                {
                    InspectRaycast();
                }
            }
            else if (EnableInspect)
            {
                EnableInspect = false;
            }
        }

        public static void InspectRaycast()
        {
            Ray ray = UnityHelpers.MainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var obj = hit.transform.gameObject;

                m_objUnderMouseName = obj.transform.GetGameObjectPath();

                if (Input.GetMouseButtonDown(0))
                {
                    EnableInspect = false;
                    m_objUnderMouseName = "";

                    WindowManager.InspectObject(obj, out _);
                }
            }
            else
            {
                m_objUnderMouseName = "";
            }
        }

        public static void OnGUI()
        {
            if (EnableInspect)
            {
                if (m_objUnderMouseName != "")
                {
                    var pos = Input.mousePosition;
                    var rect = new Rect(
                        pos.x - (Screen.width / 2), // x
                        Screen.height - pos.y - 50, // y
                        Screen.width,               // w
                        50                          // h
                    );

                    var origAlign = GUI.skin.label.alignment;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;

                    //shadow text
                    GUI.Label(rect, $"<color=black>{m_objUnderMouseName}</color>");
                    //white text
                    GUI.Label(new Rect(rect.x - 1, rect.y + 1, rect.width, rect.height), m_objUnderMouseName);

                    GUI.skin.label.alignment = origAlign;
                }
            }
        }
    }
}
