using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Master
{
    class DrMundo : Program
    {
        private const String Version = "1.0.2";

        public DrMundo()
        {
            SkillQ = new Spell(SpellSlot.Q, 1100);
            SkillW = new Spell(SpellSlot.W, 325);
            SkillE = new Spell(SpellSlot.E, 300);
            SkillR = new Spell(SpellSlot.R, 20);
            SkillQ.SetSkillshot(SkillQ.Instance.SData.SpellCastTime, SkillQ.Instance.SData.LineWidth, SkillQ.Instance.SData.MissileSpeed, true, SkillshotType.SkillshotLine);

            Config.AddSubMenu(new Menu("Combo/Harass", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "Use W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "autowusage", "Use W If Hp Above").SetValue(new Slider(20, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "Use E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "lasthitQ", "Use Q To Last Hit").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "killstealQ", "Auto Q To Kill Steal").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "smite", "Auto Smite Collision Minion").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "skin", "Skin Changer").SetValue(new Slider(7, 0, 7))).ValueChanged += SkinChanger;
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "Use Packet To Cast").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate", "useUlt"));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "useR", "Auto Use R").SetValue(true));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "autouseR", "Use R If Hp Under").SetValue(new Slider(35, 1)));

            Config.AddSubMenu(new Menu("Lane/Jungle Clear", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearQ", "Use Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearW", "Use W").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearAutoW", "Use W If Hp Above").SetValue(new Slider(20, 1)));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearE", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Draw", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawQ", "Q Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawW", "W Range").SetValue(true));

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
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
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                NormalCombo();
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) LaneJungClear();
            if (Config.Item(Name + "killstealQ").GetValue<bool>()) KillSteal();
            if (Config.Item(Name + "useR").GetValue<bool>()) AutoUltimate();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item(Name + "DrawQ").GetValue<bool>() && SkillQ.Level > 0) Utility.DrawCircle(Player.Position, SkillQ.Range, SkillQ.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawW").GetValue<bool>() && SkillW.Level > 0) Utility.DrawCircle(Player.Position, SkillW.Range, SkillW.IsReady() ? Color.Green : Color.Red);
        }

        private void KillSteal()
        {
            var target = SimpleTs.GetTarget(SkillQ.Range, SimpleTs.DamageType.Magical);
            if (target == null) return;
            if (SkillQ.IsReady() && SkillQ.IsKillable(target))
            {
                if (Config.Item(Name + "smite").GetValue<bool>() && SkillQ.GetPrediction(target).Hitchance == HitChance.Collision) CheckingCollision(target, SkillQ);
                SkillQ.Cast(target, PacketCast);
            }
        }

        private void AutoUltimate()
        {
            if (Utility.CountEnemysInRange(1000) == 0 || !SkillR.IsReady()) return;
            if ((Player.Health * 100 / Player.MaxHealth) <= Config.Item(Name + "autouseR").GetValue<Slider>().Value) SkillR.Cast();
        }

        private void NormalCombo()
        {
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady() && Player.HasBuff("BurningAgony") && Utility.CountEnemysInRange(500) == 0) SkillW.Cast();
            if (targetObj == null) return;
            if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady())
            {
                if (Config.Item(Name + "smite").GetValue<bool>() && SkillQ.GetPrediction(targetObj).Hitchance == HitChance.Collision) CheckingCollision(targetObj, SkillQ);
                SkillQ.Cast(targetObj, PacketCast);
            }
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady())
            {
                if ((Player.Health * 100 / Player.MaxHealth) > Config.Item(Name + "autowusage").GetValue<Slider>().Value)
                {
                    if (targetObj.IsValidTarget(SkillW.Range))
                    {
                        if (!Player.HasBuff("BurningAgony")) SkillW.Cast();
                    }
                    else
                    {
                        if (Player.HasBuff("BurningAgony")) SkillW.Cast();
                    }
                }
                else
                {
                    if (Player.HasBuff("BurningAgony")) SkillW.Cast();
                }
            }
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && Orbwalking.InAutoAttackRange(targetObj)) SkillE.Cast();
            if (Config.Item(Name + "iusage").GetValue<bool>() && Items.CanUseItem(Rand) && Utility.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly).OrderBy(i => i.Distance(Player)).FirstOrDefault();
            if (minionObj == null)
            {
                if (Config.Item(Name + "useClearW").GetValue<bool>() && SkillW.IsReady() && Player.HasBuff("BurningAgony")) SkillW.Cast();
                return;
            }
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady() && Orbwalking.InAutoAttackRange(minionObj)) SkillE.Cast();
            if (Config.Item(Name + "useClearW").GetValue<bool>() && SkillW.IsReady())
            {
                if ((Player.Health * 100 / Player.MaxHealth) > Config.Item(Name + "useClearAutoW").GetValue<Slider>().Value)
                {
                    if (MinionManager.GetMinions(Player.Position, SkillW.Range, MinionTypes.All, MinionTeam.NotAlly).Count >= 2)
                    {
                        if (!Player.HasBuff("BurningAgony")) SkillW.Cast();
                    }
                    else
                    {
                        if (Player.HasBuff("BurningAgony")) SkillW.Cast();
                    }
                }
                else
                {
                    if (Player.HasBuff("BurningAgony")) SkillW.Cast();
                }
            }
            if (Config.Item(Name + "useClearQ").GetValue<bool>() && SkillQ.IsReady() && SkillQ.IsKillable(minionObj)) SkillQ.Cast(minionObj, PacketCast);
        }

        private void LastHit()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly).OrderBy(i => i.Distance(Player)).FirstOrDefault();
            if (minionObj == null || !Config.Item(Name + "lasthitQ").GetValue<bool>()) return;
            if (SkillQ.IsReady() && SkillQ.IsKillable(minionObj)) SkillQ.Cast(minionObj, PacketCast);
        }
    }
}