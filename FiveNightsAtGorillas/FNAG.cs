﻿using BepInEx;
using FiveNightsAtGorillas.Managers.Refrences;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Utilla;
using System.Collections;
using FiveNightsAtGorillas.Patches.Teleport;
using FiveNightsAtGorillas.Other.Door;
using FiveNightsAtGorillas.Managers.AI;
using FiveNightsAtGorillas.Managers.DoorAndLight;
using FiveNightsAtGorillas.Managers.NetworkedData;
using FiveNightsAtGorillas.Other.Light;
using FiveNightsAtGorillas.Other.Camera;
using FiveNightsAtGorillas.Managers.Cameras;
using FiveNightsAtGorillas.Managers.TimePower;
using FiveNightsAtGorillas.Other.PlayerDetecter;
using FiveNightsAtGorillas.Other.Ignore;
using FiveNightsAtGorillas.Other.NightStart;
using FiveNightsAtGorillas.Other.Scroll;
using FiveNightsAtGorillas.Other.CustomNightAdd;
using FiveNightsAtGorillas.Other.CustomNightSub;
using FiveNightsAtGorillas.Other.BOOP;

namespace FiveNightsAtGorillas
{
    [ModdedGamemode("fnag", "FNAG", Utilla.Models.BaseGamemode.Casual)]
    [BepInDependency("org.legoandmars.gorillatag.utilla")]
    [BepInDependency("com.ahauntedarmy.gorillatag.tmploader")]
    [BepInPlugin(FNAGInfo.GUID, FNAGInfo.Name, FNAGInfo.Version)]
    public class FNAG : BaseUnityPlugin
    {
        public int Version { get; private set; } = 100;

        public static FNAG Data;
        public bool RoundCurrentlyRunning;
        public bool LocalPlayingRound;
        public bool InCustomRoom { get; private set; }

        void Start() { Events.GameInitialized += OnGameInitialized; Data = this; }

        void OnEnable() { HarmonyPatches.ApplyHarmonyPatches(); }
        void OnDisable() { this.enabled = true; HarmonyPatches.RemoveHarmonyPatches(); } //Sorry! I can't take the time to figure out how I'm going to disable this properly
        void MakeFog() { RenderSettings.fog = true; RenderSettings.fogStartDistance = 7; RenderSettings.fogColor = Color.black; }
        void MakePoweroutFog() { RenderSettings.fog = true; RenderSettings.fogStartDistance = 0.3f; RenderSettings.fogColor = Color.black; }
        void RemoveFog() { RenderSettings.fog = false; }
        public void SkyColorFullBlack() { RenderSettings.ambientSkyColor = Color.black; MakeFog(); }
        public void SkyColorGameBlack() { RenderSettings.ambientSkyColor = RefrenceManager.Data.GameSkyColor.color; MakeFog(); }
        public void SkyColorWhite() { RenderSettings.ambientSkyColor = Color.white; RemoveFog(); }

        void OnGameInitialized(object sender, EventArgs e)
        {
            GameObject[] allObjs = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject obj in allObjs)
            {
                if(obj.name == "head_end")
                {
                    obj.AddComponent<BoxCollider>().isTrigger = true;
                    obj.layer = 10;
                    obj.GetComponent<BoxCollider>().center = new Vector3(0, 0, 0);
                }
            }

            var bundle = LoadAssetBundle("FiveNightsAtGorillas.Assets.fnag");
            var map = bundle.LoadAsset<GameObject>("FNAG MAP");
            var jumpscare = bundle.LoadAsset<GameObject>("Jumpscares");
            var menu = bundle.LoadAsset<GameObject>("Menu");

            GameObject.Find("BepInEx_Manager").AddComponent<RefrenceManager>();

            RefrenceManager.Data.Menu = Instantiate(menu);
            RefrenceManager.Data.FNAGMAP = Instantiate(map);
            RefrenceManager.Data.Jumpscares = Instantiate(jumpscare);

