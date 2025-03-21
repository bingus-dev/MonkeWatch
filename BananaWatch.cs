﻿using BananaWatch.Utils;
using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using BananaWatch.Pages;
using UnityEngine.InputSystem;
using GorillaNetworking;
using Photon.Pun;

namespace BananaWatch
{
    [BepInPlugin("com.M4LivesAgain.BananaWatch", "BananaWatch", PluginInfo.Version)]
    public class BananaWatch : BaseUnityPlugin
    {
        private static List<Type> pageTypes = new List<Type>();
        public static BananaWatch Instance;
        private static AssetBundle assetBundle;
        private static GameObject bananaWatchPrefab;
        private GameObject InstantiatedBananaWatch;
        private Text screenText = GameObject.Find("Player Objects/Local VRRig/Local Gorilla Player/RigAnchor/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L/BananaWatch(Clone)/Plane/Canvas/Text")?.GetComponent<Text>();
        private static Vector3 originalScale;
        private bool isWatchOpen;

        private Vector3 positionOffset = new Vector3(0.639f, -0.64f, -0.017f); // as perfect as i can get it without src
        private Vector3 rotationOffset = new Vector3(90f, 180f, -90f);

        public List<BananaWatchPage> watchPages = new List<BananaWatchPage>();
        public BananaWatchPage _CurrentPage;

        public bool isInitialized = false;

        private bool KolossalIsNotACheatForGorillaTag = false;
        private bool HuskyGTDontOpenSourceYourCodeIfYouAreGoingToComplainWhenItIsUsed = false;

        void OnLoaded()
        {
            Instance = this;
            GameObject Plane = InstantiatedBananaWatch.transform.Find("Plane")?.gameObject;
            Plane.SetActive(false);
            Configuration.LoadSettings();

            pageTypes.Add(typeof(StartPage));
            pageTypes.Add(typeof(ErrorPage));
            pageTypes.Add(typeof(MainMenu));
            pageTypes.Add(typeof(ScoreboardPage));
            pageTypes.Add(typeof(DetailsPage));
            pageTypes.Add(typeof(PlayerDetailsPage));
            pageTypes.Add(typeof(Disconnect));
            pageTypes.Add(typeof(ReportPlayerPage));
            foreach (var page in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = page.GetTypes().Where(type => type.IsSubclassOf(typeof(BananaWatchPage)));
                    foreach (var type in types)
                    {
                        if (!pageTypes.Contains(type))
                        {
                            pageTypes.Add(type);
                        }
                    }
                }
                catch (Exception err)
                {
                    Debug.LogError($"{page.FullName} errored while registering: " + err);
                }
            }

            CreatePages();

            NavigateToPage(typeof(StartPage));
        }

        void Start()
        {
            StartCoroutine(InitializeBananaWatch());
        }

        void CreatePages()
        {
            foreach (var type in pageTypes)
            {
                try
                {
                    var watchPage = (BananaWatchPage)gameObject.AddComponent(type);
                    watchPages.Add(watchPage);
                }
                catch (Exception err)
                {
                    Debug.LogException(err);
                    continue;
                }
            }
        }

