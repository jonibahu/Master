using System;
using System.Linq;
using System.Reflection;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace MasterOfLeeSin
{
    class LeeSin
    {
        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;
        public static Obj_AI_Base allyObj = null;
        public static Obj_AI_Hero Player = ObjectManager.Player, targetObj = null;
        public static Spell SkillQ, SkillW, SkillE, SkillR;
        public static SpellDataInst QData, WData, EData, RData, FData, SData, IData;
        public static Boolean QReady = false, WReady = false, EReady = false, RReady = false, FReady = false, SReady = false, IReady = false;
        public static Int32 Tiamat = 3077, Hydra = 3074, Blade = 3153, Bilge = 3144, Rand = 3143;
        public static Boolean TiamatReady = false, HydraReady = false, BladeReady = false, BilgeReady = false, RandReady = false;
        public static InventorySlot Ward = null;
        public static float lastTimeWard = 0, lastTimeJump = 0;
        public static Int32 lastSkinId = 0;
        public static Boolean PacketCast = false;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        static void OnLoad(EventArgs args)
        {
            if (Player.ChampionName != "LeeSin") return;
            QData = Player.Spellbook.GetSpell(SpellSlot.Q);
            WData = Player.Spellbook.GetSpell(SpellSlot.W);
            EData = Player.Spellbook.GetSpell(SpellSlot.E);
            RData = Player.Spellbook.GetSpell(SpellSlot.R);
            FData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonerflash"));
            SData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonersmite"));
            IData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonerdot"));
            SkillQ = new Spell(QData.Slot, 1100);//1300
            SkillW = new Spell(WData.Slot, 700);
            SkillE = new Spell(EData.Slot, 350);//500
            SkillR = new Spell(RData.Slot, 375);
            SkillQ.SetSkillshot(-QData.SData.SpellCastTime, QData.SData.LineWidth, QData.SData.MissileSpeed, true, SkillshotType.SkillshotLine);

            Config = new Menu("Master Of LeeSin", "LeeSinCombo", true);
            var tsMenu = new Menu("Target Selector", "TSSettings");
            SimpleTs.AddToMenu(tsMenu);
            Config.AddSubMenu(tsMenu);

            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));
            Config.Item("Orbwalk").DisplayName = "Normal Combo";
            Config.Item("Farm").DisplayName = "Harrass";
            Config.Item("LaneClear").DisplayName = "Lane/Jungle Clear";

            Config.AddSubMenu(new Menu("Key Bindings", "KeyBindings"));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("starActive", "Star Combo").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("insecMake", "Insec").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("wardJump", "Ward Jump").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("ksbrdr", "Kill Steal Baron/Dragon").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Combo Settings", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem("pusage", "Use Passive").SetValue(false));
            Config.SubMenu("csettings").AddItem(new MenuItem("qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("wusage", "Use W").SetValue(false));
            Config.SubMenu("csettings").AddItem(new MenuItem("autowusage", "Use W If Hp Under").SetValue(new Slider(20, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem("eusage", "Use E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("rusage", "Use R To Finish").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Insec Settings", "insettings"));
            //string[] allylist = ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsAlly && !i.IsMe).Select(i => i.ChampionName).ToArray();
            //Config.SubMenu("insettings").AddItem(new MenuItem("insecally", "To:").SetValue(new StringList(allylist))).DontSave();
            Config.SubMenu("insettings").AddItem(new MenuItem("insectower", "To Tower If No Ally In").SetValue(new Slider(1100, 500, 1500)));
            Config.SubMenu("insettings").AddItem(new MenuItem("wflash", "Flash If Ward Jump Not Ready").SetValue(true));
            Config.SubMenu("insettings").AddItem(new MenuItem("pflash", "Prioritize Flash To Insec").SetValue(false));

            Config.AddSubMenu(new Menu("Harrass Settings", "hsettings"));
            Config.SubMenu("hsettings").AddItem(new MenuItem("harrMode", "Use Harrass If Hp Above").SetValue(new Slider(20, 1)));
            Config.SubMenu("hsettings").AddItem(new MenuItem("useHarrE", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc Settings", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem("smite", "Auto Smite Collision Minion").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem("skin", "Use Custom Skin").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem("skin1", "Skin Changer").SetValue(new Slider(4, 1, 7)));
            Config.SubMenu("miscs").AddItem(new MenuItem("packetCast", "Use Packet To Cast").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate Settings", "useUlt"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsEnemy))
            {
                Config.SubMenu("useUlt").AddItem(new MenuItem("ult" + enemy.ChampionName, "Use Ultimate On " + enemy.ChampionName).SetValue(true));
            }

            Config.AddSubMenu(new Menu("Lane/Jungle Clear Settings", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearQ", "Use Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearW", "Use W").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearE", "Use E").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearI", "Use Tiamat/Hydra Item").SetValue(true));

            Config.AddSubMenu(new Menu("Draw Settings", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("drawInsec", "Insec Line").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("drawInsecTower", "Insec To Tower Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("drawKillable", "Killable Text").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawQ", "Q Range").SetValue(false));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawW", "W Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawE", "E Range").SetValue(false));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawR", "R Range").SetValue(false));

            Config.AddToMainMenu();
            if (Config.Item("skin").GetValue<bool>())
            {
                Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, Config.Item("skin1").GetValue<Slider>().Value, Player.ChampionName)).Process();
                lastSkinId = Config.Item("skin1").GetValue<Slider>().Value;
            }
            Game.OnGameUpdate += OnTick;
            Drawing.OnDraw += OnDraw;
            Game.PrintChat("<font color = \"#33CCCC\">Master of LeeSin</font> <font color = \"#fff8e7\">Brian v" + Assembly.GetExecutingAssembly().GetName().Version + "</font>");
        }

        static void Orbwalk(Obj_AI_Base target = null)
        {
            Orbwalking.Orbwalk((target == null) ? SimpleTs.GetTarget(-1, SimpleTs.DamageType.Physical) : target, Game.CursorPos, Config.Item("ExtraWindup").GetValue<Slider>().Value, Config.Item("HoldPosRadius").GetValue<Slider>().Value);
        }

        static InventorySlot GetWardSlot()
        {
            Int32[] wardIds = { 3340, 3361, 3205, 3207, 3154, 3160, 2049, 2045, 2050, 2044 };
            InventorySlot ward = null;
            foreach (var wardId in wardIds)
            {
                ward = Player.InventoryItems.FirstOrDefault(i => i.Id == (ItemId)wardId);
                if (ward != null && Player.Spellbook.Spells.First(i => (Int32)i.Slot == ward.Slot + 4).State == SpellState.Ready) return ward;
            }
            return ward;
        }

        static Obj_AI_Base GetInsecAlly()
        {
            //Obj_AI_Base allyInsecObj = null, towerInsecObj = null;
            //foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(Config.Item("insectower").GetValue<Slider>().Value, false) && !i.IsMe))
            //{
            //    if (allyInsecObj == null || Player.Distance(ally) < Player.Distance(allyInsecObj)) allyInsecObj = ally;
            //    if (ally.ChampionName == Config.Item("insecally").GetValue<StringList>().SList[Config.Item("insecally").GetValue<StringList>().SelectedIndex])
            //    {
            //        if (Player.Distance(ally) < Player.Distance(allyInsecObj)) allyInsecObj = ally;
            //    }
            //}
            //if (allyInsecObj == null)
            //{
            //    foreach (var turret in ObjectManager.Get<Obj_AI_Turret>().Where(i => i != null && i.IsValid && i.IsAlly && !i.IsDead))
            //    {
            //        if (towerInsecObj == null || Player.Distance(turret) < Player.Distance(towerInsecObj)) towerInsecObj = turret;
            //    }
            //}
            //return (allyInsecObj != null) ? allyInsecObj : towerInsecObj;

            Obj_AI_Base nearObj = null;
            if (ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(i => i.IsAlly && !i.IsDead && !i.IsMe && i.Distance(Player) < Config.Item("insectower").GetValue<Slider>().Value) != null)
            {
                nearObj = ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsAlly && !i.IsDead && !i.IsMe).OrderBy(i => i.Distance(Player)).First();
            }
            else
            {
                nearObj = ObjectManager.Get<Obj_AI_Turret>().Where(i => i.IsAlly && !i.IsDead).OrderBy(i => i.Distance(Player)).First();
            }
            return nearObj;
        }

        static void OnTick(EventArgs args)
        {
            FReady = (FData != null && FData.Slot != SpellSlot.Unknown && FData.State == SpellState.Ready);
            SReady = (SData != null && SData.Slot != SpellSlot.Unknown && SData.State == SpellState.Ready);
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
            PacketCast = Config.Item("packetCast").GetValue<bool>();
            Ward = GetWardSlot();
            //targetObj = GetInsecTarget();
            allyObj = GetInsecAlly();
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
            if (Config.Item("insecMake").GetValue<KeyBind>().Active) InsecCombo();
            if (Config.Item("starActive").GetValue<KeyBind>().Active) StarCombo();
            if (Config.Item("wardJump").GetValue<KeyBind>().Active) WardJump(Game.CursorPos);
            if (Config.Item("ksbrdr").GetValue<KeyBind>().Active) KillStealBrDr();
            if (Config.Item("skin").GetValue<bool>() && Config.Item("skin1").GetValue<Slider>().Value != lastSkinId)
            {
                Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, Config.Item("skin1").GetValue<Slider>().Value, Player.ChampionName)).Process();
                lastSkinId = Config.Item("skin1").GetValue<Slider>().Value;
            }
        }

        static void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item("DrawQ").GetValue<bool>() && SkillQ.Level > 0) Utility.DrawCircle(Player.Position, (QData.Name == "BlindMonkQOne") ? SkillQ.Range : 1300, SkillQ.IsReady() ? Color.Green : Color.Red);
            if (Config.Item("DrawW").GetValue<bool>() && SkillW.Level > 0) Utility.DrawCircle(Player.Position, SkillW.Range, SkillW.IsReady() ? Color.Green : Color.Red);
            if (Config.Item("DrawE").GetValue<bool>() && SkillE.Level > 0) Utility.DrawCircle(Player.Position, (EData.Name == "BlindMonkEOne") ? SkillE.Range : 500, SkillE.IsReady() ? Color.Green : Color.Red);
            if (Config.Item("DrawR").GetValue<bool>() && SkillR.Level > 0) Utility.DrawCircle(Player.Position, SkillR.Range, SkillR.IsReady() ? Color.Green : Color.Red);
            if (Config.Item("drawInsec").GetValue<bool>() && SkillR.IsReady())
            {
                Byte validTargets = 0;
                if (targetObj != null)
                {
                    Utility.DrawCircle(targetObj.Position, 70, Color.FromArgb(0, 204, 0));
                    validTargets += 1;
                }
                if (allyObj != null)
                {
                    Utility.DrawCircle(allyObj.Position, 70, Color.FromArgb(0, 204, 0));
                    validTargets += 1;
                }
                if (validTargets == 2)
                {
                    var posDraw = targetObj.Position + Vector3.Normalize(allyObj.Position - targetObj.Position) * 600;
                    Drawing.DrawLine(Drawing.WorldToScreen(targetObj.Position), Drawing.WorldToScreen(posDraw), 2, Color.White);
                }
            }
            if (Config.Item("drawInsecTower").GetValue<bool>() && SkillR.IsReady()) Utility.DrawCircle(Player.Position, Config.Item("insectower").GetValue<Slider>().Value, Color.Purple);
            if (Config.Item("drawKillable").GetValue<bool>())
            {
                foreach (var killableObj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget()))
                {
                    var dmgTotal = Player.GetAutoAttackDamage(killableObj);
                    if (SkillQ.IsReady() && QData.Name == "BlindMonkQOne") dmgTotal += SkillQ.GetDamage(killableObj);
                    if (SkillR.IsReady() && Config.Item("ult" + killableObj.ChampionName).GetValue<bool>()) dmgTotal += SkillR.GetDamage(killableObj);
                    if (SkillE.IsReady() && EData.Name == "BlindMonkEOne") dmgTotal += SkillE.GetDamage(killableObj);
                    if (SkillQ.IsReady() && (killableObj.HasBuff("BlindMonkQOne", true) || killableObj.HasBuff("blindmonkqonechaos", true))) dmgTotal += GetQ2Dmg(killableObj, dmgTotal);
                    if (killableObj.Health < dmgTotal)
                    {
                        var posText = Drawing.WorldToScreen(killableObj.Position);
                        Drawing.DrawText(posText.X - 30, posText.Y - 5, Color.White, "Killable");
                    }
                }
            }
        }

        static void KillStealBrDr()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, 1500, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault(i => i.Name == "Worm12.1.1" || i.Name == "Dragon6.1.1");
            Orbwalk(minionObj);
            if (minionObj == null) return;
            if (SkillQ.IsReady() && !SReady && minionObj.Health < GetQ2Dmg(minionObj, SkillQ.GetDamage(minionObj)))
            {
                if (QData.Name == "BlindMonkQOne")
                {
                    SkillQ.Cast(minionObj, PacketCast);
                }
                else if ((minionObj.HasBuff("BlindMonkQOne", true) || minionObj.HasBuff("blindmonkqonechaos", true)) && minionObj.IsValidTarget(1300)) SkillQ.Cast();
            }
            if (SkillQ.IsReady() && SReady && minionObj.Health < GetQ2Dmg(minionObj, SkillQ.GetDamage(minionObj) + Player.GetSummonerSpellDamage(minionObj, Damage.SummonerSpell.Smite)))
            {
                if (QData.Name == "BlindMonkQOne")
                {
                    SkillQ.Cast(minionObj, PacketCast);
                }
                else if ((minionObj.HasBuff("BlindMonkQOne", true) || minionObj.HasBuff("blindmonkqonechaos", true)) && minionObj.IsValidTarget(1300))
                {
                    SkillQ.Cast();
                    Player.SummonerSpellbook.CastSpell(SData.Slot, minionObj);
                }
            }
            if (SReady && minionObj.IsValidTarget(SData.SData.CastRange[0]) && minionObj.Health < Player.GetSummonerSpellDamage(minionObj, Damage.SummonerSpell.Smite)) Player.SummonerSpellbook.CastSpell(SData.Slot, minionObj);
        }

        static float GetHitBox(Obj_AI_Base minion)
        {
            var nameMinion = minion.Name.ToLower();
            if (nameMinion.Contains("mech")) return 65;
            if (nameMinion.Contains("wizard") || nameMinion.Contains("basic")) return 48;
            if (nameMinion.Contains("wolf") || nameMinion.Contains("wraith")) return 50;
            if (nameMinion.Contains("golem") || nameMinion.Contains("lizard")) return 80;
            if (nameMinion.Contains("dragon") || nameMinion.Contains("worm")) return 100;
            return 50;
        }

        static bool CheckingCollision(Obj_AI_Hero target)
        {
            foreach (var col in MinionManager.GetMinions(Player.Position, 1500, MinionTypes.All, MinionTeam.NotAlly))
            {
                var Segment = Geometry.ProjectOn(col.ServerPosition.To2D(), Player.ServerPosition.To2D(), col.Position.To2D());
                if (Segment.IsOnSegment && target.ServerPosition.To2D().Distance(Segment.SegmentPoint) <= GetHitBox(col) + 70)
                {
                    if (col.IsValidTarget(SData.SData.CastRange[0]) && col.Health < Player.GetSummonerSpellDamage(col, Damage.SummonerSpell.Smite))
                    {
                        Player.SummonerSpellbook.CastSpell(SData.Slot, col);
                        return true;
                    }
                }
            }
            return false;
        }

        static void Harass()
        {
            if (targetObj == null) return;
            var jumpObj = ObjectManager.Get<Obj_AI_Base>().Where(i => !i.IsMe && !(i is Obj_AI_Turret) && i.Distance(Player.ServerPosition) <= SkillW.Range).OrderBy(i => i.Distance(ObjectManager.Get<Obj_AI_Turret>().Where(a => a.IsAlly).OrderBy(a => a.Distance(Player.ServerPosition)).First().ServerPosition)).FirstOrDefault();
            if (SkillQ.IsReady())
            {
                if (QData.Name == "BlindMonkQOne")
                {
                    if (Config.Item("smite").GetValue<bool>() && SReady && SkillQ.GetPrediction(targetObj).CollisionObjects.Count == 1) CheckingCollision(targetObj);
                    SkillQ.Cast(targetObj, PacketCast);
                }
                else if ((targetObj.HasBuff("BlindMonkQOne", true) || targetObj.HasBuff("blindmonkqonechaos", true)) && targetObj.IsValidTarget(1300) && SkillW.IsReady() && WData.Name == "BlindMonkWOne" && Player.Mana >= (Config.Item("useHarrE").GetValue<bool>() ? 130 : 80) && (Player.Health * 100 / Player.MaxHealth) >= Config.Item("harrMode").GetValue<Slider>().Value)
                {
                    if (jumpObj != null) SkillQ.Cast();
                }
            }
            if (!SkillQ.IsReady() && Config.Item("useHarrE").GetValue<bool>() && SkillE.IsReady() && EData.Name == "BlindMonkEOne" && targetObj.IsValidTarget(SkillE.Range)) SkillE.Cast();
            if (!SkillQ.IsReady() && SkillW.IsReady() && WData.Name == "BlindMonkWOne" && ((Config.Item("useHarrE").GetValue<bool>() && targetObj.HasBuff("BlindMonkEOne", true)) || (!Config.Item("useHarrE").GetValue<bool>() && Utility.CountEnemysInRange(200) >= 1)))
            {
                if ((Environment.TickCount - lastTimeJump) > 200)
                {
                    SkillW.Cast(jumpObj, PacketCast);
                    lastTimeJump = Environment.TickCount;
                }
            }
        }

        static void WardJump(Vector3 Pos)
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Pos);
            if ((SkillW.IsReady() && WData.Name != "BlindMonkWOne") || !SkillW.IsReady()) return;
            bool Jumped = false;
            if (Player.Distance(Pos) > SkillW.Range) Pos = Player.Position + Vector3.Normalize(Pos - Player.Position) * 600;
            foreach (var jumpObj in ObjectManager.Get<Obj_AI_Base>().Where(i => !i.IsMe && i.IsAlly && !(i is Obj_AI_Turret) && i.Distance(Pos) <= 200))
            {
                Jumped = true;
                if (Player.Distance(jumpObj) <= SkillW.Range + jumpObj.BoundingRadius)
                {
                    if ((Environment.TickCount - lastTimeJump) > 200)
                    {
                        SkillW.Cast(jumpObj, PacketCast);
                        lastTimeJump = Environment.TickCount;
                    }
                }
            }
            if (!Jumped && Ward != null)
            {
                if ((Environment.TickCount - lastTimeWard) > 200)
                {
                    Ward.UseItem(Pos);
                    lastTimeWard = Environment.TickCount;
                }
            }
        }

        static void InsecCombo()
        {
            Orbwalk();
            if (targetObj == null || allyObj == null) return;
            if (!Config.Item("pflash").GetValue<bool>() && WardJumpInsec()) return;
            if (!Config.Item("pflash").GetValue<bool>() && Config.Item("wflash").GetValue<bool>() && FlashInsec()) return;
            if (Config.Item("pflash").GetValue<bool>() && FlashInsec()) return;
            if (Config.Item("pflash").GetValue<bool>() && !FReady && WardJumpInsec()) return;
            if (SkillQ.IsReady())
            {
                if (QData.Name == "BlindMonkQOne")
                {
                    if (Config.Item("smite").GetValue<bool>() && SReady) CheckingCollision(targetObj);
                    SkillQ.Cast(targetObj, PacketCast);
                }
                else
                {
                    if ((targetObj.HasBuff("BlindMonkQOne", true) || targetObj.HasBuff("blindmonkqonechaos", true)) && targetObj.IsValidTarget(1300))
                    {
                        if (Player.Distance(targetObj) > 600 || targetObj.Health < SkillQ.GetDamage(targetObj, 1) || (Environment.TickCount - SkillQ.LastCastAttemptT) > 1800) SkillQ.Cast();
                    }
                    else
                    {
                        var enemyObj = ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(i => i.IsValidTarget(1300) && i.Distance(targetObj) < 590 && (i.HasBuff("BlindMonkQOne", true) || i.HasBuff("blindmonkqonechaos", true)));
                        if (enemyObj != null && (Player.Distance(enemyObj) > 600 || (Environment.TickCount - SkillQ.LastCastAttemptT) > 1800)) SkillQ.Cast();
                    }
                }
            }
        }

        static bool WardJumpInsec()
        {
            if (SkillR.IsReady())
            {
                if (targetObj.IsValidTarget(SkillR.Range))
                {
                    var pos = Player.Position + Vector3.Normalize(targetObj.Position - Player.Position) * (targetObj.Distance(Player) + 500);
                    var newDistance = allyObj.Distance(targetObj) - allyObj.Distance(pos);
                    if (newDistance > 0 && (newDistance / 500) > 0.7)
                    {
                        SkillR.Cast(targetObj, PacketCast);
                        return true;
                    }
                }
                if (SkillW.IsReady() && WData.Name == "BlindMonkWOne")
                {
                    var insecObj2 = Prediction.GetPrediction(targetObj, 0.25f, 2000).UnitPosition;
                    var pos = allyObj.Position + Vector3.Normalize(insecObj2 - allyObj.Position) * (insecObj2.Distance(allyObj.Position) + 300);
                    if (Player.Distance(pos) < 600)
                    {
                        WardJump(pos);
                        return true;
                    }
                }
            }
            return false;
        }

        static bool FlashInsec()
        {
            if (SkillR.IsReady())
            {
                if (targetObj.IsValidTarget(SkillR.Range))
                {
                    var pos = Player.Position + Vector3.Normalize(targetObj.Position - Player.Position) * (targetObj.Distance(Player) + 500);
                    var newDistance = allyObj.Distance(targetObj) - allyObj.Distance(pos);
                    if (newDistance > 0 && (newDistance / 500) > 0.7)
                    {
                        SkillR.Cast(targetObj, PacketCast);
                        return true;
                    }
                }
                if (FReady)
                {
                    var insecObj2 = Prediction.GetPrediction(targetObj, 0.25f, 2000).UnitPosition;
                    var pos = allyObj.Position + Vector3.Normalize(insecObj2 - allyObj.Position) * (insecObj2.Distance(allyObj.Position) + 400);
                    if (Player.Distance(pos) < 600)
                    {
                        Player.SummonerSpellbook.CastSpell(FData.Slot, pos);
                        return true;
                    }
                }
            }
            return false;
        }

        static void NormalCombo()
        {
            if (targetObj == null) return;
            if (Config.Item("pusage").GetValue<bool>() && Player.HasBuff("blindmonkpassive_cosmetic", true) && Orbwalking.InAutoAttackRange(targetObj)) return;
            if (Config.Item("eusage").GetValue<bool>() && SkillE.IsReady() && EData.Name == "BlindMonkEOne" && targetObj.IsValidTarget(SkillE.Range)) SkillE.Cast();
            if (Config.Item("qusage").GetValue<bool>() && SkillQ.IsReady() && QData.Name == "BlindMonkQOne")
            {
                if (Config.Item("smite").GetValue<bool>() && SReady && SkillQ.GetPrediction(targetObj).CollisionObjects.Count == 1) CheckingCollision(targetObj);
                SkillQ.Cast(targetObj, PacketCast);
            }
            if (Config.Item("qusage").GetValue<bool>() && SkillQ.IsReady() && targetObj.IsValidTarget(1300) && (targetObj.HasBuff("BlindMonkQOne", true) || targetObj.HasBuff("blindmonkqonechaos", true)))
            {
                if (Player.Distance(targetObj) > 500 || targetObj.Health < SkillQ.GetDamage(targetObj, 1) || (targetObj.HasBuff("BlindMonkEOne", true) && targetObj.IsValidTarget(SkillE.Range)) || (Environment.TickCount - SkillQ.LastCastAttemptT) > 1800) SkillQ.Cast();
            }
            if (Config.Item("eusage").GetValue<bool>() && SkillE.IsReady() && targetObj.IsValidTarget(500) && targetObj.HasBuff("BlindMonkEOne", true))
            {
                if (Player.Distance(targetObj) > 400 || (Environment.TickCount - SkillE.LastCastAttemptT) > 1800) SkillE.Cast();
            }
            if (Config.Item("rusage").GetValue<bool>() && Config.Item("ult" + targetObj.ChampionName).GetValue<bool>() && SkillR.IsReady() && targetObj.IsValidTarget(SkillR.Range))
            {
                if (targetObj.Health < SkillR.GetDamage(targetObj) || (targetObj.Health < GetQ2Dmg(targetObj, SkillR.GetDamage(targetObj)) && SkillQ.IsReady() && (targetObj.HasBuff("BlindMonkQOne", true) || targetObj.HasBuff("blindmonkqonechaos", true)) && Player.Mana >= 50)) SkillR.Cast(targetObj, PacketCast);
            }
            if (Config.Item("wusage").GetValue<bool>() && SkillW.IsReady() && targetObj.IsValidTarget(SkillE.Range) && (Player.Health * 100 / Player.MaxHealth) <= Config.Item("autowusage").GetValue<Slider>().Value)
            {
                if (WData.Name == "BlindMonkWOne")
                {
                    SkillW.Cast();
                }
                else if (!Player.HasBuff("blindmonkwoneshield", true)) SkillW.Cast();
            }
            if (Config.Item("iusage").GetValue<bool>()) UseItem(targetObj);
            if (Config.Item("ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        static void StarCombo()
        {
            Orbwalk();
            if (targetObj == null) return;
            if (SkillE.IsReady() && EData.Name == "BlindMonkEOne" && targetObj.IsValidTarget(SkillE.Range)) SkillE.Cast();
            if (SkillQ.IsReady() && QData.Name == "BlindMonkQOne")
            {
                if (Config.Item("smite").GetValue<bool>() && SReady && SkillQ.GetPrediction(targetObj).CollisionObjects.Count == 1) CheckingCollision(targetObj);
                SkillQ.Cast(targetObj, PacketCast);
            }
            if (!targetObj.IsValidTarget(SkillR.Range) && SkillR.IsReady() && SkillQ.IsReady() && (targetObj.HasBuff("BlindMonkQOne", true) || targetObj.HasBuff("blindmonkqonechaos", true)) && targetObj.IsValidTarget(SkillW.Range)) WardJump(targetObj.Position);
            UseItem(targetObj);
            if (SkillR.IsReady() && SkillQ.IsReady() && (targetObj.HasBuff("BlindMonkQOne", true) || targetObj.HasBuff("blindmonkqonechaos", true)) && targetObj.IsValidTarget(SkillR.Range) && Player.Mana >= 50) SkillR.Cast(targetObj, PacketCast);
            if (!SkillR.IsReady() && SkillQ.IsReady() && targetObj.IsValidTarget(1300) && (targetObj.HasBuff("BlindMonkQOne", true) || targetObj.HasBuff("blindmonkqonechaos", true)) && (Environment.TickCount - SkillR.LastCastAttemptT) > 500) SkillQ.Cast();
            if (!SkillR.IsReady() && SkillE.IsReady() && targetObj.IsValidTarget(500) && targetObj.HasBuff("BlindMonkEOne", true) && (Player.Distance(targetObj) > 400 || (Environment.TickCount - SkillE.LastCastAttemptT) > 1800)) SkillE.Cast();
            CastIgnite(targetObj);
        }

        static void CastIgnite(Obj_AI_Hero target)
        {
            if (IReady && target.IsValidTarget(IData.SData.CastRange[0]) && target.Health < Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite)) Player.SummonerSpellbook.CastSpell(IData.Slot, target);
        }

        static void UseItem(Obj_AI_Hero target)
        {
            if (BilgeReady && Player.Distance(target) <= 450) Items.UseItem(Bilge, target);
            if (BladeReady && Player.Distance(target) <= 450) Items.UseItem(Blade, target);
            if (TiamatReady && Utility.CountEnemysInRange(350) >= 1) Items.UseItem(Tiamat);
            if (HydraReady && (Utility.CountEnemysInRange(350) >= 2 || (Player.GetAutoAttackDamage(target) < target.Health && Utility.CountEnemysInRange(350) == 1))) Items.UseItem(Hydra);
            if (RandReady && Utility.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
        }

        static double GetQ2Dmg(Obj_AI_Base target, double dmgPlus)
        {
            Int32[] dmgQ = { 50, 80, 110, 140, 170 };
            return Player.CalcDamage(target, Damage.DamageType.Physical, dmgQ[SkillQ.Level - 1] + 0.9 * Player.FlatPhysicalDamageMod + 0.08 * (target.MaxHealth - (target.Health - dmgPlus)));
        }

        static void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly).OrderBy(i => i.Distance(Player)).FirstOrDefault();
            if (minionObj == null) return;
            var Passive = Player.HasBuff("blindmonkpassive_cosmetic", true);
            if (Config.Item("useClearW").GetValue<bool>() && SkillW.IsReady() && minionObj.IsValidTarget(Orbwalking.GetRealAutoAttackRange(minionObj)))
            {
                if (WData.Name == "BlindMonkWOne")
                {
                    if (!Passive) SkillW.Cast();
                }
                else if (!Passive || (Environment.TickCount - SkillW.LastCastAttemptT) > 2500 || !Player.HasBuff("blindmonkwoneshield", true)) SkillW.Cast();
            }
            if (Config.Item("useClearQ").GetValue<bool>() && SkillQ.IsReady())
            {
                if (QData.Name == "BlindMonkQOne")
                {
                    if (!Passive) SkillQ.Cast(minionObj, PacketCast);
                }
                else if ((minionObj.HasBuff("BlindMonkQOne", true) || minionObj.HasBuff("blindmonkqonechaos", true)) && (!Passive || minionObj.Health < SkillQ.GetDamage(minionObj, 1) || (Environment.TickCount - SkillQ.LastCastAttemptT) > 2500 || Player.Distance(minionObj) > 500)) SkillQ.Cast();
            }
            if (Config.Item("useClearE").GetValue<bool>() && SkillE.IsReady() && minionObj.IsValidTarget(SkillE.Range))
            {
                if (EData.Name == "BlindMonkEOne")
                {
                    if (!Passive) SkillE.Cast();
                }
                else if (minionObj.HasBuff("BlindMonkEOne", true) && (!Passive || (Environment.TickCount - SkillE.LastCastAttemptT) > 2500 || Player.Distance(minionObj) > 400)) SkillE.Cast();
            }
            if (Config.Item("useClearI").GetValue<bool>() && Player.Distance(minionObj) <= 350)
            {
                if (TiamatReady) Items.UseItem(Tiamat);
                if (HydraReady) Items.UseItem(Hydra);
            }
        }
    }
}