            RefrenceManager.Data.SetRefrences();

            SetupComps();
            SetupEnemys();
            SetupMenu();
            SetupHitSounds();
        }

        #region MenuSetup
        void SetupMenu()
        {
            RefrenceManager.Data.Menu.transform.position = new Vector3(-63.078f, 12.4836f, -82.3281f);
            RefrenceManager.Data.Menu.transform.localRotation = Quaternion.Euler(0, 90, 0);

            RefrenceManager.Data.NightOneSelect.SetActive(true);
            RefrenceManager.Data.NightTwoSelect.SetActive(false);
            RefrenceManager.Data.NightThreeSelect.SetActive(false);
            RefrenceManager.Data.NightFourSelect.SetActive(false);
            RefrenceManager.Data.NightFiveSelect.SetActive(false);
            RefrenceManager.Data.NightSixSelect.SetActive(false);
            RefrenceManager.Data.CustomNightSelect.SetActive(false);

            RefrenceManager.Data.MenuScrollLeft.SetActive(false);
            RefrenceManager.Data.MenuScrollRight.SetActive(false);
            RefrenceManager.Data.MenuWarning.SetActive(true);
            RefrenceManager.Data.MenuSelects.SetActive(false);
            RefrenceManager.Data.MenuRoundRunning.SetActive(false);

            RefrenceManager.Data.Menu.SetActive(false);
        }
        #endregion
        #region EnemySetup
        void SetupEnemys()
        {
            foreach (GameObject obj in RefrenceManager.Data.gorilla) { obj.SetActive(false); }
            foreach(GameObject obj in RefrenceManager.Data.mingus) { obj.SetActive(false); }
            foreach(GameObject obj in RefrenceManager.Data.dingus) { obj.SetActive(false); }
            foreach(GameObject obj in RefrenceManager.Data.bob) { obj.SetActive(false); }
            RefrenceManager.Data.gorilla[0].SetActive(true);
            RefrenceManager.Data.mingus[0].SetActive(true);
            RefrenceManager.Data.dingus[0].SetActive(true);
            RefrenceManager.Data.bob[0].SetActive(true);
            RefrenceManager.Data.SixAM.SetActive(false);
            RefrenceManager.Data.Jumpscare.SetActive(false);
            RefrenceManager.Data.FNAGMAP.transform.position = new Vector3(-102.0151f, 23.7944f, -65.1198f);
            RefrenceManager.Data.Jumpscares.transform.localRotation = Quaternion.Euler(0, 90, 0);
            RefrenceManager.Data.Jumpscare.transform.parent = GorillaTagger.Instance.mainCamera.transform;
            RefrenceManager.Data.SixAM.transform.parent = GorillaTagger.Instance.mainCamera.transform;
            RefrenceManager.Data.Jumpscare.transform.localPosition = new Vector3(0, -0.5f, 0.4f);
            RefrenceManager.Data.SixAM.transform.localPosition = new Vector3(0.2273f, 0, 0.1091f);
        }
        #endregion
        #region SetupHitsounds
        void SetupHitSounds()
        {
            GameObject.Find($"{RefrenceManager.Data.FNAGMAP.name}/Office/Floor/Floor").AddComponent<GorillaSurfaceOverride>().overrideIndex = 0;
            GameObject.Find("Office Walls").AddComponent<GorillaSurfaceOverride>().overrideIndex = 0;
            GameObject.Find($"{RefrenceManager.Data.FNAGMAP.name}/Office/Chair/Cylinder (1)").AddComponent<GorillaSurfaceOverride>().overrideIndex = 3;
            GameObject.Find($"{RefrenceManager.Data.FNAGMAP.name}/TheRest/Deco/Monitors").AddComponent<GorillaSurfaceOverride>().overrideIndex = 146;
            RefrenceManager.Data.CameraScreen.AddComponent<GorillaSurfaceOverride>().overrideIndex = 29;
            GameObject.Find($"{RefrenceManager.Data.FNAGMAP.name}/Office/Desk/Collider").AddComponent<GorillaSurfaceOverride>().overrideIndex = 146;
            RefrenceManager.Data.RightDoorVoid.AddComponent<GorillaSurfaceOverride>().overrideIndex = 0;
            RefrenceManager.Data.LeftDoorVoid.AddComponent<GorillaSurfaceOverride>().overrideIndex = 0;
        }
        #endregion
        #region SetupComps
        void SetupComps()
        {
            RefrenceManager.Data.LeftDoor.AddComponent<DoorButton>().isLeft = true;
            RefrenceManager.Data.RightDoor.AddComponent<DoorButton>().isLeft = false;
            RefrenceManager.Data.RightLight.AddComponent<LightButton>().isLeft = false;
            RefrenceManager.Data.LeftLight.AddComponent<LightButton>().isLeft = true;
            RefrenceManager.Data.ChainLoader.AddComponent<PhotonData>();
            RefrenceManager.Data.ChainLoader.AddComponent<DoorManager>();
            RefrenceManager.Data.ChainLoader.AddComponent<CameraManager>();
            RefrenceManager.Data.ChainLoader.AddComponent<TimePowerManager>();
            RefrenceManager.Data.gorillaParent.AddComponent<AIManager>().AIName = "gorilla";
            RefrenceManager.Data.mingusParent.AddComponent<AIManager>().AIName = "mingus";
            RefrenceManager.Data.bobParent.AddComponent<AIManager>().AIName = "bob";
            RefrenceManager.Data.dingusParent.AddComponent<AIManager>().AIName = "dingus";
            RefrenceManager.Data.gorillaParent.GetComponent<AIManager>().CamPos = "Cam11";
            RefrenceManager.Data.mingusParent.GetComponent<AIManager>().CamPos = "Cam11";
            RefrenceManager.Data.bobParent.GetComponent<AIManager>().CamPos = "Cam11";
            RefrenceManager.Data.dingusParent.GetComponent<AIManager>().CamPos = "Stage1";
            RefrenceManager.Data.NearGameTrigger.AddComponent<PlayersInRound>();
            RefrenceManager.Data.gorillaBoopTrigger.AddComponent<Boop_>();

            RefrenceManager.Data.Cam1.AddComponent<CameraButton>().CameraButtonTrigger = "Cam1";
            RefrenceManager.Data.Cam2.AddComponent<CameraButton>().CameraButtonTrigger = "Cam2";
            RefrenceManager.Data.Cam3.AddComponent<CameraButton>().CameraButtonTrigger = "Cam3";
            RefrenceManager.Data.Cam4.AddComponent<CameraButton>().CameraButtonTrigger = "Cam4";
            RefrenceManager.Data.Cam5.AddComponent<CameraButton>().CameraButtonTrigger = "Cam5";
            RefrenceManager.Data.Cam6.AddComponent<CameraButton>().CameraButtonTrigger = "Cam6";
            RefrenceManager.Data.Cam7.AddComponent<CameraButton>().CameraButtonTrigger = "Cam7";
            RefrenceManager.Data.Cam8.AddComponent<CameraButton>().CameraButtonTrigger = "Cam8";
            RefrenceManager.Data.Cam9.AddComponent<CameraButton>().CameraButtonTrigger = "Cam9";
            RefrenceManager.Data.Cam10.AddComponent<CameraButton>().CameraButtonTrigger = "Cam10";
            RefrenceManager.Data.Cam11.AddComponent<CameraButton>().CameraButtonTrigger = "Cam11";

            RefrenceManager.Data.MenuIgnoreButton.AddComponent<IgnoreWarning>();
            RefrenceManager.Data.MenuStartButton[0].AddComponent<StartNight>().Night = 1;
            RefrenceManager.Data.MenuStartButton[1].AddComponent<StartNight>().Night = 2;
            RefrenceManager.Data.MenuStartButton[2].AddComponent<StartNight>().Night = 3;
            RefrenceManager.Data.MenuStartButton[3].AddComponent<StartNight>().Night = 4;
            RefrenceManager.Data.MenuStartButton[4].AddComponent<StartNight>().Night = 5;
            RefrenceManager.Data.MenuStartButton[5].AddComponent<StartNight>().Night = 6;
            RefrenceManager.Data.MenuStartButton[6].AddComponent<StartNight>().Night = 7;
            RefrenceManager.Data.MenuScrollLeftButton.AddComponent<MenuScroll>().isRight = false;
            RefrenceManager.Data.MenuScrollRightButton.AddComponent<MenuScroll>().isRight = true;

            RefrenceManager.Data.MenuCNAddGorilla.AddComponent<CNAdd>().IsGorilla = true;
            RefrenceManager.Data.MenuCNAddMingus.AddComponent<CNAdd>().IsMingus = true;
            RefrenceManager.Data.MenuCNAddBob.AddComponent<CNAdd>().IsBob = true;
            RefrenceManager.Data.MenuCNAddDingus.AddComponent<CNAdd>().IsDingus = true;

            RefrenceManager.Data.MenuCNSubGorilla.AddComponent<CNSub>().IsGorilla = true;
            RefrenceManager.Data.MenuCNSubMingus.AddComponent<CNSub>().IsMingus = true;
            RefrenceManager.Data.MenuCNSubBob.AddComponent<CNSub>().IsBob = true;
            RefrenceManager.Data.MenuCNSubDingus.AddComponent<CNSub>().IsDingus = true;
        }
        #endregion

