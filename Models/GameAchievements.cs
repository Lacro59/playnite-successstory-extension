﻿using System.Collections.Generic;

namespace AchievementsLocal.Models
{
    /// <summary>
    /// Represents GameAchievements file.
    /// </summary>
    public class GameAchievements
    {
        /// <summary>
        /// Game Name in the Playnite database.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool HaveAchivements { get; set; }
        /// <summary>
        /// Total achievements for the game.
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Unlocked { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Locked { get; set; }
        /// <summary>
        /// Percentage
        /// </summary>
        public int Progression { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<Achievements> Achievements { get; set; }
    }
}
