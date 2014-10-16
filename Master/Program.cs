using System;

using LeagueSharp;
using LeagueSharp.Common;

namespace Master
{
    class Program
    {
        public static Obj_AI_Hero Player = ObjectManager.Player, targetObj = null;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell SkillQ, SkillW, SkillE, SkillR;
        public static SpellDataInst FData, SData, IData;
        public static Boolean FReady = false, SReady = false, IReady = false;
        public static Menu Config;
        public static String Name;
        public static Boolean PacketCast = false;
        public static Int32 lastSkinId = 0;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            Name = Player.ChampionName;
            var QData = Player.Spellbook.GetSpell(SpellSlot.Q);
            var WData = Player.Spellbook.GetSpell(SpellSlot.W);
            var EData = Player.Spellbook.GetSpell(SpellSlot.E);
            var RData = Player.Spellbook.GetSpell(SpellSlot.R);
            //Game.PrintChat("{0}/{1}/{2}/{3}", QData.SData.CastRange[0], WData.SData.CastRange[0], EData.SData.CastRange[0], RData.SData.CastRange[0]);
            FData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonerflash"));
            SData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonersmite"));
            IData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonerdot"));
            Config = new Menu("Master Of " + Name, "Master_" + Name, true);
            var tsMenu = new Menu("Target Selector", "TSSettings");
            SimpleTs.AddToMenu(tsMenu);
            Config.AddSubMenu(tsMenu);

            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));
            Config.Item("Orbwalk").DisplayName = "Normal Combo";
            Config.Item("Farm").DisplayName = "Harass";
            Config.Item("LaneClear").DisplayName = "Lane/Jungle Clear";
            try
            {
                if (Activator.CreateInstance(null, "Master." + Name) != null) Config.AddToMainMenu();
            }
            catch
            {
            }
        }
    }
}