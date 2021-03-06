﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Explorer
{
    public class ScenePage : WindowPage
    {
        public static ScenePage Instance;

        public override string Name { get => "Scene Explorer"; set => base.Name = value; }

        private int m_pageOffset = 0;
        private int m_limit = 20;
        private int m_currentTotalCount = 0;

        private float m_timeOfLastUpdate = -1f;

    // ----- Holders for GUI elements ----- //

    private string m_currentScene = "";

        // gameobject list
        private Transform m_currentTransform;
        private List<GameObjectCache> m_objectList = new List<GameObjectCache>();

        // search bar
        private bool m_searching = false;
        private string m_searchInput = "";
        private List<GameObjectCache> m_searchResults = new List<GameObjectCache>();

        // ------------ Init and Update ------------ //

        public override void Init()
        {
            Instance = this;
        }

        public void OnSceneChange()
        {
            m_currentScene = UnityHelpers.ActiveSceneName;
            SetTransformTarget(null);
        }

        public void CheckOffset(ref int offset, int childCount)
        {
            if (offset >= childCount)
            {
                offset = 0;
                m_pageOffset = 0;
            }
        }

        public override void Update()
        {
            if (m_searching) return;

            if (Time.time - m_timeOfLastUpdate < 1f) return;
            m_timeOfLastUpdate = Time.time;

            m_objectList = new List<GameObjectCache>();
            int offset = m_pageOffset * m_limit;

            var allTransforms = new List<Transform>();

            // get current list of all transforms (either scene root or our current transform children)
            if (m_currentTransform)
            {
                for (int i = 0; i < m_currentTransform.childCount; i++)
                {
                    allTransforms.Add(m_currentTransform.GetChild(i));
                }
            }
            else
            {
                var scene = SceneManager.GetSceneByName(m_currentScene);
                var rootObjects = scene.GetRootGameObjects();

                foreach (var obj in rootObjects)
                {
                    allTransforms.Add(obj.transform);
                }
            }

            m_currentTotalCount = allTransforms.Count;

            // make sure offset doesn't exceed count
            CheckOffset(ref offset, m_currentTotalCount);

            // sort by childcount
            allTransforms.Sort((a, b) => b.childCount.CompareTo(a.childCount));

            for (int i = offset; i < offset + m_limit && i < m_currentTotalCount; i++)
            {
                var child = allTransforms[i];
                m_objectList.Add(new GameObjectCache(child.gameObject));
            }
        }

        public void SetTransformTarget(Transform t)
        {
            m_currentTransform = t;

            if (m_searching)
                CancelSearch();

            m_timeOfLastUpdate = -1f;
            Update();
        }

        public void TraverseUp()
        {
            if (m_currentTransform.parent != null)
            {
                SetTransformTarget(m_currentTransform.parent);
            }
            else
            {
                SetTransformTarget(null);
            }
        }

        public void Search()
        {
            m_searchResults = SearchSceneObjects(m_searchInput);
            m_searching = true;
            m_currentTotalCount = m_searchResults.Count;
        }

        public void CancelSearch()
        {
            m_searching = false;
        }

        public List<GameObjectCache> SearchSceneObjects(string _search)
        {
            var matches = new List<GameObjectCache>();

            foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.name.ToLower().Contains(_search.ToLower()) && obj.scene.name == m_currentScene)
                {
                    matches.Add(new GameObjectCache(obj));
                }
            }

            return matches;
        }

        // --------- GUI Draw Function --------- //        

        public override void DrawWindow()
        {
            try
            {
                DrawHeaderArea();

                GUILayout.BeginVertical(GUI.skin.box, null);

                DrawPageButtons();

                if (!m_searching)
                {
                    DrawGameObjectList();
                }
                else
                {
                    DrawSearchResultsList();
                }

                GUILayout.EndVertical();
            }
            catch (Exception e)
            {
                MelonLogger.Log("Exception drawing ScenePage! " + e.GetType() + ", " + e.Message);
                MelonLogger.Log(e.StackTrace);
                m_currentTransform = null;
            }
        }

        private void DrawHeaderArea()
        {
            GUILayout.BeginHorizontal(null);

            // Current Scene label
            GUILayout.Label("Current Scene:", new GUILayoutOption[] { GUILayout.Width(120) });
            try
            {
                // Need to do 'ToList()' so the object isn't cleaned up by Il2Cpp GC.
                var scenes = SceneManager.GetAllScenes().ToList();

                if (scenes.Count > 1)
                {
                    int changeWanted = 0;
                    if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.Width(30) }))
                    {
                        changeWanted = -1;
                    }
                    if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.Width(30) }))
                    {
                        changeWanted = 1;
                    }
                    if (changeWanted != 0)
                    {
                        int index = scenes.IndexOf(SceneManager.GetSceneByName(m_currentScene));
                        index += changeWanted;
                        if (index > scenes.Count - 1)
                        {
                            index = 0;
                        }
                        else if (index < 0)
                        {
                            index = scenes.Count - 1;
                        }
                        m_currentScene = scenes[index].name;
                    }
                }
            }
            catch { }
            GUILayout.Label("<color=cyan>" + m_currentScene + "</color>", null); //new GUILayoutOption[] { GUILayout.Width(250) });

            GUILayout.EndHorizontal();

            // ----- GameObject Search -----
            GUILayout.BeginHorizontal(GUI.skin.box, null);
            GUILayout.Label("<b>Search Scene:</b>", new GUILayoutOption[] { GUILayout.Width(100) });
            m_searchInput = GUILayout.TextField(m_searchInput, null);
            if (GUILayout.Button("Search", new GUILayoutOption[] { GUILayout.Width(80) }))
            {
                Search();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }

        private void DrawPageButtons()
        {
            GUILayout.BeginHorizontal(null);

            GUILayout.Label("Limit per page: ", new GUILayoutOption[] { GUILayout.Width(100) });
            var limit = m_limit.ToString();
            limit = GUILayout.TextField(limit, new GUILayoutOption[] { GUILayout.Width(30) });
            if (int.TryParse(limit, out int lim))
            {
                m_limit = lim;
            }

            // prev/next page buttons
            if (m_currentTotalCount > m_limit)
            {
                int count = m_currentTotalCount;
                int maxOffset = (int)Mathf.Ceil((float)(count / (decimal)m_limit)) - 1;
                if (GUILayout.Button("< Prev", null))
                {
                    if (m_pageOffset > 0) m_pageOffset--;
                    m_timeOfLastUpdate = -1f;
                    Update();
                }

                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label($"Page {m_pageOffset + 1}/{maxOffset + 1}", new GUILayoutOption[] { GUILayout.Width(80) });

                if (GUILayout.Button("Next >", null))
                {
                    if (m_pageOffset < maxOffset) m_pageOffset++;
                    m_timeOfLastUpdate = -1f;
                    Update();
                }
            }

            GUILayout.EndHorizontal();
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        private void DrawGameObjectList()
        {
            if (m_currentTransform != null)
            {
                GUILayout.BeginHorizontal(null);
                if (GUILayout.Button("<-", new GUILayoutOption[] { GUILayout.Width(35) }))
                {
                    TraverseUp();
                }
                else
                {
                    GUILayout.Label("<color=cyan>" + m_currentTransform.GetGameObjectPath() + "</color>",
                        new GUILayoutOption[] { GUILayout.Width(MainMenu.MainRect.width - 187f) });
                }

                UIHelpers.SmallInspectButton(m_currentTransform);
                
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Scene Root GameObjects:", null);
            }

            if (m_objectList.Count > 0)
            {
                foreach (var obj in m_objectList)
                {
                    if (!obj.RefGameObject)
                    {
                        string label = "<color=red><i>null";

                        if (obj.RefGameObject != null)
                        {
                            label += " (Destroyed)";
                        }

                        label += "</i></color>";
                        GUILayout.Label(label, null);
                    }
                    else
                    {
                        UIHelpers.FastGameobjButton(obj.RefGameObject,
                        obj.EnabledColor,
                        obj.Label,
                        obj.RefGameObject.activeSelf,
                        SetTransformTarget,
                        true,
                        MainMenu.MainRect.width - 170);
                    }
                }
            }
        }

        private void DrawSearchResultsList()
        {
            if (GUILayout.Button("<- Cancel Search", new GUILayoutOption[] { GUILayout.Width(150) }))
            {
                CancelSearch();
            }

            GUILayout.Label("Search Results:", null);

            if (m_searchResults.Count > 0)
            {
                int offset = m_pageOffset * m_limit;

                if (offset >= m_searchResults.Count)
                {
                    offset = 0;
                    m_pageOffset = 0;
                }

                for (int i = offset; i < offset + m_limit && offset < m_searchResults.Count; i++)
                {
                    var obj = m_searchResults[i];

                    UIHelpers.FastGameobjButton(obj.RefGameObject, obj.EnabledColor, obj.Label, obj.RefGameObject.activeSelf, SetTransformTarget, true, MainMenu.MainRect.width - 170);
                }
            }
            else
            {
                GUILayout.Label("<color=red><i>No results found!</i></color>", null);
            }
        }

        // -------- Mini GameObjectCache class ---------- //
    
        public class GameObjectCache
        {
            public GameObject RefGameObject;
            public string Label;
            public Color EnabledColor;
            public int ChildCount;

            public GameObjectCache(GameObject obj)
            {
                RefGameObject = obj;
                ChildCount = obj.transform.childCount;

                Label = (ChildCount > 0) ? "[" + obj.transform.childCount + " children] " : "";
                Label += obj.name;

                bool enabled = obj.activeSelf;
                int childCount = obj.transform.childCount;
                if (enabled)
                {
                    if (childCount > 0)
                    {
                        EnabledColor = Color.green;
                    }
                    else
                    {
                        EnabledColor = UIStyles.LightGreen;
                    }
                }
                else
                {
                    EnabledColor = Color.red;
                }
            }
        }
    }
}
