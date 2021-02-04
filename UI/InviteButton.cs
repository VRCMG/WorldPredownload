﻿using MelonLoader;
using System;
using UnityEngine;
using VRC.Core;
using WorldPredownload.Cache;
using WorldPredownload.DownloadManager;

namespace WorldPredownload.UI
{
    public class InviteButton
    {

        public static bool canChangeText { get; set; } = true;
        public static GameObject button { get; set; }
        private static bool canDownload = true;
        public static bool initialised { get; set; } = false;
        

        private const string PATH_TO_GAMEOBJECT_TO_CLONE = "UserInterface/QuickMenu/QuickModeMenus/QuickModeInviteResponseMoreOptionsMenu/BlockButton";
        private const string PATH_TO_CLONE_PARENT = "UserInterface/QuickMenu/QuickModeMenus/QuickModeInviteResponseMoreOptionsMenu";
        private const string UNABLE_TO_CONVERT_WORLDID = "Error Creating ApiWorld From Notification";

        public static void Setup()
        {
            return;
            button = Utilities.CloneGameObject(PATH_TO_GAMEOBJECT_TO_CLONE, PATH_TO_CLONE_PARENT);
            button.GetRectTrans().SetAnchoredPos(Constants.INVITE_BUTTON_POS);
            button.SetName(Constants.INVITE_BUTTON_NAME);
            button.SetText(Constants.BUTTON_IDLE_TEXT);
            button.SetButtonAction(onClick);
            button.SetActive(true);
            initialised = true;
        }


        public static void UpdateTextDownloadStopped()
        {
            return;
            button.SetText(Constants.BUTTON_IDLE_TEXT);
            canChangeText = true;
        }

        public static void UpdateText()
        {
            return;
            MelonLogger.Msg("Got here");
            
            if(Utilities.GetSelectedNotification().notificationType.Equals("invite")) {
                MelonLogger.Msg("Got here 2");
                button.SetActive(true);
                MelonLogger.Msg("Got here 3");
                if (WorldDownloadManager.downloading)
                {
                    if (Utilities.GetSelectedNotification().GetWorldID().Equals(WorldDownloadManager.DownloadInfo.ApiWorld.id))
                        canChangeText = true;
                    else
                    {
                        canChangeText = false;
                        button.SetText(Constants.BUTTON_BUSY_TEXT);
                    }
                }
                else
                {
                    MelonLogger.Msg("Got here 4");
                    if (CacheManager.HasDownloadedWorld(Utilities.GetSelectedNotification().GetWorldID())) button.SetText(Constants.BUTTON_ALREADY_DOWNLOADED_TEXT);
                    else button.SetText(Constants.BUTTON_IDLE_TEXT);
                }
            } 
            else
                button.SetActive(false);
        }

        public static System.Collections.IEnumerator InviteButtonTimer(int time)
        {
            canDownload = false;
            for (int i = time; i >= 0; i--)
            {
                if (!WorldDownloadManager.downloading)
                    button.SetText($"Time Left:{i}");
                yield return new WaitForSeconds(1);
            }
            canDownload = true;
            UpdateText();
        }

        public static Action onClick = delegate
        {
            return;
            Utilities.DeselectClickedButton(button);
            if (WorldDownloadManager.downloading || button.GetTextComponentInChildren().text
                .Equals(Constants.BUTTON_ALREADY_DOWNLOADED_TEXT))
            {
                WorldDownloadManager.CancelDownload();
                return;
            }

            if (!canDownload)
            {
                Utilities.QueueHudMessage("Please wait a while before trying to download again");
                return;
            }
            //MelonCoroutines.Start(InviteButtonTimer(15));

            //Credit: https://github.com/Psychloor/AdvancedInvites/blob/master/AdvancedInvites/InviteHandler.cs
            API.Fetch<ApiWorld>(Utilities.GetSelectedNotification().GetWorldID(),
                new Action<ApiContainer>(
                    container =>
                    {

                        WorldDownloadManager.ProcessDownload(
                            DownloadInfo.CreateInviteDownloadInfo(
                                container.Model.Cast<ApiWorld>(),
                                Utilities.GetSelectedNotification().GetInstanceIDWithTags(),
                                DownloadType.Invite,
                                Utilities.GetSelectedNotification()
                            )
                        );
                    }),
                new Action<ApiContainer>(delegate
                {
                    MelonLogger.Log(UNABLE_TO_CONVERT_WORLDID);
                }));
        };



    }
}
