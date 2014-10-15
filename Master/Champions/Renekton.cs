using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Master
{
    class Renekton : Program
    {
        private const String Version = "1.0.0";
        private Spell SkillQ, SkillW, SkillE, SkillR;
        private SpellDataInst QData, WData, EData, RData, IData;
        private Boolean IReady = false;
        private Int32 Tiamat = 3077, Hydra = 3074, Rand = 3143;
        private Boolean TiamatReady = false, HydraReady = false, RandReady = false;
        private Vector3 DashBackPos = default(Vector3);

        public Renekton()
        {
            QData = Player.Spellbook.GetSpell(SpellSlot.Q);
            WData = Player.Spellbook.GetSpell(SpellSlot.W);
            EData = Player.Spellbook.GetSpell(SpellSlot.E);
            RData = Player.Spellbook.GetSpell(SpellSlot.R);
            IData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonerdot"));
            SkillQ = new Spell(QData.Slot, QData.SData.CastRange[0]);
            SkillW = new Spell(WData.Slot, WData.SData.CastRange[0]);
            SkillE = new Spell(EData.Slot, 480);
            SkillR = new Spell(RData.Slot, RData.SData.CastRange[0]);
            SkillE.SetSkillshot(-EData.SData.SpellCastTime, EData.SData.LineWidth, EData.SData.MissileSpeed, false, SkillshotType.SkillshotLine);

            Config.AddSubMenu(new Menu("Combo Settings", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "Use W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "Use E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Harass Settings", "hsettings"));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "harMode", "Use Harass If Hp Above").SetValue(new Slider(20, 1)));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "useHarrQ", "Use Q").SetValue(true));
            Config.SubMenu("hsettings").AddItem(new MenuItem(Name + "useHarrW", "Use W").SetValue(true));

            Config.AddSubMenu(new Menu("Misc Settings", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useAntiW", "Use W To Anti Gap Closer").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useInterW", "Use W To Interrupt").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "calcelW", "Cancel W Animation").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "skin", "Use Custom Skin").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "skin1", "Skin Changer").SetValue(new Slider(6, 1, 7)));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "packetCast", "Use Packet To Cast").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate Settings", "useUlt"));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "useR", "Auto Use R").SetValue(true));
            Config.SubMenu("useUlt").AddItem(new MenuItem(Name + "autouseR", "Use R If Hp Under").SetValue(new Slider(20, 1)));

            Config.AddSubMenu(new Menu("Lane/Jungle Clear Settings", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearQ", "Use Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearW", "Use W").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearE", "Use E").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearI", "Use Tiamat/Hydra Item").SetValue(true));

            Config.AddSubMenu(new Menu("Draw Settings", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawQ", "Q Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem(Name + "DrawE", "E Range").SetValue(true));

            if (Config.Item(Name + "skin").GetValue<bool>())
            {
                Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, Config.Item(Name + "skin1").GetValue<Slider>().Value, Name)).Process();
                lastSkinId = Config.Item(Name + "skin1").GetValue<Slider>().Value;
            }
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Game.PrintChat("<font color = \"#33CCCC\">Master of {0}</font> <font color = \"#fff8e7\">Brian v{1}</font>", Name, Version);
        }

        private void OnGameUpdate(EventArgs args)
        {
            IReady = (IData != null && IData.Slot != SpellSlot.Unknown && IData.State == SpellState.Ready);
            TiamatReady = Items.CanUseItem(Tiamat);
            HydraReady = Items.CanUseItem(Hydra);
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
            }
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
            if (Config.Item(Name + "DrawQ").GetValue<bool>() && SkillQ.Level > 0) Utility.DrawCircle(Player.Position, SkillQ.Range, SkillQ.IsReady() ? Color.Green : Color.Red);
            if (Config.Item(Name + "DrawE").GetValue<bool>() && SkillE.Level > 0) Utility.DrawCircle(Player.Position, SkillE.Range, SkillE.IsReady() ? Color.Green : Color.Red);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item(Name + "useAntiW").GetValue<bool>()) return;
            if (gapcloser.Sender.IsValidTarget(SkillE.Range))
            {
                if (SkillW.IsReady()) SkillW.Cast();
                if (Player.HasBuff("RenektonPreExecute")) Player.IssueOrder(GameObjectOrder.AttackUnit, gapcloser.Sender);
            }
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item(Name + "useInterW").GetValue<bool>()) return;
            if (SkillW.IsReady() && SkillE.IsReady() && Player.Distance(unit) > SkillW.Range && unit.IsValidTarget(SkillE.Range)) SkillE.Cast(unit, PacketCast);
            if (unit.IsValidTarget(SkillW.Range))
            {
                if (SkillW.IsReady()) SkillW.Cast();
                if (Player.HasBuff("RenektonPreExecute")) Player.IssueOrder(GameObjectOrder.AttackUnit, unit);
            }
        }

        private void CancelW()
        {
            if (Player.HasBuff("RenektonPreExecute") || !targetObj.IsValidTarget(SkillW.Range)) return;
            if (TiamatReady) Items.UseItem(Tiamat);
            if (HydraReady) Items.UseItem(Hydra);
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (Config.Item(Name + "calcelW").GetValue<bool>() && args.SData.Name == "RenektonExecute") Utility.DelayAction.Add(1, delegate { CancelW(); });
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && args.SData.Name == "RenektonSliceAndDice" && DashBackPos == default(Vector3)) DashBackPos = Player.Position;
        }

        private void AutoUltimate()
        {
            if (Utility.CountEnemysInRange(1000) == 0 || !SkillR.IsReady()) return;
            if ((Player.Health * 100 / Player.MaxHealth) <= Config.Item(Name + "autouseR").GetValue<Slider>().Value) SkillR.Cast();
        }

        private void Harass()
        {
            if (targetObj == null || (Player.Health * 100 / Player.MaxHealth) < Config.Item(Name + "harMode").GetValue<Slider>().Value) return;
            if (SkillE.IsReady())
            {
                if (EData.Name == "RenektonSliceAndDice")
                {
                    SkillE.Cast(targetObj, PacketCast);
                }
                else if (!Player.IsDashing())
                {
                    if (Config.Item(Name + "useHarrW").GetValue<bool>() && targetObj.IsValidTarget(SkillW.Range))
                    {
                        if (SkillW.IsReady()) SkillW.Cast();
                        if (Player.HasBuff("RenektonPreExecute")) Player.IssueOrder(GameObjectOrder.AttackUnit, targetObj);
                    }
                    if (Config.Item(Name + "useHarrQ").GetValue<bool>() && SkillQ.IsReady() && targetObj.IsValidTarget(SkillQ.Range)) SkillQ.Cast();
                    SkillE.Cast(DashBackPos, PacketCast);
                }
            }
            else DashBackPos = default(Vector3);
        }

        private void NormalCombo()
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && EData.Name == "RenektonSliceAndDice") SkillE.Cast(targetObj, PacketCast);
            if (Config.Item(Name + "wusage").GetValue<bool>() && targetObj.IsValidTarget(SkillW.Range))
            {
                if (SkillW.IsReady()) SkillW.Cast();
                if (Player.HasBuff("RenektonPreExecute")) Player.IssueOrder(GameObjectOrder.AttackUnit, targetObj);
            }
            if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady() && targetObj.IsValidTarget(SkillQ.Range)) SkillQ.Cast();
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && EData.Name != "RenektonSliceAndDice") SkillE.Cast(targetObj, PacketCast);
            if (Config.Item(Name + "iusage").GetValue<bool>()) UseItem(targetObj);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillE.Range, MinionTypes.All, MinionTeam.NotAlly).OrderBy(i => i.Distance(Player)).FirstOrDefault();
            if (minionObj == null) return;
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady() && EData.Name == "RenektonSliceAndDice") SkillE.Cast(minionObj, PacketCast);
            if (Config.Item(Name + "useClearQ").GetValue<bool>() && SkillQ.IsReady() && minionObj.IsValidTarget(SkillQ.Range)) SkillQ.Cast();
            if (Config.Item(Name + "useClearW").GetValue<bool>() && minionObj.IsValidTarget(SkillW.Range) && SkillW.IsKillable(minionObj))
            {
                if (SkillW.IsReady()) SkillW.Cast();
                if (Player.HasBuff("RenektonPreExecute")) Player.IssueOrder(GameObjectOrder.AttackUnit, minionObj);
            }
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady() && EData.Name != "RenektonSliceAndDice") SkillE.Cast(minionObj, PacketCast);
            if (Config.Item(Name + "useClearI").GetValue<bool>() && Player.Distance(minionObj) <= 350)
            {
                if (TiamatReady) Items.UseItem(Tiamat);
                if (HydraReady) Items.UseItem(Hydra);
            }
        }

        private void CastIgnite(Obj_AI_Hero target)
        {
            if (IReady && target.IsValidTarget(IData.SData.CastRange[0]) && target.Health < Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite)) Player.SummonerSpellbook.CastSpell(IData.Slot, target);
        }

        private void UseItem(Obj_AI_Hero target)
        {
            if (TiamatReady && Utility.CountEnemysInRange(350) >= 1) Items.UseItem(Tiamat);
            if (HydraReady && (Utility.CountEnemysInRange(350) >= 2 || (Player.GetAutoAttackDamage(target) < target.Health && Utility.CountEnemysInRange(350) == 1))) Items.UseItem(Hydra);
            if (RandReady && Utility.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
        }
    }
}