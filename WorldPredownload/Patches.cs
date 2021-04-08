﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Harmony;
using MelonLoader;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Attributes;
using UnhollowerRuntimeLib;
using VRC.Core;
using VRC.UI;
using WorldPredownload.UI;
using WorldPredownload.Cache;
using WorldPredownload.DownloadManager;
using File = Il2CppSystem.IO.File;
using InfoType = VRC.UI.PageUserInfo.EnumNPublicSealedvaNoOnOfSeReBlInFa9vUnique;
using ListType = UiUserList.EnumNPublicSealedvaNoInFrOnOfSeInFa9vUnique;
using Object = Il2CppSystem.Object;
using OnDownloadComplete = AssetBundleDownloadManager.MulticastDelegateNInternalSealedVoObUnique;

namespace WorldPredownload
{
    //[HarmonyPatch(typeof(NetworkManager), "OnJoinedRoom")]
    class OnJoinedRoomPatch
    {
        static void Prefix() => new Task(CacheManager.UpdateDirectories).Start();
    }

    [HarmonyPatch(typeof(NetworkManager), "OnLeftRoom")]
    class OnLeftRoomPatch
    {
        static void Prefix() => WorldDownloadManager.CancelDownload();
    }
    
    class WorldInfoSetup
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void WorldInfoSetupDelegate(IntPtr thisPtr, IntPtr apiWorld, IntPtr apiWorldInstance, byte something1, byte something2, IntPtr additionalJunk);

        private static WorldInfoSetupDelegate worldInfoSetupDelegate;
        public static void Patch()
        {
            unsafe
            {
                var setupMethod = typeof(PageWorldInfo).GetMethods()
                    .Where(m =>
                        m.Name.StartsWith("Method_Public_Void_ApiWorld_ApiWorldInstance_Boolean_Boolean_") &&
                        !m.Name.Contains("PDM"))
                    .OrderBy(m => m.GetCustomAttribute<CallerCountAttribute>().Count)
                    .Last();
                
                // Thanks to Knah
                var originalMethod = *(IntPtr*) (IntPtr) UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(setupMethod).GetValue(null);
                
                Imports.Hook((IntPtr) (&originalMethod), typeof(WorldInfoSetup).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.Public)!.MethodHandle.GetFunctionPointer());
                
                worldInfoSetupDelegate = Marshal.GetDelegateForFunctionPointer<WorldInfoSetupDelegate>(originalMethod);
            }
        }

        public static void Postfix(IntPtr thisPtr, IntPtr apiWorldPtr, IntPtr apiWorldInstancePtr, byte something1, byte something2, IntPtr additionalJunkPtr)
        {
            try
            {
                worldInfoSetupDelegate(thisPtr, apiWorldPtr, apiWorldInstancePtr, something1, something2, additionalJunkPtr);
                if (apiWorldPtr != IntPtr.Zero) WorldButton.UpdateText(new ApiWorld(apiWorldPtr));
            }
            catch(Exception e)
            {
                MelonLogger.Error($"Something went horribly wrong in WorldInfoSetup Patch, pls report to gompo: {e}");
            }
        }
    }

    
    class WorldDownloadListener
    {
        public static void Patch()
        {
            WorldPredownload.HarmonyInstance.Patch(Utilities.WorldDownloadMethodInfo, 
                new HarmonyMethod(typeof(WorldDownloadListener).GetMethod(nameof(Prefix))));
        }
        public static void Prefix(ApiWorld __0, ref OnDownloadComplete __2)
        {
            __2 = new OnDownloadComplete(
                IL2CPP.Il2CppObjectBaseToPtr(
                    Il2CppSystem.Delegate.Combine(
                        __2,
                        DelegateSupport.ConvertDelegate<OnDownloadComplete>(
                            new Action<AssetBundleDownload>(
                                _ =>
                                {
                                    if (CacheManager.WorldFileExists(__0.id))
                                        CacheManager.AddDirectory(CacheManager.ComputeAssetHash(__0.id));
                                    else
                                        MelonLogger.Warning(
                                            $"Failed to verify world {__0.id} was downloaded. Pls report to gompo");
                                }
                            )
                        )
                    )
                )
            );
        }
    }

    class SocialMenuSetup
    {
        public static void Patch()
        {
            WorldPredownload.HarmonyInstance.Patch(typeof(PageUserInfo).GetMethods().Single(
                    m => m.ReturnType == typeof(void)
                         && m.GetParameters().Length == 3
                         && m.GetParameters()[0].ParameterType == typeof(APIUser)
                         && m.GetParameters()[1].ParameterType == typeof(InfoType)
                         && m.GetParameters()[2].ParameterType == typeof(ListType)
                         && !m.Name.Contains("PDM")
                ),
                null,
                new HarmonyMethod(typeof(SocialMenuSetup).GetMethod(nameof(Postfix)))
            );
        }

        public static void Postfix(APIUser __0 = null) //, InfoType __1, ListType __2 = ListType.None
        {
            if (__0 == null) return;
            if (__0.location == null)
            {
                FriendButton.button.SetActive(false);
                return;
            }
            if (!__0.isFriend || 
                Utilities.isInSameWorld(__0) || 
                __0.location.ToLower().Equals("private") || 
                __0.location.ToLower().Equals("offline")
            )
                FriendButton.button.SetActive(false);
            else
            {
                FriendButton.button.SetActive(true);
                MelonCoroutines.Start(FriendButton.UpdateText());
            }
        }
    }
}
