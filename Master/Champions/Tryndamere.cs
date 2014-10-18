using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Master
{
    class Tryndamere : Program
    {
        private const String Version = "1.0.4";

        public Tryndamere()
        {
            SkillQ = new Spell(SpellSlot.Q, 320);
            SkillW = new Spell(SpellSlot.W, 750);
            SkillE = new Spell(SpellSlot.E, 660);
            SkillR = new Spell(SpellSlot.R, 400);
            SkillE.SetSkillshot(SkillE.Instance.SData.SpellCastTime, SkillE.Instance.SData.LineWidth, SkillE.Instance.SData.MissileSpeed, false, SkillshotType.SkillshotLine);

            Config.AddSubMenu(new Menu("Combo/Harass", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "autoqusage", "Use Q If Hp Under").SetValue(new Slider(40, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "Use W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "Use E In Combo").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "killstealE", "Auto E To Kill Steal").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "skin", "Use Custom Skin").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "skin1", "Skin Changer").SetValue(new Slider(4, 1, 7)));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "Use Packet To Cast").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate", "useUlt"));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "useR", "Auto Use R").SetValue(true));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "autouseR", "Use R If Hp Under").SetValue(new Slider(20, 1)));

            Config.AddSubMenu(new Menu("Lane/Jungle Clear", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearE", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Draw", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawW", "W Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawE", "E Range").SetValue(true));

            if (Config.Item(Name + "skin").GetValue<bool>())
            {
                Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, Config.Item(Name + "skin1").GetValue<Slider>().Value, Name)).Process();
                lastSkinId = Config.Item(Name + "skin1").GetValue<Slider>().Value;
            }
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Game.PrintChat("<font color = \"#33CCCC\">Master of {0}</font> <font color = \"#fff8e7\">Brian v{1}</font>", Name, Version);
        }

        private void OnGameUpdate(EventArgs args)
        {
            IReady = (IData != null && IData.Slot != SpellSlot.Unknown && IData.State == SpellState.Ready);
            if (Player.IsDead) return;
            var target = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Physical);
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
                    NormalCombo(false);
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    NormalCombo(true);
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneJungClear();
                    break;
            }
            if (Config.Item(Name + "killstealE").GetValue<bool>()) KillSteal();
            if (Config.Item(Name + "useR").GetValue<bool>()) AutoUltimate();
            if (Config.Item(Name + "skin").GetValue<bool>() && Config.Item(Name + "skin1").GetValue<Slider>().Value != lastSkinId)
            {
                Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, Config.Item(Name + "skin1").GetValue<Slider>().Value, Name)).Process();
                lastSkinId = Config.Item(Name + "skin1").GetValue<Slider>().Value;
            }
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item(Name + "DrawW").GetValue<bool>() && SkillW.Level > 0) Utility.DrawCircle(Player.Position, SkillW.Range, SkillW.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawE").GetValue<bool>() && SkillE.Level > 0) Utility.DrawCircle(Player.Position, SkillE.Range, SkillE.IsReady() ? Color.Green : Color.Red);
        }

        void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe || sender.IsAlly) return;
            if (Config.Item(Name + "useR").GetValue<bool>() && SkillR.IsReady())
            {
                if (args.SData.Name == sender.Name + "BasicAttack")
                {
                    if (args.Target == Player && Player.Health <= sender.GetAutoAttackDamage(Player, true)) SkillR.Cast();
                }
                else if (args.Target == Player || args.End.Distance(Player.Position) <= 200)
                {
                    if (Player.Health <= sender.GetDamageSpell(Player, args.SData.Name).CalculatedDamage) SkillR.Cast();
                }
            }
        }

        private void KillSteal()
        {
            var target = SimpleTs.GetTarget(SkillE.Range, SimpleTs.DamageType.Physical);
            if (target == null) return;
            if (SkillE.IsReady() && SkillE.IsKillable(target)) SkillE.Cast(Player.Position + Vector3.Normalize(SkillE.GetPrediction(target).CastPosition - Player.Position) * SkillE.Range, PacketCast);
        }

        private void AutoUltimate()
        {
            if (Utility.CountEnemysInRange(1000) == 0 || !SkillR.IsReady()) return;
            if ((Player.Health * 100 / Player.MaxHealth) <= Config.Item(Name + "autouseR").GetValue<Slider>().Value) SkillR.Cast();
        }

        private void NormalCombo(bool IsHarass)
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && (Player.Health * 100 / Player.MaxHealth) <= Config.Item(Name + "autoqusage").GetValue<Slider>().Value && Utility.CountEnemysInRange(1000) >= 1) SkillQ.Cast();
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady() && targetObj.IsValidTarget(SkillW.Range))
            {
                var Pos = (targetObj.IsMoving) ? targetObj.Position + Vector3.Normalize(targetObj.Path[0] - targetObj.Position) * 100 : default(Vector3);
                var targetMoveS = (targetObj.IsMoving && Player.Distance(Pos) > Player.Distance(targetObj)) ? targetObj.MoveSpeed : 0;
                var targetReach = (Player.Distance(targetObj) - Orbwalking.GetRealAutoAttackRange(targetObj)) / ((Player.MoveSpeed - targetMoveS == 0) ? 0.0001 : Player.MoveSpeed - targetMoveS);
                if (targetReach > 1.7 || targetReach < 0)
                {
                    if (!Orbwalking.InAutoAttackRange(targetObj)) SkillW.Cast();
                }
                else
                {
                    if (Player.GetAutoAttackDamage(targetObj) < targetObj.GetAutoAttackDamage(Player) || Player.Health < targetObj.Health) SkillW.Cast();
                }
            }
            if (!IsHarass && Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && targetObj.IsValidTarget(SkillE.Range) && !Orbwalking.InAutoAttackRange(targetObj)) SkillE.Cast(Player.Position + Vector3.Normalize(SkillE.GetPrediction(targetObj).CastPosition - Player.Position) * SkillE.Range, PacketCast);
            if (Config.Item(Name + "iusage").GetValue<bool>()) UseItem(targetObj);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillE.Range, MinionTypes.All, MinionTeam.NotAlly);
            if (minionObj.Count == 0) return;
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady()) SkillE.Cast(SkillE.GetLineFarmLocation(minionObj.ToList()).Position, PacketCast);
        }

        private void UseItem(Obj_AI_Hero target)
        {
            if (Items.CanUseItem(Bilge) && Player.Distance(target) <= 450) Items.UseItem(Bilge, target);
            if (Items.CanUseItem(Blade) && Player.Distance(target) <= 450) Items.UseItem(Blade, target);
            if (Items.CanUseItem(Tiamat) && Utility.CountEnemysInRange(350) >= 1) Items.UseItem(Tiamat);
            if (Items.CanUseItem(Hydra) && (Utility.CountEnemysInRange(350) >= 2 || (Player.GetAutoAttackDamage(target) < target.Health && Utility.CountEnemysInRange(350) == 1))) Items.UseItem(Hydra);
            if (Items.CanUseItem(Rand) && Utility.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
            if (Items.CanUseItem(Youmuu) && Utility.CountEnemysInRange(350) >= 1) Items.UseItem(Youmuu);
        }
    }
}