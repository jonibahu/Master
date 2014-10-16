using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Master
{
    class Nasus : Program
    {
        private const String Version = "1.0.1";
        private Int32 Sheen = 3057, Iceborn = 3025;

        public Nasus()
        {
            SkillQ = new Spell(SpellSlot.Q, 300);
            SkillW = new Spell(SpellSlot.W, 600);
            SkillE = new Spell(SpellSlot.E, 650);
            SkillR = new Spell(SpellSlot.R, 20);

            Config.AddSubMenu(new Menu("Combo/Harass Settings", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "Use W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "Use E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Misc Settings", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "killstealE", "Auto E To Kill Steal").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "skin", "Use Custom Skin").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "skin1", "Skin Changer").SetValue(new Slider(5, 1, 6)));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "Use Packet To Cast").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate Settings", "useUlt"));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "useR", "Auto Use R").SetValue(true));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "autouseR", "Use R If Hp Under").SetValue(new Slider(30, 1)));

            Config.AddSubMenu(new Menu("Lane/Jungle Clear Settings", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearQ", "Use Q").SetValue(true));
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

        private double GetBonusDmg(Obj_AI_Base target)
        {
            double DmgItem = 0;
            if (Items.HasItem(Sheen) && (Items.CanUseItem(Sheen) || Player.HasBuff("sheen", true)) && Player.BaseAttackDamage * 1 > DmgItem) DmgItem = Player.BaseAttackDamage * 1;
            if (Items.HasItem(Iceborn) && (Items.CanUseItem(Iceborn) || Player.HasBuff("itemfrozenfist", true)) && Player.BaseAttackDamage * 1.25 > DmgItem) DmgItem = Player.BaseAttackDamage * 1.25;
            return SkillQ.GetDamage(target) + Player.CalcDamage(target, Damage.DamageType.Physical, DmgItem);
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
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                NormalCombo();
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneJungClear();
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit) LastHit();
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
            var target = SimpleTs.GetTarget(SkillE.Range, SimpleTs.DamageType.Magical);
            if (target == null) return;
            if (SkillE.IsReady() && SkillE.IsKillable(target, 1)) SkillE.Cast(target.Position, PacketCast);
        }

        private void AutoUltimate()
        {
            if (Utility.CountEnemysInRange(1000) == 0 || !SkillR.IsReady()) return;
            if ((Player.Health * 100 / Player.MaxHealth) <= Config.Item(Name + "autouseR").GetValue<Slider>().Value) SkillR.Cast();
        }

        private void NormalCombo()
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady() && targetObj.IsValidTarget(SkillW.Range)) SkillW.Cast(targetObj, PacketCast);
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && targetObj.IsValidTarget(SkillE.Range)) SkillE.Cast(targetObj.Position, PacketCast);
            if (Config.Item(Name + "qusage").GetValue<bool>() && targetObj.IsValidTarget(SkillQ.Range))
            {
                var HitWhenQCd = Math.Floor(SkillQ.Instance.Cooldown / (1 / Player.AttackSpeedMod));
                var DmgWhenQCd = HitWhenQCd * Player.GetAutoAttackDamage(targetObj);
                if ((targetObj.Health < GetBonusDmg(targetObj) || targetObj.Health > DmgWhenQCd + GetBonusDmg(targetObj)) && (SkillQ.IsReady() || Player.HasBuff("NasusQ", true)))
                {
                    if (!Player.HasBuff("NasusQ", true)) SkillQ.Cast();
                    Player.IssueOrder(GameObjectOrder.AttackUnit, targetObj);
                }
                else if (Orbwalking.CanAttack() && targetObj.Health > Player.GetAutoAttackDamage(targetObj)) Player.IssueOrder(GameObjectOrder.AttackUnit, targetObj);
            }
            if (Config.Item(Name + "iusage").GetValue<bool>() && Items.CanUseItem(Rand) && Utility.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillE.Range, MinionTypes.All, MinionTeam.NotAlly).OrderBy(i => i.Distance(Player)).FirstOrDefault();
            if (minionObj == null) return;
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady()) SkillE.Cast(minionObj.Position, PacketCast);
            if (Config.Item(Name + "useClearQ").GetValue<bool>() && minionObj.IsValidTarget(SkillQ.Range))
            {
                var HitWhenQCd = Math.Floor(SkillQ.Instance.Cooldown / (1 / Player.AttackSpeedMod));
                var DmgWhenQCd = HitWhenQCd * Player.GetAutoAttackDamage(minionObj);
                if ((minionObj.Health < GetBonusDmg(minionObj) || minionObj.Health > DmgWhenQCd + GetBonusDmg(minionObj)) && (SkillQ.IsReady() || Player.HasBuff("NasusQ", true)))
                {
                    if (!Player.HasBuff("NasusQ", true)) SkillQ.Cast();
                    Player.IssueOrder(GameObjectOrder.AttackUnit, minionObj);
                }
                else if (Orbwalking.CanAttack() && minionObj.Health > Player.GetAutoAttackDamage(minionObj)) Player.IssueOrder(GameObjectOrder.AttackUnit, minionObj);
            }
        }

        private void LastHit()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillE.Range, MinionTypes.All, MinionTeam.NotAlly).OrderBy(i => i.Distance(Player)).FirstOrDefault();
            if (minionObj == null) return;
            if (minionObj.Health < GetBonusDmg(minionObj) && minionObj.IsValidTarget(SkillQ.Range))
            {
                if (SkillQ.IsReady() && !Player.HasBuff("NasusQ", true))
                {
                    SkillQ.Cast();
                    Player.IssueOrder(GameObjectOrder.AttackUnit, minionObj);
                }
                if (Player.HasBuff("NasusQ", true)) Player.IssueOrder(GameObjectOrder.AttackUnit, minionObj);
            }
        }

        private void CastIgnite(Obj_AI_Hero target)
        {
            if (IReady && target.IsValidTarget(IData.SData.CastRange[0]) && target.Health < Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite)) Player.SummonerSpellbook.CastSpell(IData.Slot, target);
        }
    }
}