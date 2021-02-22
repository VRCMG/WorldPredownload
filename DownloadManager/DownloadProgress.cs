﻿using System;
using UnhollowerRuntimeLib;
using UnityEngine.Networking;
using WorldPredownload.UI;
using OnDownloadProgress = AssetBundleDownloadManager.MulticastDelegateNInternalSealedVoUnUnique;

namespace WorldPredownload.DownloadManager
{
    public static class DownloadProgress
    {
        private static OnDownloadProgress onProgressDel;
        public static OnDownloadProgress GetOnProgressDel
        {
            get
            {
                if (onProgressDel != null) return onProgressDel;
                onProgressDel = 
                    DelegateSupport.ConvertDelegate<OnDownloadProgress>(
                        new Action<UnityWebRequest>(
                            delegate(UnityWebRequest request)
                            {
                                if (WorldDownloadManager.cancelled)
                                {
                                    request.Abort();
                                    WorldDownloadManager.cancelled = false;
                                    return;
                                }
                                string size = request.GetResponseHeader("Content-Length");
                                if (request.downloadProgress >= 0 && 0.9 >= request.downloadProgress)
                                {
                                    string progress = $"Progress:{((request.downloadProgress / 0.9) * 100).ToString("0")} %";
                                    if(ModSettings.showStatusOnQM) WorldDownloadStatus.gameObject.SetText(progress);
                                    if (InviteButton.canChangeText) InviteButton.button.SetText(progress);
                                    if (FriendButton.canChangeText) FriendButton.button.SetText(progress);
                                    if (WorldButton.canChangeText) WorldButton.button.SetText(progress);
                                    if (ModSettings.showStatusOnHud) HudIcon.Update((float)(request.downloadProgress/0.9));
                                }
                            
                            }));
                return onProgressDel;
            }
        }

    }
}