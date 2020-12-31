﻿using Newtonsoft.Json;
using Playnite.SDK;
using AchievementsLocal.Playnite;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using AchievementsLocal.Models;

namespace AchievementsLocal
{
    internal class Common
    {
        private static ILogger logger = LogManager.GetLogger();


        /// <summary>
        /// Set in application ressources the common ressources.
        /// </summary>
        /// <param name="pluginFolder"></param>
        public static void Load(string pluginFolder)
        {
            List<string> ListCommonFiles = new List<string>
            {
                Path.Combine(pluginFolder, "Resources\\Common.xaml"),
                Path.Combine(pluginFolder, "Resources\\LiveChartsCommon\\Common.xaml")
            };

            foreach (string CommonFile in ListCommonFiles)
            {
                if (File.Exists(CommonFile))
                {
#if DEBUG
                    logger.Debug($"CommonShared [Ignored] - Load {CommonFile}");
#endif

                    ResourceDictionary res = null;
                    try
                    {
                        res = Xaml.FromFile<ResourceDictionary>(CommonFile);
                        res.Source = new Uri(CommonFile, UriKind.Absolute);

                        foreach (var key in res.Keys)
                        {
                            if (res[key] is string locString && locString.IsNullOrEmpty())
                            {
                                res.Remove(key);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "CommonShared", $"Failed to parse file {CommonFile}");
                        return;
                    }

#if DEBUG
                    logger.Debug($"CommonShared [Ignored] - res: {JsonConvert.SerializeObject(res)}");
#endif
                    Application.Current.Resources.MergedDictionaries.Add(res);
                }
                else
                {
                    logger.Warn($"CommonShared - File {CommonFile} not find");
                    return;
                }
            }


            // Add font
            string FontFile = Path.Combine(pluginFolder, "Resources\\font.ttf");
            if (File.Exists(FontFile))
            {
                FontFamily fontFamily = new FontFamily(new Uri(FontFile), "./#font");
                Application.Current.Resources.Remove("CommonFont");
                Application.Current.Resources.Add("CommonFont", fontFamily);
            }
            else
            {
                logger.Warn($"CommonShared - -File not find {FontFile}");
            }
        }


        public static void SetEvent(IPlayniteAPI PlayniteAPI)
        {
            if (PlayniteAPI.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(WindowBase_LoadedEvent));
            }
        }

        private static void WindowBase_LoadedEvent(object sender, System.EventArgs e)
        {
            string WinIdProperty = String.Empty;

            try
            {
                WinIdProperty = ((Window)sender).GetValue(AutomationProperties.AutomationIdProperty).ToString();

                if (WinIdProperty == "WindowSettings")
                {
                    ((Window)sender).Width = 850;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "CommonShared", $"Error on WindowBase_LoadedEvent for {WinIdProperty}");
            }
        }



        /// <summary>
        /// Normalize log error in Playnite.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="PluginName"></param>
        public static void LogError(Exception ex, string PluginName)
        {
            TraceInfos traceInfos = new TraceInfos(ex);
            string Message = $"{PluginName} [{traceInfos.FileName} {traceInfos.LineNumber}]";

            if (!traceInfos.InitialCaller.IsNullOrEmpty())
            {
                Message += $" - Error on {traceInfos.InitialCaller}()";
            }

            logger.Error(ex, $"{Message}");
        }

        /// <summary>
        /// Normalize log error in Playnite.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="PluginName"></param>
        /// <param name="Message"></param>
        public static void LogError(Exception ex, string PluginName, string Message)
        {
            TraceInfos traceInfos = new TraceInfos(ex);
            Message = $"{PluginName} [{traceInfos.FileName} {traceInfos.LineNumber}] - {Message}";

            logger.Error(ex, $"{Message}");
        }


        public static string NormalizeGameName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var newName = name.ToLower();
            newName = newName.RemoveTrademarks();
            newName = newName.Replace("_", "");
            newName = newName.Replace(".", "");
            newName = newName.Replace('’', '\'');
            newName = newName.Replace(":", "");
            newName = newName.Replace("-", "");
            newName = newName.Replace("goty", "");
            newName = newName.Replace("game of the year edition", "");
            newName = newName.Replace("  ", " ");

            return newName.Trim();
        }
    }
}
