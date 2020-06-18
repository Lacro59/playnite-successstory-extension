using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using AchievementsLocal.Models;
using Newtonsoft.Json.Linq;
using Playnite.Common.Web;
using Playnite.SDK;
using PluginCommon;

namespace AchievementsLocal
{
    public class SteamEmulators
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI PlayniteApi { get; set; }

        private List<string> AchievementsDirectories = new List<string>();

        public SteamEmulators(IPlayniteAPI PlayniteApi)
        {
            this.PlayniteApi = PlayniteApi;

            AchievementsDirectories.Add("%PUBLIC%\\Documents\\Steam\\CODEX");
            AchievementsDirectories.Add("%appdata%\\Steam\\CODEX");

            AchievementsDirectories.Add(Environment.ExpandEnvironmentVariables("%ProgramData%\\Steam"));
            AchievementsDirectories.Add(Environment.ExpandEnvironmentVariables("%localappdata%\\SKIDROW"));
            AchievementsDirectories.Add(Environment.ExpandEnvironmentVariables("%DOCUMENTS%\\SKIDROW"));
        }

        public GameAchievements GetAchievementsLocal(string GameName, string apiKey)
        {
            List<Achievements> Achievements = new List<Achievements>();
            bool HaveAchivements = false;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            int SteamId = GetSteamId(GameName);

            Achievements = Get(SteamId, apiKey);
            if (Achievements != new List<Achievements>())
            {
                HaveAchivements = true;

                for (int i = 0; i < Achievements.Count; i++)
                {
                    if (Achievements[i].DateUnlocked == default(DateTime))
                    {
                        Locked += 1;
                    }
                    else
                    {
                        Unlocked += 1;
                    }
                }

                Total = Achievements.Count;
            }

            GameAchievements Result = new GameAchievements
            {
                Name = GameName,
                HaveAchivements = HaveAchivements,
                Total = Total,
                Unlocked = Unlocked,
                Locked = Locked,
                Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0,
                Achievements = Achievements
            };

            return Result;
        }


        private int GetSteamId(string GameName, bool IsLoop = false)
        {
            string url = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";

            try
            {    
                string ResultWeb = HttpDownloader.DownloadString(url, Encoding.UTF8);
                JObject ListSteamGame = JObject.Parse(ResultWeb);

                foreach (JObject Apps in ListSteamGame["applist"]["apps"])
                {
                    if (GameName.ToLower() == ((string)Apps["name"]).ToLower())
                    {
                        return int.Parse((string)Apps["appid"]);
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.BadRequest: // HTTP 400
                            break;
                        case HttpStatusCode.ServiceUnavailable: // HTTP 503
                            break;
                        default:
                            var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                            string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                            logger.Error(ex, $"AchievementsLocal [{FileName} {LineNumber}] - Failed to load from {url}. ");
                            break;
                    }

                    logger.Error($"AchievementsLocal - No Find SteamId for {GameName}. ");
                    return 0;
                }
            }

            logger.Error($"AchievementsLocal - No Find SteamId for {GameName}. ");

            if (!IsLoop)
            {
                int SteamId = GetSteamId(GameName.Replace(":", ""), true);
                if (SteamId != 0)
                {
                    logger.Info($"AchievementsLocal - Find SteamId in loop. ");
                    return SteamId;
                }
            }
            
            return 0;
        }