        AssetBundle LoadAssetBundle(string path)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            AssetBundle bundle = AssetBundle.LoadFromStream(stream);
            stream.Close();
            return bundle;
        }

        [ModdedGamemodeJoin]
        void OnJoin(string gamemode) 
        { 
            InCustomRoom = true; 
            RefrenceManager.Data.Menu.SetActive(true);
        }

        [ModdedGamemodeLeave]
        void OnLeave(string gamemode) 
        {
            InCustomRoom = false;
            StopGame();
            RefrenceManager.Data.Menu.SetActive(false);
            GameObject.Find("Selects").SetActive(false);
        }

        public void StopGame()
        {
            #region ResetAI
            foreach (GameObject ai in RefrenceManager.Data.gorilla) { ai.gameObject.SetActive(false); }
            foreach (GameObject ai in RefrenceManager.Data.mingus) { ai.gameObject.SetActive(false); }
            foreach (GameObject ai in RefrenceManager.Data.bob) { ai.gameObject.SetActive(false); }
            foreach (GameObject ai in RefrenceManager.Data.dingus) { ai.gameObject.SetActive(false); }
            RefrenceManager.Data.gorilla[0].SetActive(true);
            RefrenceManager.Data.mingus[0].SetActive(true);
            RefrenceManager.Data.bob[0].SetActive(true);
            RefrenceManager.Data.dingus[0].SetActive(true);
            AIManager[] AI = Resources.FindObjectsOfTypeAll<AIManager>();
            foreach(AIManager ai in AI) { ai.Difficulty = 0; ai.StopAI(); }
            #endregion
            #region ResetDoors
            DoorManager.Data.ResetDoors();
            #endregion
            #region ResetMenu
            RefrenceManager.Data.MenuWarning.SetActive(false);
            RefrenceManager.Data.MenuIgnoreButton.SetActive(false);
            RefrenceManager.Data.MenuSelects.SetActive(true);
            RefrenceManager.Data.MenuScrollLeft.SetActive(false);
            RefrenceManager.Data.MenuScrollRight.SetActive(true);
            RefrenceManager.Data.NightOneSelect.SetActive(true);
            RefrenceManager.Data.NightTwoSelect.SetActive(false);
            RefrenceManager.Data.NightThreeSelect.SetActive(false);
            RefrenceManager.Data.NightFourSelect.SetActive(false);
            RefrenceManager.Data.NightFiveSelect.SetActive(false);
            RefrenceManager.Data.NightSixSelect.SetActive(false);
            RefrenceManager.Data.CustomNightSelect.SetActive(false);
            RefrenceManager.Data.MenuRoundRunning.SetActive(false);
            #endregion
        }

        public void TeleportPlayerBack() 
        { 
            Vector3 Back = new Vector3(-66.3163f, 12.9148f, -82.4704f); TeleportPatch.TeleportPlayer(Back, 90, true);
        }

        public void TeleportPlayerToGame() 
        { 
            Vector3 Back = new Vector3(-103.6259f, 24.8166f, -66.3371f); TeleportPatch.TeleportPlayer(Back, 90, true);
        }

        public void Jumpscare()
        {
            RefrenceManager.Data.Jumpscare.SetActive(true);
            RefrenceManager.Data.JumpscareAnimation.Play("Jumpscare");
            RefrenceManager.Data.JumpscareSound.Play();
            StartCoroutine(JumpscareDelay());
            StopGame();
        }

        public void Poweroutage()
        {
            if (!DoorManager.Data.RightDoorOpen)
            {
                DoorManager.Data.UseLocalDoor(true);
            }
            if (!DoorManager.Data.LeftDoorOpen)
            {
                DoorManager.Data.UseLocalDoor(false);
            }
            if (DoorManager.Data.LeftLightOn)
            {
                DoorManager.Data.UseLight(false);
            }
            if (DoorManager.Data.RightLightOn)
            {
                DoorManager.Data.UseLight(true);
            }
            StopGame();
            DoorManager.Data.PowerOutage();
            RefrenceManager.Data.Poweroutage.Play();
            TimePowerManager.Data.StopOnlyPower();
            SkyColorFullBlack();
            StartCoroutine(PoweroutageDelay());
        }

        public void SixAM()
        {
            StopGame();
            RefrenceManager.Data.SixAM.SetActive(true);
            RefrenceManager.Data.SixAMSound.Play();
            RefrenceManager.Data.Poweroutage.Stop();
            StartCoroutine(SixAMDelay());
        }

        public void DingusRun()
        {
            RefrenceManager.Data.DingusRunning.Play();
            foreach (GameObject D in RefrenceManager.Data.dingus)
            {
                D.SetActive(false);
            }
            StartCoroutine(DingusRunDelay());
        }

        IEnumerator SixAMDelay()
        {
            yield return new WaitForSeconds(10);
            RefrenceManager.Data.SixAM.SetActive(false);
            TeleportPlayerBack();
            SkyColorWhite();
            TimePowerManager.Data.StopEverything();
        }

        IEnumerator PoweroutageDelay()
        {
            yield return new WaitForSeconds(67.8f);
            Jumpscare();
        }

        IEnumerator DingusRunDelay()
        {
            yield return new WaitForSeconds(2);
            if (DoorManager.Data.LeftDoorOpen)
            {
                Jumpscare();
            }
            else
            {
                RefrenceManager.Data.dingusParent.GetComponent<AIManager>().ResetDingus();
                RefrenceManager.Data.DingusScrapingSound.Play();
            }
        }

        IEnumerator JumpscareDelay()
        {
            yield return new WaitForSeconds(3);
            TeleportPlayerBack();
            TimePowerManager.Data.StopEverything();
            SkyColorWhite();
            RefrenceManager.Data.Jumpscare.SetActive(false);
            RefrenceManager.Data.JumpscareAnimation.StopPlayback();
            RefrenceManager.Data.JumpscareSound.Stop();
            SkyColorWhite();
        }
    }
}