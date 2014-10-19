using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Master
{
    class Shen : Program
    {
        private const String Version = "1.0.4";
        private Spell SkillP;
        private long lastTimeAlert = 0;

        public Shen()
        {
            SkillQ = new Spell(SpellSlot.Q, 475);
            SkillW = new Spell(SpellSlot.W, 20);
            SkillE = new Spell(SpellSlot.E, 600);
            SkillR = new Spell(SpellSlot.R, 25000);
            SkillP = new Spell(Player.GetSpellSlot("ShenKiAttack", false), Orbwalking.GetRealAutoAttackRange(null));
            SkillE.SetSkillshot(SkillE.Instance.SData.SpellCastTime, SkillE.Instance.SData.LineWidth, SkillE.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotLine);

            Config.AddSubMenu(new Menu("Combo", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "Use W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "autowusage", "Use W If Hp Under").SetValue(new Slider(20, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "Use E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "multieusage", "Try Use E Multi Target").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "hsettings"));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "useHarrQ", "Use Q").SetValue(true));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "useHarrE", "Use E").SetValue(true));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "harModeE", "Use E If Hp Above").SetValue(new Slider(20, 1)));

            Config.AddSubMenu(new Menu("Lane/Jungle Clear", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearQ", "Use Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearW", "Use W").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useAutoE", "Use E If Enemy In Tower Range").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "lasthitQ", "Use Q To Last Hit").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useAntiE", "Use E To Anti Gap Closer").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useInterE", "Use E To Interrupt").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "skin", "Skin Changer").SetValue(new Slider(6, 0, 6))).ValueChanged += SkinChanger;
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "Use Packet To Cast").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate", "useUlt"));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "alert", "Alert Ally Low Hp").SetValue(true));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "autoalert", "Alert When Ally Hp Under").SetValue(new Slider(30, 1)));

            Config.AddSubMenu(new Menu("Draw", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawQ", "Q Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawE", "E Range").SetValue(true));

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            TimeTick = Stopwatch.StartNew();
            Game.PrintChat("<font color = \"#33CCCC\">Master of {0}</font> <font color = \"#fff8e7\">Brian v{1}</font>", Name, Version);
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            var target = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Magical);
            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Mixed && targetObj != null)
            {
                targetObj = null;
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                targetObj = target;
            }
            else if ((target.IsValidTarget() && targetObj == null) || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                targetObj = target;
            }
            PacketCast = Config.Item(Name + "packetCast").GetValue<bool>();
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    NormalCombo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneJungClear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    LastHit();
                    break;
            }
            if (Config.Item(Name + "alert").GetValue<bool>() && SkillR.IsReady()) UltimateAlert();
            if (Config.Item(Name + "useAutoE").GetValue<bool>() && SkillE.IsReady()) AutoEInTower();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item(Name + "DrawQ").GetValue<bool>() && SkillQ.Level > 0) Utility.DrawCircle(Player.Position, SkillQ.Range, SkillQ.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawE").GetValue<bool>() && SkillE.Level > 0) Utility.DrawCircle(Player.Position, SkillE.Range, SkillE.IsReady() ? Color.Green : Color.Red);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item(Name + "useAntiE").GetValue<bool>()) return;
            if (gapcloser.Sender.IsValidTarget(SkillE.Range) && SkillE.IsReady()) SkillE.Cast(gapcloser.Sender.Position, PacketCast);
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item(Name + "useInterE").GetValue<bool>()) return;
            if (unit.IsValidTarget(SkillE.Range) && SkillE.IsReady()) SkillE.Cast(unit.Position, PacketCast);
        }

        private void UltimateAlert()
        {
            foreach (var allyObj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsAlly && !i.IsMe && !i.IsDead && Utility.CountEnemysInRange(1000, i) >= 1 && (i.Health * 100 / i.MaxHealth) <= Config.Item(Name + "autoalert").GetValue<Slider>().Value))
            {
                if ((TimeTick.ElapsedMilliseconds - lastTimeAlert) > 5000)
                {
                    Game.PrintChat("Use Ultimate (R) To Help: {0}", allyObj.ChampionName);
                    Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(allyObj.Position.X, allyObj.Position.Y, allyObj.NetworkId, Player.NetworkId, Packet.PingType.FallbackSound)).Process();
                    lastTimeAlert = TimeTick.ElapsedMilliseconds;
                }
            }
        }

        private void AutoEInTower()
        {
            var nearTower = ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(i => i.IsValidTarget(775, false));
            if (nearTower == null) return;
            var target = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(i => i.IsValidTarget(775, true, i.Position) && i.IsValidTarget(SkillE.Range));
            if (target != null) SkillE.Cast(target.Position, PacketCast);
        }

        private void NormalCombo()
        {
            if (targetObj == null) return;
            IEnumerable<SpellSlot> ComboQE = new[] { SpellSlot.Q, SpellSlot.E };
            var AADmg = Player.GetAutoAttackDamage(targetObj) + (SkillP.IsReady() ? Player.CalcDamage(targetObj, Damage.DamageType.Magical, 4 + (4 * Player.Level) + (0.1 * Player.ScriptHealthBonus)) : 0);
            //Game.PrintChat("{0}/{1}", Player.GetAutoAttackDamage(targetObj), 4 + (4 * Player.Level) + (0.1 * Player.ScriptHealthBonus));
            if (targetObj.Health < Player.GetComboDamage(targetObj, ComboQE) + AADmg)
            {
                if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && targetObj.IsValidTarget(SkillQ.Range) && SkillQ.IsKillable(targetObj))
                {
                    SkillQ.Cast(targetObj, PacketCast);
                }
                else if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && targetObj.Health < Player.GetComboDamage(targetObj, ComboQE) && targetObj.IsValidTarget(SkillE.Range))
                {
                    SkillE.Cast(targetObj.Position, PacketCast);
                    SkillQ.Cast(targetObj, PacketCast);
                }
                else
                {
                    if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && targetObj.IsValidTarget(SkillE.Range)) SkillE.Cast(targetObj.Position, PacketCast);
                    if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && targetObj.IsValidTarget(SkillQ.Range)) SkillQ.Cast(targetObj, PacketCast);
                }
            }
            else
            {
                if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && targetObj.IsValidTarget(SkillE.Range))
                {
                    if (Config.Item(Name + "multieusage").GetValue<bool>())
                    {
                        if (ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(SkillE.Range) && i != targetObj).Count() >= 1)
                        {
                            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(SkillE.Range) && i != targetObj))
                            {
                                var Segment = Geometry.ProjectOn(targetObj.Position.To2D(), Player.Position.To2D(), target.Position.To2D());
                                if (Segment.IsOnSegment && targetObj.Distance(Segment.SegmentPoint) <= targetObj.BoundingRadius + SkillE.Width) SkillE.Cast(target.Position, PacketCast);
                            }
                        }
                        else SkillE.Cast(targetObj.Position, PacketCast);
                    }
                    else SkillE.Cast(targetObj.Position, PacketCast);
                }
                if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && targetObj.IsValidTarget(SkillQ.Range)) SkillQ.Cast(targetObj, PacketCast);
            }
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady() && targetObj.IsValidTarget(SkillE.Range) && (Player.Health * 100 / Player.MaxHealth) <= Config.Item(Name + "autowusage").GetValue<Slider>().Value) SkillW.Cast();
            if (Config.Item(Name + "iusage").GetValue<bool>() && Items.CanUseItem(Rand) && Utility.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void Harass()
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "useHarrE").GetValue<bool>())
            {
                if (SkillE.IsReady() && targetObj.IsValidTarget(SkillE.Range) && (Player.Health * 100 / Player.MaxHealth) >= Config.Item(Name + "harModeE").GetValue<Slider>().Value) SkillE.Cast(targetObj.Position, PacketCast);
            }
            if (Config.Item(Name + "useHarrQ").GetValue<bool>() && SkillQ.IsReady() && targetObj.IsValidTarget(SkillQ.Range)) SkillQ.Cast(targetObj, PacketCast);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly).OrderBy(i => i.Distance(Player)).FirstOrDefault();
            if (minionObj == null) return;
            if (Config.Item(Name + "useClearW").GetValue<bool>() && SkillW.IsReady() && Orbwalking.InAutoAttackRange(minionObj)) SkillW.Cast();
            if (Config.Item(Name + "useClearQ").GetValue<bool>() && SkillQ.IsReady()) SkillQ.Cast(minionObj, PacketCast);
        }

        private void LastHit()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault(i => SkillQ.IsKillable(i));
            if (minionObj == null || !Config.Item(Name + "lasthitQ").GetValue<bool>()) return;
            if (SkillQ.IsReady()) SkillQ.Cast(minionObj, PacketCast);
        }
    }
}