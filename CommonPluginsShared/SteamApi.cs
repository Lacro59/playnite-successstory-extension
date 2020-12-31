﻿using AchievementsLocal.Playnite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using System;
using System.IO;

namespace AchievementsLocal
{
    internal class SteamApi
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly string urlSteamListApp = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";
        private readonly JObject SteamListApp = new JObject();


        public SteamApi(string PluginUserDataPath)
        {
            // Class variable
            string PluginCachePath = PluginUserDataPath + "\\cache\\";
            string PluginCacheFile = PluginCachePath + "\\SteamListApp.json";

            // Load Steam list app
            try
            {
                if (Directory.Exists(PluginCachePath))
                {
                    // From cache if it exists
                    if (File.Exists(PluginCacheFile))
                    {
                        // If not expired
                        if (File.GetLastWriteTime(PluginCacheFile).AddDays(3) > DateTime.Now)
                        {
                            logger.Info("CommonShared - GetSteamAppListFromCache");
                            SteamListApp = JObject.Parse(File.ReadAllText(PluginCacheFile));
                        }
                        else
                        {
                            SteamListApp = GetSteamAppListFromWeb(PluginCacheFile);
                        }
                    }
                    // From web
                    else
                    {
                        SteamListApp = GetSteamAppListFromWeb(PluginCacheFile);
                    }
                }
                else
                {
                    Directory.CreateDirectory(PluginCachePath);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "CommonShared", "Error on load SteamListApp");
            }
        }

        private JObject GetSteamAppListFromWeb(string PluginCacheFile)
        {
#if DEBUG
            logger.Debug("CommonShared [Ignored] - GetSteamAppListFromWeb");
#endif

            string responseData = string.Empty;
            try
            {
                responseData = Web.DownloadStringData(urlSteamListApp).GetAwaiter().GetResult();
                if (responseData.IsNullOrEmpty() || responseData == "{\"applist\":{\"apps\":[]}}")
                {
                    responseData = JsonConvert.SerializeObject(new JObject());
                }
                else
                {
                    // Write file for cache usage
                    File.WriteAllText(PluginCacheFile, responseData);
                }
            }
            catch(Exception ex)
            {
                Common.LogError(ex, "CommonShared", $"Failed to load from {urlSteamListApp}");
                responseData = "{\"applist\":{\"apps\":[]}}";
            }

            return JObject.Parse(responseData);
        }

        public int GetSteamId(string Name)
        {
            int SteamId = 0;
        
            try
            {
                if (SteamListApp != null && SteamListApp["applist"] != null && SteamListApp["applist"]["apps"] != null)
                {
                    foreach (JObject Game in SteamListApp["applist"]["apps"])
                    {
                        string NameSteam = Common.NormalizeGameName((string)Game["name"]);
                        string NameSearch = Common.NormalizeGameName(Name);

                        if (NameSteam == NameSearch)
                        {
                            return (int)Game["appid"];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "CommonShared", $"Error with {Name}");
            }
        
            if (SteamId == 0)
            {
                logger.Warn($"CommonShared - SteamId not find for {Name}");
            }
        
            return SteamId;
        }
    }
}
