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
        private const String Version = "1.0.0";
        private Spell SkillQ, SkillW, SkillE, SkillR;
        private SpellDataInst QData, WData, EData, RData, IData;
        private Boolean IReady = false;
        private Int32 Tiamat = 3077, Hydra = 3074, Blade = 3153, Bilge = 3144, Rand = 3143;
        private Boolean TiamatReady = false, HydraReady = false, BladeReady = false, BilgeReady = false, RandReady = false;

        public Tryndamere()
        {
            QData = Player.Spellbook.GetSpell(SpellSlot.Q);
            WData = Player.Spellbook.GetSpell(SpellSlot.W);
            EData = Player.Spellbook.GetSpell(SpellSlot.E);
            RData = Player.Spellbook.GetSpell(SpellSlot.R);
            IData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonerdot"));
            SkillQ = new Spell(QData.Slot, QData.SData.CastRange[0]);
            SkillW = new Spell(WData.Slot, 750);
            SkillE = new Spell(EData.Slot, 660);
            SkillR = new Spell(RData.Slot, RData.SData.CastRange[0]);
            SkillE.SetSkillshot(-EData.SData.SpellCastTime, EData.SData.LineWidth, EData.SData.MissileSpeed, false, SkillshotType.SkillshotLine);

            Config.AddSubMenu(new Menu("Combo/Harass Settings", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "autoqusage", "Use Q If Hp Under").SetValue(new Slider(20, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "Use W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "Use E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Misc Settings", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "killstealE", "Auto E To Kill Steal").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "skin", "Use Custom Skin").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "skin1", "Skin Changer").SetValue(new Slider(4, 1, 7)));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "Use Packet To Cast").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate Settings", "useUlt"));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "useR", "Auto Use R").SetValue(true));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "autouseR", "Use R If Hp Under").SetValue(new Slider(10, 1)));

            Config.AddSubMenu(new Menu("Lane/Jungle Clear Settings", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearE", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Draw Settings", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawW", "W Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawE", "E Range").SetValue(true));

            if (Config.Item(Name + "skin").GetValue<bool>())
            {
                Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, Config.Item(Name + "skin1").GetValue<Slider>().Value, Name)).Process();
                lastSkinId = Config.Item(Name + "skin1").GetValue<Slider>().Value;
            }
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Game.PrintChat("<font color = \"#33CCCC\">Master of {0}</font> <font color = \"#fff8e7\">Brian v{1}</font>", Name, Version);
        }

        private void OnGameUpdate(EventArgs args)
        {
            IReady = (IData != null && IData.Slot != SpellSlot.Unknown && IData.State == SpellState.Ready);
            TiamatReady = Items.CanUseItem(Tiamat);
            HydraReady = Items.CanUseItem(Hydra);
            BladeReady = Items.CanUseItem(Blade);
            BilgeReady = Items.CanUseItem(Bilge);
            RandReady = Items.CanUseItem(Rand);
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
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                NormalCombo();
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) LaneJungClear();
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

        private void KillSteal()
        {
            var target = SimpleTs.GetTarget(SkillE.Range, SimpleTs.DamageType.Physical);
            if (target == null) return;
            if (SkillE.IsReady() && target.Health < (SkillE.GetDamage(target) + Player.GetAutoAttackDamage(target)))
            {
                SkillE.Cast(target, PacketCast);
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
        }

        private void AutoUltimate()
        {
            if (Utility.CountEnemysInRange(1000) == 0 || !SkillR.IsReady()) return;
            if ((Player.Health * 100 / Player.MaxHealth) <= Config.Item(Name + "autouseR").GetValue<Slider>().Value) SkillR.Cast();
        }

        private void NormalCombo()
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && (Player.Health * 100 / Player.MaxHealth) <= Config.Item(Name + "autoqusage").GetValue<Slider>().Value && Utility.CountEnemysInRange((int)SkillE.Range) >= 1) SkillQ.Cast();
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady() && targetObj.IsValidTarget(SkillW.Range))
            {
                if (Player.Path.Count() > 0 && Player.Path[0].Distance(targetObj.ServerPosition) < Player.Distance(targetObj))
                {
                    if (Player.Health < targetObj.Health) SkillW.Cast();
                }
                else SkillW.Cast();
            }
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && targetObj.IsValidTarget(SkillE.Range)) SkillE.Cast(targetObj, PacketCast);
            if (Config.Item(Name + "iusage").GetValue<bool>()) UseItem(targetObj);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillE.Range, MinionTypes.All, MinionTeam.NotAlly).OrderBy(i => i.Distance(Player)).FirstOrDefault();
            if (minionObj == null) return;
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady()) SkillE.Cast(minionObj, PacketCast);
        }

        private void CastIgnite(Obj_AI_Hero target)
        {
            if (IReady && target.IsValidTarget(IData.SData.CastRange[0]) && target.Health < Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite)) Player.SummonerSpellbook.CastSpell(IData.Slot, target);
        }

        private void UseItem(Obj_AI_Hero target)
        {
            if (BilgeReady && Player.Distance(target) <= 450) Items.UseItem(Bilge, target);
            if (BladeReady && Player.Distance(target) <= 450) Items.UseItem(Blade, target);
            if (TiamatReady && Utility.CountEnemysInRange(350) >= 1) Items.UseItem(Tiamat);
            if (HydraReady && (Utility.CountEnemysInRange(350) >= 2 || (Player.GetAutoAttackDamage(target) < target.Health && Utility.CountEnemysInRange(350) == 1))) Items.UseItem(Hydra);
            if (RandReady && Utility.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
        }
    }
}