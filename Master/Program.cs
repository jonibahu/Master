using System;
using System.Diagnostics;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Master
{
    class Program
    {
        public static Obj_AI_Hero Player = ObjectManager.Player, targetObj = null;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell SkillQ, SkillW, SkillE, SkillR;
        private static SpellDataInst FData, SData, IData;
        public static Int32 Tiamat = 3077, Hydra = 3074, Blade = 3153, Bilge = 3144, Rand = 3143, Youmuu = 3142;
        public static Menu Config;
        public static String Name;
        public static Boolean PacketCast = false;
        public static Stopwatch TimeTick;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
            CustomEvents.Game.OnGameEnd += OnGameEnd;
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
                if (Activator.CreateInstance(null, "Master." + Name) != null)
                {
                    Config.AddToMainMenu();
                    SkinChanger(null, null);
                }
            }
            catch
            {
            }
        }

        private static void OnGameEnd(EventArgs args)
        {
            if (TimeTick.IsRunning) TimeTick.Stop();
        }

        public static void SkinChanger(object sender, OnValueChangeEventArgs e)
        {
            Utility.DelayAction.Add(10, () => Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, Config.Item(Name + "skin").GetValue<Slider>().Value, Name)).Process());
        }

        public static void Orbwalk(Obj_AI_Base target = null)
        {
            Orbwalking.Orbwalk((target == null) ? SimpleTs.GetTarget(-1, SimpleTs.DamageType.Physical) : target, Game.CursorPos, Config.Item("ExtraWindup").GetValue<Slider>().Value, Config.Item("HoldPosRadius").GetValue<Slider>().Value);
        }

        public static bool CheckingCollision(Obj_AI_Hero target, Spell Skill)
        {
            foreach (var col in MinionManager.GetMinions(Player.Position, Skill.Range, MinionTypes.All, MinionTeam.NotAlly))
            {
                var Segment = Geometry.ProjectOn(col.Position.To2D(), Player.Position.To2D(), target.Position.To2D());
                if (Segment.IsOnSegment && col.Distance(Segment.SegmentPoint) <= col.BoundingRadius + Skill.Width && CastSmite(col)) return true;
            }
            return false;
        }

        public static bool FlashReady()
        {
            return (FData != null && FData.Slot != SpellSlot.Unknown && FData.State == SpellState.Ready);
        }

        public static bool SmiteReady()
        {
            return (SData != null && SData.Slot != SpellSlot.Unknown && SData.State == SpellState.Ready);
        }

        public static bool IgniteReady()
        {
            return (IData != null && IData.Slot != SpellSlot.Unknown && IData.State == SpellState.Ready);
        }

        public static bool CastFlash(Vector3 pos)
        {
            return (FlashReady() && Player.SummonerSpellbook.CastSpell(FData.Slot, pos));
        }

        public static bool CastSmite(Obj_AI_Base target)
        {
            if (SmiteReady() && target.IsValidTarget(SData.SData.CastRange[0]) && target.Health < Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Smite))
            {
                Player.SummonerSpellbook.CastSpell(SData.Slot, target);
                return true;
            }
            return false;
        }

        public static bool CastIgnite(Obj_AI_Hero target)
        {
            if (IgniteReady() && target.IsValidTarget(IData.SData.CastRange[0]) && target.Health < Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite))
            {
                Player.SummonerSpellbook.CastSpell(IData.Slot, target);
                return true;
            }
            return false;
        }
    }
}