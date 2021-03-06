﻿using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using LX_Orbwalker;

namespace Master
{
    class Renekton : Program
    {
        private const String Version = "1.0.3";
        private Vector3 DashBackPos = default(Vector3);
        private bool ECasted = false;

        public Renekton()
        {
            SkillQ = new Spell(SpellSlot.Q, 325);
            SkillW = new Spell(SpellSlot.W, 300);
            SkillE = new Spell(SpellSlot.E, 480);
            SkillR = new Spell(SpellSlot.R, 20);
            SkillE.SetSkillshot(SkillE.Instance.SData.SpellCastTime, SkillE.Instance.SData.LineWidth, SkillE.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotLine);

            Config.AddSubMenu(new Menu("Combo", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "Use W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "Use E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "hsettings"));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "harMode", "Use Harass If Hp Above").SetValue(new Slider(20, 1)));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "useHarQ", "Use Q").SetValue(true));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "useHarW", "Use W").SetValue(true));

            Config.AddSubMenu(new Menu("Lane/Jungle Clear", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearQ", "Use Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearW", "Use W").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearE", "Use E").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearI", "Use Tiamat/Hydra Item").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate", "useUlt"));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "surviveR", "Try Use R To Survive").SetValue(true));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "autouseR", "Use R If Hp Under").SetValue(new Slider(20, 1)));

            Config.AddSubMenu(new Menu("Misc", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useAntiW", "Use W To Anti Gap Closer").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useInterW", "Use W To Interrupt").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "calcelW", "Cancel W Animation").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "SkinID", "Skin Changer").SetValue(new Slider(6, 0, 6))).ValueChanged += SkinChanger;
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "Use Packet To Cast").SetValue(true));

            Config.AddSubMenu(new Menu("Draw", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawQ", "Q Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawE", "E Range").SetValue(true));

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            GameObject.OnCreate += OnCreate;
            Game.PrintChat("<font color = \"#33CCCC\">Master of {0}</font> <font color = \"#fff8e7\">Brian v{1}</font>", Name, Version);
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            PacketCast = Config.Item(Name + "packetCast").GetValue<bool>();
            switch (LXOrbwalker.CurrentMode)
            {
                case LXOrbwalker.Mode.Combo:
                    NormalCombo();
                    break;
                case LXOrbwalker.Mode.Harass:
                    Harass();
                    break;
                case LXOrbwalker.Mode.LaneClear:
                    LaneJungClear();
                    break;
                case LXOrbwalker.Mode.LaneFreeze:
                    LaneJungClear();
                    break;
                case LXOrbwalker.Mode.Flee:
                    if (SkillE.IsReady()) SkillE.Cast(Game.CursorPos, PacketCast);
                    break;
            }
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item(Name + "DrawQ").GetValue<bool>() && SkillQ.Level > 0) Utility.DrawCircle(Player.Position, SkillQ.Range, SkillQ.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawE").GetValue<bool>() && SkillE.Level > 0) Utility.DrawCircle(Player.Position, SkillE.Range, SkillE.IsReady() ? Color.Green : Color.Red);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item(Name + "useAntiW").GetValue<bool>()) return;
            if (gapcloser.Sender.IsValidTarget(SkillE.Range) && (SkillW.IsReady() || Player.HasBuff("RenektonPreExecute")))
            {
                if (!Player.HasBuff("RenektonPreExecute")) SkillW.Cast();
                if (Player.HasBuff("RenektonPreExecute")) Player.IssueOrder(GameObjectOrder.AttackUnit, gapcloser.Sender);
            }
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item(Name + "useInterW").GetValue<bool>()) return;
            if (SkillW.IsReady() && SkillE.IsReady() && !SkillW.InRange(unit.Position) && unit.IsValidTarget(SkillE.Range)) SkillE.Cast(unit.Position + Vector3.Normalize(unit.Position - Player.Position) * 200, PacketCast);
            if (unit.IsValidTarget(SkillW.Range) && (SkillW.IsReady() || Player.HasBuff("RenektonPreExecute")))
            {
                if (!Player.HasBuff("RenektonPreExecute")) SkillW.Cast();
                if (Player.HasBuff("RenektonPreExecute")) Player.IssueOrder(GameObjectOrder.AttackUnit, unit);
            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (Config.Item(Name + "calcelW").GetValue<bool>() && Player.HasBuff("RenektonPreExecute") && args.SData.Name == Name + "BasicAttack" && args.Target == targetObj)
            {
                if (Items.CanUseItem(Tiamat)) Items.UseItem(Tiamat);
                if (Items.CanUseItem(Hydra)) Items.UseItem(Hydra);
            }
            if (args.SData.Name == "RenektonSliceAndDice")
            {
                ECasted = true;
                Utility.DelayAction.Add(400, () => ECasted = false);
                if (LXOrbwalker.CurrentMode == LXOrbwalker.Mode.Harass && DashBackPos == default(Vector3) && ECasted) DashBackPos = Player.Position + (Player.Position - targetObj.Position) * SkillE.Range;
            }
        }

        private void OnCreate(GameObject sender, EventArgs args)
        {
            if (sender is Obj_SpellMissile && sender.IsValid && Config.Item(Name + "surviveR").GetValue<bool>() && SkillR.IsReady())
            {
                var missle = (Obj_SpellMissile)sender;
                var caster = missle.SpellCaster;
                if (caster.IsEnemy)
                {
                    if (missle.SData.Name.Contains("BasicAttack"))
                    {
                        if (missle.Target.IsMe && (Player.Health - caster.GetAutoAttackDamage(Player, true)) * 100 / Player.MaxHealth <= Config.Item(Name + "autouseR").GetValue<Slider>().Value) SkillR.Cast();
                    }
                    else if (missle.Target.IsMe || missle.EndPosition.Distance(Player.Position) <= 130)
                    {
                        if (missle.SData.Name == "summonerdot")
                        {
                            if ((Player.Health - (caster as Obj_AI_Hero).GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite)) * 100 / Player.MaxHealth <= Config.Item(Name + "autouseR").GetValue<Slider>().Value) SkillR.Cast();
                        }
                        else if ((Player.Health - caster.GetDamageSpell(Player, missle.SData.Name).CalculatedDamage) * 100 / Player.MaxHealth <= Config.Item(Name + "autouseR").GetValue<Slider>().Value) SkillR.Cast();
                    }
                }
            }
        }

        private void NormalCombo()
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && SkillE.Instance.Name == "RenektonSliceAndDice" && SkillE.InRange(targetObj.Position)) SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 200, PacketCast);
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.InRange(targetObj.Position) && !ECasted && SkillW.IsReady()) SkillW.Cast();
            if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && SkillQ.InRange(targetObj.Position) && !ECasted) SkillQ.Cast();
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && SkillE.Instance.Name != "RenektonSliceAndDice" && !ECasted && SkillE.InRange(targetObj.Position)) SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 200, PacketCast);
            if (Config.Item(Name + "iusage").GetValue<bool>() && !ECasted) UseItem(targetObj);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void Harass()
        {
            if (targetObj == null || Player.Health * 100 / Player.MaxHealth < Config.Item(Name + "harMode").GetValue<Slider>().Value) return;
            if (SkillE.IsReady() && SkillE.Instance.Name == "RenektonSliceAndDice" && SkillE.InRange(targetObj.Position)) SkillE.Cast(targetObj.Position + Vector3.Normalize(targetObj.Position - Player.Position) * 200, PacketCast);
            if (Config.Item(Name + "useHarW").GetValue<bool>() && SkillW.InRange(targetObj.Position) && !ECasted && SkillW.IsReady()) SkillW.Cast();
            if (Config.Item(Name + "useHarQ").GetValue<bool>() && SkillQ.IsReady() && SkillQ.InRange(targetObj.Position) && !ECasted) SkillQ.Cast();
            if (SkillE.IsReady() && SkillE.Instance.Name != "RenektonSliceAndDice" && !ECasted) SkillE.Cast(DashBackPos, PacketCast);
            if (!SkillE.IsReady()) DashBackPos = default(Vector3);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillE.Range, MinionTypes.All, MinionTeam.NotAlly);
            if (minionObj.Count == 0) return;
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady() && SkillE.Instance.Name == "RenektonSliceAndDice") SkillE.Cast(SkillE.GetLineFarmLocation(minionObj.ToList()).Position, PacketCast);
            if (Config.Item(Name + "useClearQ").GetValue<bool>() && SkillQ.IsReady() && minionObj.Count(i => i.IsValidTarget(SkillQ.Range)) >= 2 && !ECasted) SkillQ.Cast();
            if (Config.Item(Name + "useClearW").GetValue<bool>() && !ECasted && SkillW.IsReady() && minionObj.FirstOrDefault(i => SkillW.InRange(i.Position) && (Player.Mana >= 50) ? SkillW.IsKillable(i, 1) : SkillW.IsKillable(i)) != null) SkillW.Cast();
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady() && SkillE.Instance.Name != "RenektonSliceAndDice" && !ECasted) SkillE.Cast(SkillE.GetLineFarmLocation(minionObj.ToList()).Position, PacketCast);
            if (Config.Item(Name + "useClearI").GetValue<bool>() && minionObj.Count(i => i.IsValidTarget(350)) >= 2 && !ECasted)
            {
                if (Items.CanUseItem(Tiamat)) Items.UseItem(Tiamat);
                if (Items.CanUseItem(Hydra)) Items.UseItem(Hydra);
            }
        }

        private void UseItem(Obj_AI_Hero target)
        {
            if (Items.CanUseItem(Tiamat) && Utility.CountEnemysInRange(350) >= 1) Items.UseItem(Tiamat);
            if (Items.CanUseItem(Hydra) && (Utility.CountEnemysInRange(350) >= 2 || (Player.GetAutoAttackDamage(target) < target.Health && Utility.CountEnemysInRange(350) == 1))) Items.UseItem(Hydra);
            if (Items.CanUseItem(Rand) && Utility.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
        }
    }
}