        private IEnumerator InitializeBananaWatch()
        {
            while (GorillaTagger.Instance == null)
            {
                yield return null;
            }

            try
            {
                assetBundle = AssetTools.LoadAssetBundle("BananaWatch.Resources.bananawatch");

                if (assetBundle != null)
                {
                    bananaWatchPrefab = assetBundle.LoadAsset<GameObject>("BananaWatch");

                    if (bananaWatchPrefab != null)
                    {
                        InstantiatedBananaWatch = Instantiate(bananaWatchPrefab);

                        originalScale = InstantiatedBananaWatch.transform.localScale;

                        InstantiatedBananaWatch.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                        InstantiatedBananaWatch.transform.localRotation = Quaternion.Euler(180, 0, 0);

                        InitializeScreenText();

                        GameObject buttonsParent = InstantiatedBananaWatch.transform.Find("Plane/Buttons")?.gameObject;
                        GameObject watchTop = InstantiatedBananaWatch.transform.Find("Watch/Top")?.gameObject;

                        watchTop.layer = 18;

                        if (buttonsParent != null && watchTop != null)
                        {
                            Transform[] childTransforms = buttonsParent.GetComponentsInChildren<Transform>(true);

                            watchTop.gameObject.AddComponent<WatchButton>();

                            foreach (Transform child in childTransforms)
                            {
                                if (child == buttonsParent.transform) continue;

                                WatchButton watchButton = child.gameObject.GetComponent<WatchButton>();
                                if (watchButton == null)
                                {
                                    watchButton = child.gameObject.AddComponent<WatchButton>();
                                    watchButton.gameObject.layer = 18;
                                }

                                switch (child.name)
                                {
                                    case "Up":
                                        watchButton.buttonType = BananaWatchButton.Up;
                                        break;
                                    case "Down":
                                        watchButton.buttonType = BananaWatchButton.Down;
                                        break;
                                    case "Left":
                                        watchButton.buttonType = BananaWatchButton.Left;
                                        break;
                                    case "Right":
                                        watchButton.buttonType = BananaWatchButton.Right;
                                        break;
                                    case "Back":
                                        watchButton.buttonType = BananaWatchButton.Back;
                                        break;
                                    case "Enter":
                                        watchButton.buttonType = BananaWatchButton.Enter;
                                        break;
                                    default:
                                        Debug.LogWarning($"button was registered that i dont understand: {child.name}");
                                        break;
                                }

                                watchButton.Interface();
                            }
                        }

                        isInitialized = true;
                        OnLoaded();


                        foreach (var page in watchPages)
                        {
                            try
                            {
                                page.PostModLoaded();
                            }
                            catch (Exception err)
                            {
                                Debug.LogError($"page error: {page.GetType().FullName}: {err}");
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Debug.LogError($"asset loading failed: {err.Message}");
                Debug.LogException(err);
            }
        }

        private void InitializeScreenText()
        {
            if (InstantiatedBananaWatch != null)
            {
                screenText = InstantiatedBananaWatch.transform.Find("Plane/Canvas/Text")?.GetComponent<Text>();
            }
        }

        public void OpenWatch()
        {
            GameObject Plane = GameObject.Find("Player Objects/Local VRRig/Local Gorilla Player/RigAnchor/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L/BananaWatch(Clone)/Plane");

            if (Plane != null)
                Plane.SetActive(true);
        }

        public void CloseWatch()
        {
            GameObject Plane = GameObject.Find("Player Objects/Local VRRig/Local Gorilla Player/RigAnchor/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L/BananaWatch(Clone)/Plane");

            if (Plane != null)
                Plane.SetActive(false);
        }

        public void UpdateScreen()
        {
            try
            {
                screenText.text = _CurrentPage.RenderScreenContent();
            }
            catch (Exception exception)
            {
                var e = $"{exception}";
                Debug.LogError(e);
                ErrorPage.page = e;
                NavigateToPage(typeof(ErrorPage));
            }
        }

        void Update()
        {
            if (isInitialized)
            {
                var watchAngle = Vector3.Angle(InstantiatedBananaWatch.transform.up, GorillaTagger.Instance.offlineVRRig.transform.up);
                if (watchAngle > Configuration.WatchClosingAngle.Value)
                {
                    isWatchOpen = false;
                    InstantiatedBananaWatch.transform.Find("Plane").gameObject.SetActive(false);
                    return;
                }
            }
        }

        public void LateUpdate()
        {
            if (isInitialized && InstantiatedBananaWatch != null)
            {
                try
                {
                    Transform parentTransform = GameObject.Find("Player Objects/Local VRRig/Local Gorilla Player/RigAnchor/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L")?.transform;
                    Transform huntComputerTransform = GameObject.Find("Player Objects/Local VRRig/Local Gorilla Player/RigAnchor/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L/huntcomputer (1)")?.transform;

                    if (parentTransform != null && huntComputerTransform != null)
                    {
                        if (InstantiatedBananaWatch.transform.parent != parentTransform)
                        {
                            InstantiatedBananaWatch.transform.SetParent(parentTransform);
                        }
                        InstantiatedBananaWatch.transform.localPosition = Vector3.Lerp(InstantiatedBananaWatch.transform.localPosition, huntComputerTransform.localPosition + positionOffset, Time.deltaTime * 10f);
                        InstantiatedBananaWatch.transform.localRotation = Quaternion.Slerp(InstantiatedBananaWatch.transform.localRotation, huntComputerTransform.localRotation * Quaternion.Euler(rotationOffset), Time.deltaTime * 10f);

                        GameObject Screen = GameObject.Find("Player Objects/Local VRRig/Local Gorilla Player/RigAnchor/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L/BananaWatch(Clone)/Plane");
                        GameObject head = GameObject.Find("Player Objects/Local VRRig/Local Gorilla Player/RigAnchor/rig/body/head");
                        Vector3 targetLookAt = head.transform.position;
                        Vector3 smoothLookAt = Vector3.Lerp(Screen.transform.position, targetLookAt, Time.deltaTime * 10f);
                        Screen.transform.LookAt(smoothLookAt);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"{e.Message}");
                    Debug.LogException(e);
                }
            }
            else
            {
                if (!isInitialized)
                {
                    return;
                }
            }
        }


        public void PressButton(BananaWatchButton type)
        {
            if (InstantiatedBananaWatch == null)
            {
                return;
            }

            GameObject plane = InstantiatedBananaWatch.transform.Find("Plane")?.gameObject;
            if (plane == null)
            {
                return;
            }

            if (type == BananaWatchButton.Watch)
            {
                isWatchOpen = !isWatchOpen;
                plane.SetActive(true);
                return;
            }

            if (_CurrentPage == null)
            {
                Debug.LogError("404 Page Not Found");
                return;
            }
            _CurrentPage.ButtonPressed(type);
            UpdateScreen();
        }


        public void NavigateToPage(BananaWatchPage screen)
        {
            if (screen == null)
            {
                return;
            }

            BananaWatch.Instance._CurrentPage = screen;
            screen.PageOpened();
            BananaWatch.Instance.UpdateScreen();
        }

        public void NavigateToPage(Type screenType)
        {
            if (watchPages == null)
            {
                return;
            }

            BananaWatchPage screen = watchPages.FirstOrDefault(page => page.GetType() == screenType);

            if (screen != null)
            {
                NavigateToPage(screen);
            }
            else
            {
                Debug.LogError($"{screenType}");
            }
        }
    }
}
