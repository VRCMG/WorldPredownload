﻿using System;
using UnityEngine;
using VRC.Core;
using VRC.UI;
using MelonLoader;
using WorldPredownload.Cache;
using WorldPredownload.DownloadManager;

namespace WorldPredownload.UI
{
    public class WorldButton
    {
        public static bool canChangeText { get; set; } = true;
        public static bool initialised { get; set; } = false;
        public static GameObject button { get; set; }
        private const string PATH_TO_GAMEOBJECT_TO_CLONE = "UserInterface/MenuContent/Screens/WorldInfo/WorldButtons/GoButton";
        private const string PATH_TO_CLONE_PARENT = "UserInterface/MenuContent/Screens/WorldInfo/WorldButtons";
        private const string PATH_TO_WORLDINFO = "UserInterface/MenuContent/Screens/WorldInfo";

        public static void Setup()
        {
            button = Utilities.CloneGameObject(PATH_TO_GAMEOBJECT_TO_CLONE, PATH_TO_CLONE_PARENT);
            button.GetRectTrans().SetAnchoredPos(Constants.WORLD_BUTTON_POS);
            button.SetName(Constants.WORLD_BUTTON_NAME);
            button.SetText(Constants.BUTTON_IDLE_TEXT);
            button.SetButtonAction(onClick);
            button.SetActive(true);
            initialised = true;
        }

        public static void UpdateText(ApiWorld world)
        {
            if (WorldDownloadManager.downloading)
            {
                if (world.id.Equals(WorldDownloadManager.DownloadInfo.ApiWorld.id))
                    {
                        canChangeText = true;
                    }
                    else
                    {
                        canChangeText = false;
                        button.SetText(Constants.BUTTON_BUSY_TEXT);

                    }
                }
            else
            {
                if (CacheManager.HasDownloadedWorld(world.id, world.version))
                    button.SetText(Constants.BUTTON_ALREADY_DOWNLOADED_TEXT);
                else
                    button.SetText(Constants.BUTTON_IDLE_TEXT);

            }
        }

        public static void UpdateTextDownloadStopped()
        {
            try
            {
                if (CacheManager.HasDownloadedWorld(WorldDownloadManager.DownloadInfo.ApiWorld.id,
                    WorldDownloadManager.DownloadInfo.ApiWorld.version))
                    button.SetText(Constants.BUTTON_ALREADY_DOWNLOADED_TEXT);
                else
                    button.SetText(Constants.BUTTON_IDLE_TEXT);
            }
            catch { }
            canChangeText = true;
        }

        private static PageWorldInfo GetWorldInfo()
        {
            return GameObject.Find(PATH_TO_WORLDINFO).GetComponent<VRC.UI.PageWorldInfo>();
        }
        
        public static Action onClick = delegate
        {
            Utilities.DeselectClickedButton(button);
            try
            {
                Utilities.DeselectClickedButton(button);
                if (WorldDownloadManager.downloading || 
                    button.GetTextComponentInChildren().text.Equals(Constants.BUTTON_ALREADY_DOWNLOADED_TEXT))
                {
                    WorldDownloadManager.CancelDownload();
                    return;
                }

                WorldDownloadManager.ProcessDownload(
                    DownloadInfo.CreateWorldPageDownloadInfo(
                        GetWorldInfo().field_Private_ApiWorld_0,
                        GetWorldInfo().field_Public_ApiWorldInstance_0.tagsOnly,
                        DownloadType.World,
                        GetWorldInfo()
                    )
                );
            }
            catch(Exception e) { MelonLogger.LogError($"Exception Occured In Setup For World Download: {e}"); }
        };
        
    }
}
