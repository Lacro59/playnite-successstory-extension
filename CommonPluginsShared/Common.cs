using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using AchievementsLocal.Playnite;
using AchievementsLocal.Models;

namespace CommonPluginsShared
{
    public class Common
    {
        private static ILogger logger = LogManager.GetLogger();


        public static void SetEvent(IPlayniteAPI PlayniteAPI)
        {
            if (PlayniteAPI.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(WindowBase_LoadedEvent));
            }
        }

        private static void WindowBase_LoadedEvent(object sender, System.EventArgs e)
        {
            string WinIdProperty = string.Empty;

            try
            {
                WinIdProperty = ((Window)sender).GetValue(AutomationProperties.AutomationIdProperty).ToString();

                if (WinIdProperty == "WindowSettings")
                {
                    ((Window)sender).Width = 860;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on WindowBase_LoadedEvent for {WinIdProperty}");
            }
        }




        #region Logs
        public static void LogDebug(bool IsIgnored, string Message)
        {
            if (IsIgnored)
            {
                Message = $"[Ignored] {Message}";
            }

#if DEBUG
            logger.Debug(Message);
#else
            if (!IsIgnored) 
            {            
                logger.Debug(Message); 
            }
#endif
        }

        public static void LogError(Exception ex, bool IsIgnored)
        {
            TraceInfos traceInfos = new TraceInfos(ex);
            string Message = string.Empty;

            if (IsIgnored)
            {
                Message = $"[Ignored] ";
            }

            if (!traceInfos.InitialCaller.IsNullOrEmpty())
            {
                Message += $"Error on {traceInfos.InitialCaller}()";
            }

            Message += $"|{traceInfos.FileName}|{traceInfos.LineNumber}";

#if DEBUG
            logger.Error(ex, $"{Message}");
#else
            if (!IsIgnored) 
            {
                logger.Error(ex, $"{Message}");
            }
#endif
        }

        public static void LogError(Exception ex, bool IsIgnored, string Message)
        {
            TraceInfos traceInfos = new TraceInfos(ex);

            if (IsIgnored)
            {
                Message = $"[Ignored] {Message}";
            }

            Message = $"{Message}|{traceInfos.FileName}|{traceInfos.LineNumber}";

#if DEBUG
            logger.Error(ex, $"{Message}");
#else
            if (!IsIgnored) 
            {
                logger.Error(ex, $"{Message}");
            }
#endif
        }
        #endregion


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
