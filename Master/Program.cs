using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace Master
{
    class Program
    {
        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            Config = new Menu("Master Of " + ObjectManager.Player.ChampionName, "Master_" + ObjectManager.Player.ChampionName, true);
            var tsMenu = new Menu("Target Selector", "TSSettings");
            SimpleTs.AddToMenu(tsMenu);
            Config.AddSubMenu(tsMenu);

            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));
            Config.Item("Orbwalk").DisplayName = "Normal Combo";
            Config.Item("Farm").DisplayName = "Harrass";
            Config.Item("LaneClear").DisplayName = "Lane/Jungle Clear";
            try
            {
                Activator.CreateInstance(null, "Master." + ObjectManager.Player.ChampionName);
            }
            catch
            {
            }
            Config.AddToMainMenu();
        }
    }
}