        private List<Achievements> Get(int SteamId, string apiKey)
        {
            List<Achievements> ReturnAchievements = new List<Achievements>();

            //logger.Debug($"AchievementsLocal - SteamId: {SteamId}");

            // Search data local
            foreach (string DirAchivements in AchievementsDirectories)
            {
                switch (DirAchivements.ToLower())
                {
                    case ("%public%\\documents\\steam\\codex"):
                    case ("%appdata%\\steam\\codex"):

                        if (File.Exists(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{SteamId}\\achievements.ini"))
                        {
                            string line;

                            string Name = "";
                            DateTime? DateUnlocked = null;

                            StreamReader file = new StreamReader(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{SteamId}\\achievements.ini");
                            while ((line = file.ReadLine()) != null)
                            {
                                // Achievement name
                                if (line.IndexOf("[") > -1)
                                {
                                    Name = line.Replace("[", "").Replace("]", "").Trim();
                                }
                                // Achievement UnlockTime
                                if (line.IndexOf("UnlockTime") > -1 && line.ToLower() != "unlocktime=0")
                                {
                                    DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(int.Parse(line.Replace("UnlockTime=", "")));
                                }

                                // End Achievement
                                if (line.Trim() == "" && DateUnlocked != null)
                                {
                                    ReturnAchievements.Add(new Achievements
                                    {
                                        Name = Name,
                                        Description = "",
                                        UrlUnlocked = "",
                                        UrlLocked = "",
                                        DateUnlocked = DateUnlocked
                                    });

                                    Name = "";
                                    DateUnlocked = null;
                                }
                            }
                            file.Close();
                        }
                        break;
                }
            }

            if (ReturnAchievements == new List<Achievements>())
            {
                logger.Error($"AchievementsLocal - No data for {SteamId}. ");
                return new List<Achievements>();
            }

            //logger.Debug($"AchievementsLocal - Middle - " + JsonConvert.SerializeObject(ReturnAchievements));

            #region Get details achievements
            // List details acheviements
            string lang = CodeLang.GetSteamLang(Localization.GetPlayniteLanguageConfiguration(PlayniteApi.Paths.ConfigurationPath));
            string url = string.Format(@"https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key={0}&appid={1}&l={2}",
                apiKey, SteamId, lang);

            string ResultWeb = "";
            try
            {
                ResultWeb = HttpDownloader.DownloadString(url, Encoding.UTF8);
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.BadRequest: // HTTP 400
                            break;
                        case HttpStatusCode.ServiceUnavailable: // HTTP 503
                            break;
                        default:
                            var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                            string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                            logger.Error(ex, $"AchievementsLocal [{FileName} {LineNumber}] - Failed to load from {url}. ");
                            break;
                    }
                    return new List<Achievements>();
                }
            }

            if (ResultWeb != "")
            {
                JObject resultObj = JObject.Parse(ResultWeb);
                JArray resultItems = new JArray();

                try
                {
                    resultItems = (JArray)resultObj["game"]["availableGameStats"]["achievements"];

                    for (int i = 0; i < resultItems.Count; i++)
                    {
                        bool isFind = false;
                        for (int j = 0; j < ReturnAchievements.Count; j++)
                        {
                            if (ReturnAchievements[j].Name.ToLower() == ((string)resultItems[i]["name"]).ToLower())
                            {
                                Achievements temp = new Achievements
                                {
                                    Name = (string)resultItems[i]["displayName"],
                                    Description = (string)resultItems[i]["description"],
                                    UrlUnlocked = (string)resultItems[i]["icon"],
                                    UrlLocked = (string)resultItems[i]["icongray"],
                                    DateUnlocked = ReturnAchievements[j].DateUnlocked
                                };

                                isFind = true;
                                ReturnAchievements[j] = temp;
                                j = ReturnAchievements.Count;
                            }
                        }

                        if (!isFind)
                        {
                            ReturnAchievements.Add(new Achievements
                            {
                                Name = (string)resultItems[i]["displayName"],
                                Description = (string)resultItems[i]["description"],
                                UrlUnlocked = (string)resultItems[i]["icon"],
                                UrlLocked = (string)resultItems[i]["icongray"],
                                DateUnlocked = default(DateTime)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                    string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                    logger.Error(ex, $"AchievementsLocal [{FileName} {LineNumber}] - Failed to parse. ");
                    return new List<Achievements>();
                }
            }
            #endregion

            //logger.Debug($"AchievementsLocal - End - " + JsonConvert.SerializeObject(ReturnAchievements));

            return ReturnAchievements;
        }
    }
}
