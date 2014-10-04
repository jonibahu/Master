using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Color = System.Drawing.Color;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace MasterOfLeeSin
{
    class LeeSin
    {
        public static Menu Config;
        //public static List<Obj_AI_Base> targetMinions, jungleMinions;
        public static Obj_AI_Turret turretObj = null;
        public static Obj_AI_Hero Player = ObjectManager.Player, targetObj = null, friendlyObj = null, insecObj = null;
        public static TargetSelector ts;
        public static Spell SkillQ, SkillW, SkillE, SkillR;
        public static SpellDataInst QData, WData, EData, RData, FData, SData, IData;
        public static Boolean QReady = false, WReady = false, EReady = false, RReady = false, FReady = false, SReady = false, IReady = false;
        public static Int32 Tiamat = 3077, Hydra = 3074, Blade = 3153, Bilge = 3144, Rand = 3143;
        public static Boolean TiamatReady = false, HydraReady = false, BladeReady = false, BilgeReady = false, RandReady = false;
        public static InventorySlot useSight = null;
        public static Obj_AI_Base lastWard = null, farmMinion = null;
        public static float lastTimeInsec = 0, lastTimeWard = 0, lastTimeJump = 0, lastTimeQ = 0;
        public static Int32 lastSkin = 0;
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
            SkillQ = new Spell(QData.Slot, QData.SData.CastRange[0]);
            SkillW = new Spell(WData.Slot, WData.SData.CastRange[0]);
            SkillE = new Spell(EData.Slot, EData.SData.CastRange[0]);
            SkillR = new Spell(RData.Slot, RData.SData.CastRange[0]);
            SkillQ.SetSkillshot(-QData.SData.SpellCastTime, QData.SData.LineWidth, QData.SData.MissileSpeed, true, SkillshotType.SkillshotLine);

            Config = new Menu("Master Of LeeSin", "LeeSinCombo", true);
            Config.AddSubMenu(new Menu("Key Bindings", "KeyBindings"));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("scriptActive", "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("insecMake", "Insec").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("starActive", "Star Combo").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("harass", "Harass").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("wardJump", "Ward Jump").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            //Config.SubMenu("KeyBindings").AddItem(new MenuItem("ljclr", "Lane/Jungle Clear").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("ksbrdr", "Kill Steal Baron/Dragon").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Combo Settings", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem("qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("wusage", "Use W").SetValue(false));
            Config.SubMenu("csettings").AddItem(new MenuItem("autowusage", "Use W If Hp 60%").SetValue(false));
            Config.SubMenu("csettings").AddItem(new MenuItem("eusage", "Use E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("rusage", "Use R To Finish").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Insec Settings", "insettings"));
            Config.SubMenu("insettings").AddItem(new MenuItem("insecMode", "Mode:").SetValue(new StringList(new[] { "Selected Ally", "Nearest Tower" })));
            string[] enemylist = ObjectManager.Get<Obj_AI_Hero>().Where(i => i != null && i.IsValid && i.IsEnemy).Select(i => i.ChampionName).ToArray();
            Config.SubMenu("insettings").AddItem(new MenuItem("insecenemy", "Insec Target:").SetValue(new StringList(enemylist))).DontSave();
            string[] allylist = ObjectManager.Get<Obj_AI_Hero>().Where(i => i != null && i.IsValid && i.IsAlly && !i.IsMe).Select(i => i.ChampionName).ToArray();
            Config.SubMenu("insettings").AddItem(new MenuItem("insecally", "To Ally:").SetValue(new StringList(allylist))).DontSave();
            Config.SubMenu("insettings").AddItem(new MenuItem("wjump", "Ward Jump To Insec").SetValue(true));
            Config.SubMenu("insettings").AddItem(new MenuItem("wflash", "Flash If Ward Jump Not Ready").SetValue(true));
            Config.SubMenu("insettings").AddItem(new MenuItem("pflash", "Prioritize Flash To Insec").SetValue(false));

            Config.AddSubMenu(new Menu("Misc Settings", "miscs"));
            //Config.SubMenu("miscs").AddItem(new MenuItem("smite", "Auto Smite Collision Minion").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem("skin", "Use Custom Skin").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem("skin1", "Skin Changer").SetValue(new Slider(4, 1, 7)));
            Config.SubMenu("miscs").AddItem(new MenuItem("packetCast", "Use Packet To Cast").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate Settings", "useUlt"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(i => i != null && i.IsValid && i.IsEnemy))
            {
                Config.SubMenu("useUlt").AddItem(new MenuItem("ult" + enemy.ChampionName, "Use Ultimate On " + enemy.ChampionName).SetValue(true));
            }

            //Config.AddSubMenu(new Menu("Lane/Jungle Clear Settings", "LaneJungClear"));
            //Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearQ", "Use Q").SetValue(true));
            //Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearW", "Use W").SetValue(true));
            //Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearE", "Use E").SetValue(true));
            //Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearI", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Draw Settings", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("drawInsec", "Insec Line").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("drawKillable", "Killable Text").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawQ", "Q Range").SetValue(false));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawW", "W Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawE", "E Range").SetValue(false));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawR", "R Range").SetValue(false));

            Config.AddSubMenu(new Menu("Target Selector", "TSSettings"));
            Config.SubMenu("TSSettings").AddItem(new MenuItem("Focus", "Mode:")).SetValue(new StringList(new[] { "Auto", "Closest", "LessAttack", "LessCast", "LowHP", "MostAD", "MostAP", "NearMouse" }, 7));

            Config.AddToMainMenu();
            if (Config.Item("skin").GetValue<bool>())
            {
                GenModelPacket(Player.ChampionName, Config.Item("skin1").GetValue<Slider>().Value);
                lastSkin = Config.Item("skin1").GetValue<Slider>().Value;
            }
            ts = new TargetSelector(SkillQ.Range, TargetSelector.TargetingMode.NearMouse);
            ts.SetDrawCircleOfTarget(true);
            Game.OnGameUpdate += OnTick;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            GameObject.OnCreate += OnCreateObj;
            Drawing.OnDraw += OnDraw;
            Game.PrintChat("<font color = \"#33CCCC\">[Lee Sin] Master of Insec</font> <font color = \"#fff8e7\">Brian v" + Assembly.GetExecutingAssembly().GetName().Version + "</font>");
        }

        static void ModeFocus()
        {
            switch (Config.Item("Focus").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.AutoPriority);
                    break;
                case 1:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.Closest);
                    break;
                case 2:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.LessAttack);
                    break;
                case 3:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.LessCast);
                    break;
                case 4:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.LowHP);
                    break;
                case 5:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.MostAD);
                    break;
                case 6:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.MostAP);
                    break;
                case 7:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.NearMouse);
                    break;
            }
        }

        static InventorySlot wardSlot()
        {
            Int32[] wardIds = { 3340, 3350, 3205, 3207, 2049, 2045, 2044, 3361, 3154, 3362, 3160, 2043 };
            InventorySlot ward = null;
            foreach (var wardId in wardIds)
            {
                ward = Player.InventoryItems.FirstOrDefault(i => i.Id == (ItemId)wardId);
                if (ward != null && Player.Spellbook.Spells.First(i => (Int32)i.Slot == ward.Slot + 4).State == SpellState.Ready) return ward;
            }
            return ward;
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
            ModeFocus();
            if (Player.IsDead) return;
            PacketCast = Config.Item("packetCast").GetValue<bool>();
            useSight = wardSlot();
            //targetMinions = MinionManager.GetMinions(Player.Position, 600, MinionTypes.All, MinionTeam.Enemy);
            //jungleMinions = MinionManager.GetMinions(Player.Position, 600, MinionTypes.All, MinionTeam.Neutral);
            targetObj = ts.Target;
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(i => i != null && i.IsValid && i.IsEnemy && i.ChampionName == Config.Item("insecenemy").GetValue<StringList>().SList[Config.Item("insecenemy").GetValue<StringList>().SelectedIndex]))
            {
                if (!enemy.IsDead)
                {
                    insecObj = (targetObj.IsValidTarget() && Player.Distance(enemy) > Player.Distance(targetObj)) ? targetObj : enemy;
                }
                else
                {
                    insecObj = targetObj;
                }
            }
            switch (Config.Item("insecMode").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    float allydist = 9999;
                    foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(i => i != null && i.IsValid && i.IsAlly && !i.IsMe))
                    {
                        if (!ally.IsDead && ally.ChampionName == Config.Item("insecally").GetValue<StringList>().SList[Config.Item("insecally").GetValue<StringList>().SelectedIndex])
                        {
                            friendlyObj = ally;
                        }
                        else if (!ally.IsDead && Player.Distance(ally) < allydist)
                        {
                            allydist = Player.Distance(ally);
                            friendlyObj = ally;
                        }
                    }
                    break;
                case 1:
                    float turretdist = 9999;
                    foreach (var turret in ObjectManager.Get<Obj_AI_Turret>().Where(i => i != null && i.IsValid && i.IsAlly && !i.IsMe))
                    {
                        if (!turret.IsDead && Player.Distance(turret) < turretdist)
                        {
                            turretdist = Player.Distance(turret);
                            turretObj = turret;
                        }
                    }
                    break;
            }
            if (Config.Item("insecMake").GetValue<KeyBind>().Active)
            {
                Insec();
                return;
            }
            if (Config.Item("scriptActive").GetValue<KeyBind>().Active)
            {
                NormalCombo();
                return;
            }
            if (Config.Item("starActive").GetValue<KeyBind>().Active)
            {
                StarCombo();
                return;
            }
            if (Config.Item("wardJump").GetValue<KeyBind>().Active)
            {
                WardJump(Game.CursorPos, 600);
                return;
            }
            if (Config.Item("harass").GetValue<KeyBind>().Active)
            {
                Harass();
                return;
            }
            //if (Config.Item("ljclr").GetValue<KeyBind>().Active)
            //{
            //    LaneJungClear();
            //    //return;
            //}
            if (Config.Item("ksbrdr").GetValue<KeyBind>().Active)
            {
                if (KillStealBrDr()) return;
            }
            if (Config.Item("skin").GetValue<bool>() && skinChanged())
            {
                GenModelPacket(Player.ChampionName, Config.Item("skin1").GetValue<Slider>().Value);
                lastSkin = Config.Item("skin1").GetValue<Slider>().Value;
            }
        }

        static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "BlindMonkQOne") lastTimeQ = Environment.TickCount;
            }
        }

        static void OnCreateObj(GameObject sender, EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item("wardJump").GetValue<KeyBind>().Active || Config.Item("insecMake").GetValue<KeyBind>().Active || Config.Item("wjump").GetValue<bool>() || Config.Item("wflash").GetValue<bool>() || Config.Item("pflash").GetValue<bool>())
            {
                if (sender != null && sender.IsValid && (sender.Name == "VisionWard" || sender.Name == "SightWard")) lastWard = sender as Obj_AI_Base;
            }
        }

        static void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item("DrawQ").GetValue<bool>() && SkillQ.Level > 0)
            {
                Utility.DrawCircle(Player.Position, SkillQ.Range, SkillQ.IsReady() ? Color.Green : Color.Red);
            }
            if (Config.Item("DrawW").GetValue<bool>() && SkillW.Level > 0)
            {
                Utility.DrawCircle(Player.Position, SkillW.Range, SkillW.IsReady() ? Color.Green : Color.Red);
            }
            if (Config.Item("DrawE").GetValue<bool>() && SkillE.Level > 0)
            {
                Utility.DrawCircle(Player.Position, SkillE.Range, SkillE.IsReady() ? Color.Green : Color.Red);
            }
            if (Config.Item("DrawR").GetValue<bool>() && SkillR.Level > 0)
            {
                Utility.DrawCircle(Player.Position, SkillR.Range, SkillR.IsReady() ? Color.Green : Color.Red);
            }
            if (Config.Item("drawInsec").GetValue<bool>() && SkillR.IsReady() && ((SkillW.IsReady() && useSight != null) || FReady))
            {
                Byte validTargets = 0;
                switch (Config.Item("insecMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (insecObj.IsValidTarget())
                        {
                            Utility.DrawCircle(insecObj.Position, 70, Color.FromArgb(0, 204, 0));
                            validTargets += 1;
                        }
                        if (friendlyObj != null && friendlyObj.IsValid)
                        {
                            Utility.DrawCircle(friendlyObj.Position, 70, Color.FromArgb(0, 204, 0));
                            validTargets += 1;
                        }
                        if (validTargets == 2)
                        {
                            var pos = ReverseVector(insecObj.Position, friendlyObj.Position, insecObj.IsValidTarget(SkillQ.Range) ? 800 : 300);
                            Drawing.DrawLine(Drawing.WorldToScreen(pos), Drawing.WorldToScreen(insecObj.Position), 2, Color.White);
                        }
                        break;
                    case 1:
                        if (insecObj.IsValidTarget())
                        {
                            Utility.DrawCircle(insecObj.Position, 70, Color.FromArgb(0, 204, 0));
                            validTargets += 1;
                        }
                        if (turretObj != null && turretObj.IsValid)
                        {
                            Utility.DrawCircle(turretObj.Position, 70, Color.FromArgb(0, 204, 0));
                            validTargets += 1;
                        }
                        if (validTargets == 2)
                        {
                            var pos = ReverseVector(insecObj.Position, turretObj.Position, insecObj.IsValidTarget(SkillQ.Range) ? 800 : 300);
                            Drawing.DrawLine(Drawing.WorldToScreen(pos), Drawing.WorldToScreen(insecObj.Position), 2, Color.White);
                        }
                        break;
                }
            }
            if (Config.Item("drawKillable").GetValue<bool>())
            {
                foreach (var Enemy in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget()))
                {
                    var tempHealth = Enemy.Health - Player.GetAutoAttackDamage(Enemy);
                    if (SkillQ.IsReady() && QData.Name == "BlindMonkQOne") tempHealth -= SkillQ.GetDamage(Enemy);
                    if (SkillR.IsReady() && Config.Item("ult" + Enemy.ChampionName).GetValue<bool>()) tempHealth -= SkillR.GetDamage(Enemy);
                    if (SkillE.IsReady() && EData.Name == "BlindMonkEOne") tempHealth -= SkillE.GetDamage(Enemy);
                    if (SkillQ.IsReady() && Enemy.HasBuff("BlindMonkQOne", true)) tempHealth -= SkillQ.GetDamage(Enemy, 1);
                    if (tempHealth <= 0) Utility.PrintFloatText(Enemy, "Killable", Packet.FloatTextPacket.Invulnerable);
                }
            }
        }

        static Vector3 ReverseVector(Vector3 from, Vector3 to, float distance)
        {
            var X = from.X + (distance / from.Distance(to)) * (to.X - from.X);
            var Y = from.Y + (distance / from.Distance(to)) * (to.Y - from.Y);
            return new Vector3(X, Y, to.Z);
        }

        static bool KillStealBrDr()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            foreach (var minion in MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAllyForEnemy))
            {
                if (minion.Name == "Worm12.1.1" || minion.Name == "Dragon6.1.1")
                {
                    if (SReady && minion.IsValidTarget(SData.SData.CastRange[0]) && minion.Health <= Player.GetSummonerSpellDamage(minion, Damage.SummonerSpell.Smite))
                    {
                        Player.SummonerSpellbook.CastSpell(SData.Slot, minion);
                        return true;
                    }
                    if (SkillQ.IsReady() && !SReady && Player.CalcDamage(minion, Damage.DamageType.Physical, minion.Health - SkillQ.GetDamage(minion)) <= SkillQ.GetDamage(minion, 1))
                    {
                        if (QData.Name == "BlindMonkQOne")
                        {
                            SkillQ.Cast(minion, PacketCast);
                        }
                        else if (minion.HasBuff("BlindMonkQOne", true))
                        {
                            SkillQ.Cast();
                            return true;
                        }
                    }
                    var dmgQSmite = Player.CalcDamage(minion, Damage.DamageType.Physical, minion.Health - (SkillQ.GetDamage(minion) + Player.GetSummonerSpellDamage(minion, Damage.SummonerSpell.Smite)));
                    if (SkillQ.IsReady() && SReady && dmgQSmite <= SkillQ.GetDamage(minion, 1))
                    {
                        if (QData.Name == "BlindMonkQOne")
                        {
                            SkillQ.Cast(minion, PacketCast);
                        }
                        else if (minion.HasBuff("BlindMonkQOne", true))
                        {
                            SkillQ.Cast();
                            Player.SummonerSpellbook.CastSpell(SData.Slot, minion);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        static bool CheckingCollision(Obj_AI_Hero target)
        {
            //foreach (var minion in MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.Enemy).Where(i => i.Distance(Player) >= 550 && i.Health > SkillQ.GetDamage(i)))
            //{
            //    var collision = SkillQ.GetPrediction(minion).CollisionObjects;
            //    if (collision.Count == 0)
            //    {
            //        return false;
            //    }
            //    else if (collision.Count == 1 && collision.First().IsMinion && minion.ServerPosition.To2D().Distance(Player.Position.To2D(), target.Position.To2D(), true) < 80 && SReady && collision.First().Health <= Player.GetSummonerSpellDamage(collision.First(), Damage.SummonerSpell.Smite))
            //    {
            //        Player.SummonerSpellbook.CastSpell(SData.Slot, collision.First());
            //        return true;
            //    }
            //}
            return false;
        }

        static void Harass()
        {
            Orbwalking.Orbwalk(targetObj, Game.CursorPos);
            if (targetObj != null)
            {
                var jumpHero = ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsAlly && !i.IsMe && i.IsValidTarget(SkillQ.Range, false)).OrderBy(i => targetObj.Distance(i) <= SkillW.Range);
                var jumpMinion = ObjectManager.Get<Obj_AI_Minion>().Where(i => i.IsAlly && !i.IsMe && i.IsValidTarget(SkillQ.Range, false)).OrderBy(i => targetObj.Distance(i) <= SkillW.Range);
                if (jumpHero.Count() == 0 || jumpMinion.Count() == 0) return;
                if (SkillQ.IsReady())
                {
                    if (QData.Name == "BlindMonkQOne")
                    {
                        SkillQ.Cast(targetObj, PacketCast);
                        return;
                    }
                    else if (targetObj.HasBuff("BlindMonkQOne", true) && SkillW.IsReady() && WData.Name == "BlindMonkWOne" && Player.Mana >= 80 && (Player.Health / Player.MaxHealth) >= 0.5)
                    {
                        SkillQ.Cast();
                        return;
                    }
                }
                if (!SkillQ.IsReady() && SkillE.IsReady() && EData.Name == "BlindMonkEOne" && targetObj.IsValidTarget(SkillE.Range) && Player.Mana >= 100)
                {
                    SkillE.Cast();
                    return;
                }
                if (!SkillQ.IsReady() && SkillW.IsReady() && WData.Name == "BlindMonkWOne" && targetObj.IsValidTarget(SkillE.Range))
                {
                    float maxDist = 0;
                    Obj_AI_Base Jumper = null;
                    //var jumpHero = ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsAlly && !i.IsMe && i.IsValidTarget(SkillQ.Range, false));
                    foreach (var Hero in jumpHero)
                    {
                        var distJumper = targetObj.Distance(Hero);
                        if (distJumper > 450 && distJumper < 650 && distJumper > maxDist)
                        {
                            Jumper = Hero;
                            maxDist = distJumper;
                        }
                    }
                    //var jumpMinion = ObjectManager.Get<Obj_AI_Minion>().Where(i => i.IsAlly && !i.IsMe && i.IsValidTarget(SkillQ.Range, false));
                    foreach (var Minion in jumpMinion)
                    {
                        var distJumper = targetObj.Distance(Minion);
                        if (distJumper > 450 && distJumper < 650 && distJumper > maxDist)
                        {
                            Jumper = Minion;
                            maxDist = distJumper;
                        }
                    }
                    if (Jumper.IsValidTarget(SkillW.Range, false)) SkillW.Cast(Jumper, PacketCast);
                }
            }
        }

        static void WardJump(Vector3 Pos, float dist)
        {
            if ((SkillW.IsReady() && WData.Name != "BlindMonkWOne") || !SkillW.IsReady())
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Pos);
                return;
            }
            bool Jumped = false;
            if (Player.Distance(Pos) > dist) Pos = ReverseVector(Player.Position, Pos, dist);
            if (lastWard != null && lastWard.IsValid && lastWard.Distance(Pos) <= 200)
            {
                Jumped = true;
                Player.IssueOrder(GameObjectOrder.MoveTo, Pos);
                if (Player.Distance(lastWard) <= SkillW.Range + lastWard.BoundingRadius)
                {
                    if ((Environment.TickCount - lastTimeJump) > 1000)
                    {
                        SkillW.Cast(lastWard, PacketCast);
                        lastTimeJump = Environment.TickCount;
                    }
                    return;
                }
            }
            if (!Jumped && useSight != null)
            {
                if ((Environment.TickCount - lastTimeWard) > 1000)
                {
                    useSight.UseItem(Pos);
                    lastTimeWard = Environment.TickCount;
                }
            }
        }

        static void Insec()
        {
            Orbwalking.Orbwalk(targetObj, Game.CursorPos);
            if (insecObj.IsValidTarget(SkillQ.Range))
            {
                if (SkillQ.IsReady())
                {
                    if (QData.Name == "BlindMonkQOne")
                    {
                        CheckingCollision(targetObj);
                        //if (SkillQ.GetPrediction(insecObj).Hitchance >= HitChance.High)
                        //{
                        SkillQ.Cast(insecObj, PacketCast);
                            return;
                        //}
                        //var prediction = SkillQ.GetPrediction(insecObj);
                        //if (prediction.Hitchance == HitChance.Collision && SReady && Config.Item("smite").GetValue<bool>())
                        //{
                        //    var collision = prediction.CollisionObjects.Where(i => i.NetworkId != Player.NetworkId).OrderBy(i => i.Distance(Player)).FirstOrDefault();
                        //    if (collision.Distance(prediction.UnitPosition) < 200 && collision.IsValidTarget(SData.SData.CastRange[0]) && collision.Health < Damage.GetSummonerSpellDamage(Player, collision, Damage.SummonerSpell.Smite))
                        //    {
                        //        Player.SummonerSpellbook.CastSpell(SData.Slot, collision);
                        //        SkillQ.Cast(prediction.CastPosition, true);
                        //        return;
                        //    }
                        //}
                        //else if (prediction.Hitchance >= HitChance.Medium)
                        //{
                        //    SkillQ.Cast(prediction.CastPosition, true);
                        //    return;
                        //}
                    }
                    else if (insecObj.HasBuff("BlindMonkQOne", true))
                    {
                        lastTimeInsec = Environment.TickCount + 150;
                        SkillQ.Cast();
                        return;
                    }
                }
                if (Config.Item("wjump").GetValue<bool>() && !Config.Item("pflash").GetValue<bool>() && WardJumpInsec()) return;
                if (Config.Item("wjump").GetValue<bool>() && !Config.Item("pflash").GetValue<bool>() && Config.Item("wflash").GetValue<bool>() && FlashInsec()) return;
                if (Config.Item("pflash").GetValue<bool>() && FlashInsec()) return;
                if (Config.Item("pflash").GetValue<bool>() && Config.Item("wjump").GetValue<bool>() && !FReady && WardJumpInsec()) return;
            }
        }

        static bool WardJumpInsec()
        {
            if (insecObj.IsValidTarget(400))
            {
                switch (Config.Item("insecMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (SkillR.IsReady() && friendlyObj != null && friendlyObj.IsValid)
                        {
                            if (insecObj.IsValidTarget(SkillR.Range))
                            {
                                var pos = ReverseVector(Player.Position, insecObj.Position, insecObj.Distance(Player) + 500);
                                var newDistance = friendlyObj.Distance(insecObj) - friendlyObj.Distance(pos);
                                if (newDistance > 0 && (newDistance / 500) > 0.7)
                                {
                                    SkillR.Cast(insecObj, PacketCast);
                                    return true;
                                }
                            }
                            if (SkillW.IsReady() && WData.Name == "BlindMonkWOne" && Environment.TickCount > lastTimeInsec)
                            {
                                if ((Environment.TickCount - lastTimeJump) < 1000 && (Environment.TickCount - lastTimeJump) >= 10)
                                {
                                    SkillW.Cast(lastWard, PacketCast);
                                    lastTimeInsec = Environment.TickCount + 500;
                                    return true;
                                }
                                else if (useSight != null)
                                {
                                    if ((Environment.TickCount - lastTimeWard) > 350)
                                    {
                                        var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                        var pos = ReverseVector(friendlyObj.Position, targetObj2, targetObj2.Distance(friendlyObj.Position) + 300);
                                        if (Player.Distance(pos) < 600)
                                        {
                                            useSight.UseItem(pos);
                                            lastTimeWard = Environment.TickCount;
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case 1:
                        if (SkillR.IsReady() && turretObj != null && turretObj.IsValid)
                        {
                            if (insecObj.IsValidTarget(SkillR.Range))
                            {
                                var pos = ReverseVector(Player.Position, insecObj.Position, insecObj.Distance(Player) + 500);
                                var newDistance = turretObj.Distance(insecObj) - turretObj.Distance(pos);
                                if (newDistance > 0 && (newDistance / 500) > 0.7)
                                {
                                    SkillR.Cast(insecObj, PacketCast);
                                    return true;
                                }
                            }
                            if (SkillW.IsReady() && WData.Name == "BlindMonkWOne" && Environment.TickCount > lastTimeInsec)
                            {
                                if ((Environment.TickCount - lastTimeJump) < 1000 && (Environment.TickCount - lastTimeJump) >= 10)
                                {
                                    SkillW.Cast(lastWard, PacketCast);
                                    lastTimeInsec = Environment.TickCount + 500;
                                    return true;
                                }
                                else if (useSight != null)
                                {
                                    if ((Environment.TickCount - lastTimeWard) > 350)
                                    {
                                        var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                        var pos = ReverseVector(turretObj.Position, targetObj2, targetObj2.Distance(turretObj.Position) + 300);
                                        if (Player.Distance(pos) < 600)
                                        {
                                            useSight.UseItem(pos);
                                            lastTimeWard = Environment.TickCount;
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            return false;
        }

        static bool FlashInsec()
        {
            if (insecObj.IsValidTarget(400) && ((SkillW.IsReady() && useSight == null) || (!SkillW.IsReady() && useSight != null) || (!SkillW.IsReady() && useSight == null)))
            {
                switch (Config.Item("insecMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (SkillR.IsReady() && friendlyObj != null && friendlyObj.IsValid)
                        {
                            if (insecObj.IsValidTarget(SkillR.Range))
                            {
                                var pos = ReverseVector(Player.Position, insecObj.Position, insecObj.Distance(Player) + 500);
                                var newDistance = friendlyObj.Distance(insecObj) - friendlyObj.Distance(pos);
                                if (newDistance > 0 && (newDistance / 500) > 0.7)
                                {
                                    SkillR.Cast(insecObj, PacketCast);
                                    return true;
                                }
                            }
                            if (FReady && Environment.TickCount > lastTimeInsec)
                            {
                                //var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                //var pos = ReverseVector(friendlyObj.Position, targetObj2, targetObj2.Distance(friendlyObj.Position) + 400);
                                //if (Player.Distance(pos) < FData.SData.CastRange[0])
                                //{
                                //    Player.SummonerSpellbook.CastSpell(FData.Slot, pos);
                                //    lastTimeInsec = Environment.TickCount + 350;
                                //    return true;
                                //}
                                if ((Environment.TickCount - lastTimeJump) < 1000 && (Environment.TickCount - lastTimeJump) >= 10)
                                {
                                    lastTimeInsec = Environment.TickCount + 500;
                                    return true;
                                }
                                else
                                {
                                    var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                    var pos = ReverseVector(friendlyObj.Position, targetObj2, targetObj2.Distance(friendlyObj.Position) + 400);
                                    if (Player.Distance(pos) < FData.SData.CastRange[0])
                                    {
                                        Player.SummonerSpellbook.CastSpell(FData.Slot, pos);
                                        return true;
                                    }
                                }
                            }
                        }
                        break;
                    case 1:
                        if (SkillR.IsReady() && turretObj != null && turretObj.IsValid)
                        {
                            if (insecObj.IsValidTarget(SkillR.Range))
                            {
                                var pos = ReverseVector(Player.Position, insecObj.Position, insecObj.Distance(Player) + 500);
                                var newDistance = turretObj.Distance(insecObj) - turretObj.Distance(pos);
                                if (newDistance > 0 && (newDistance / 500) > 0.7)
                                {
                                    SkillR.Cast(insecObj, PacketCast);
                                    return true;
                                }
                            }
                            if (FReady && Environment.TickCount > lastTimeInsec)
                            {
                                //var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                //var pos = ReverseVector(turretObj.Position, targetObj2, targetObj2.Distance(turretObj.Position) + 400);
                                //if (Player.Distance(pos) < FData.SData.CastRange[0])
                                //{
                                //    Player.SummonerSpellbook.CastSpell(FData.Slot, pos);
                                //    lastTimeInsec = Environment.TickCount + 350;
                                //    return true;
                                //}
                                if ((Environment.TickCount - lastTimeJump) < 1000 && (Environment.TickCount - lastTimeJump) >= 10)
                                {
                                    lastTimeInsec = Environment.TickCount + 500;
                                    return true;
                                }
                                else
                                {
                                    var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                    var pos = ReverseVector(turretObj.Position, targetObj2, targetObj2.Distance(turretObj.Position) + 400);
                                    if (Player.Distance(pos) < FData.SData.CastRange[0])
                                    {
                                        Player.SummonerSpellbook.CastSpell(FData.Slot, pos);
                                        return true;
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            return false;
        }

        static void NormalCombo()
        {
            Orbwalking.Orbwalk(targetObj, Game.CursorPos);
            if (targetObj != null)
            {
                if (SkillQ.IsReady() && Config.Item("qusage").GetValue<bool>())
                {
                    if (QData.Name == "BlindMonkQOne")
                    {
                        SkillQ.Cast(targetObj, PacketCast);
                        return;
                    }
                    else if (targetObj.HasBuff("BlindMonkQOne", true) && (Player.Distance(targetObj) > 500 || targetObj.Health <= SkillQ.GetDamage(targetObj, 1) || (Environment.TickCount - lastTimeQ) > 2000))
                    {
                        SkillQ.Cast();
                        return;
                    }
                }
                if (SkillE.IsReady() && Config.Item("eusage").GetValue<bool>())
                {
                    if (EData.Name == "BlindMonkEOne" && targetObj.IsValidTarget(SkillE.Range))
                    {
                        SkillE.Cast();
                        return;
                    }
                    else if (targetObj.HasBuff("BlindMonkEOne", true) && targetObj.IsValidTarget(450))
                    {
                        SkillE.Cast();
                        return;
                    }
                }
                if (SkillR.IsReady() && Config.Item("rusage").GetValue<bool>() && Config.Item("ult" + targetObj.ChampionName).GetValue<bool>() && targetObj.IsValidTarget(SkillR.Range))
                {
                    if (SkillR.IsKillable(targetObj) || (Player.CalcDamage(targetObj, Damage.DamageType.Physical, targetObj.Health - SkillR.GetDamage(targetObj)) <= SkillQ.GetDamage(targetObj, 1) && targetObj.HasBuff("BlindMonkQOne", true) && SkillQ.IsReady() && Player.Mana >= 50))
                    {
                        SkillR.Cast(targetObj, PacketCast);
                        return;
                    }
                }
                if (SkillW.IsReady() && Config.Item("autowusage").GetValue<bool>())
                {
                    if (targetObj.IsValidTarget(350) && (Player.Health / Player.MaxHealth) < 0.6)
                    {
                        if (WData.Name == "BlindMonkWOne")
                        {
                            SkillW.Cast(Player, PacketCast);
                            return;
                        }
                        else if (!Player.HasBuff("blindmonkwoneshield", true))
                        {
                            SkillW.Cast();
                            return;
                        }
                    }
                }
                if (SkillW.IsReady() && Config.Item("wusage").GetValue<bool>())
                {
                    if (targetObj.IsValidTarget(350))
                    {
                        if (WData.Name == "BlindMonkWOne")
                        {
                            SkillW.Cast(Player, PacketCast);
                            return;
                        }
                        else if (!Player.HasBuff("blindmonkwoneshield", true))
                        {
                            SkillW.Cast();
                            return;
                        }
                    }
                }
                if (Config.Item("iusage").GetValue<bool>()) UseItem(targetObj);
                if (IReady && Config.Item("ignite").GetValue<bool>())
                {
                    CastIgnite(targetObj);
                    return;
                }
            }
            //var prediction = SkillQ.GetPrediction(targetObj);
            //if (prediction.Hitchance == HitChance.Collision && SReady && Config.Item("smite").GetValue<bool>())
            //{
            //    var collision = prediction.CollisionObjects.Where(i => i.NetworkId != Player.NetworkId).OrderBy(i => i.Distance(Player)).FirstOrDefault();
            //    if (collision.Distance(prediction.UnitPosition) < 200 && collision.IsValidTarget(SData.SData.CastRange[0]) && collision.Health < Damage.GetSummonerSpellDamage(Player, collision, Damage.SummonerSpell.Smite))
            //    {
            //        Player.SummonerSpellbook.CastSpell(SData.Slot, collision);
            //        SkillQ.Cast(prediction.CastPosition, true);
            //        return;
            //    }
            //}
            //else if (prediction.Hitchance >= HitChance.Medium)
            //{
            //    SkillQ.Cast(prediction.CastPosition, true);
            //    return;
            //}
        }

        static void StarCombo()
        {
            Orbwalking.Orbwalk(targetObj, Game.CursorPos);
            if (targetObj != null)
            {
                if (SkillQ.IsReady() && QData.Name == "BlindMonkQOne")
                {
                    SkillQ.Cast(targetObj, PacketCast);
                    return;
                }
                if (!targetObj.IsValidTarget(SkillR.Range) && SkillR.IsReady() && targetObj.HasBuff("BlindMonkQOne", true) && targetObj.IsValidTarget(SkillW.Range)) WardJump(targetObj.Position, 600);
                if (SkillE.IsReady() && EData.Name == "BlindMonkEOne" && targetObj.HasBuff("BlindMonkQOne", true) && targetObj.IsValidTarget(SkillE.Range))
                {
                    SkillE.Cast();
                    return;
                }
                if (SkillR.IsReady() && targetObj.HasBuff("BlindMonkQOne", true) && targetObj.IsValidTarget(SkillR.Range) && Player.Mana >= 50) SkillR.Cast(targetObj, PacketCast);
                if (!SkillR.IsReady() && targetObj.HasBuff("BlindMonkQOne", true) && Player.Distance(targetObj) > 400)
                {
                    SkillQ.Cast();
                    return;
                }
                if (!SkillR.IsReady() && targetObj.HasBuff("BlindMonkEOne", true) && targetObj.IsValidTarget(450))
                {
                    SkillE.Cast();
                    return;
                }
                UseItem(targetObj);
                if (IReady)
                {
                    CastIgnite(targetObj);
                    return;
                }
            }
        }

        static void CastIgnite(Obj_AI_Hero target)
        {
            if (target.IsValidTarget(IData.SData.CastRange[0]) && target.Health < Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite))
            {
                Player.SummonerSpellbook.CastSpell(IData.Slot, target);
                return;
            }
        }

        static void UseItem(Obj_AI_Hero target)
        {
            if (BilgeReady && Player.Distance(target) < 450)
            {
                Items.UseItem(Bilge, target);
                return;
            }
            if (BladeReady && Player.Distance(target) < 450)
            {
                Items.UseItem(Blade, target);
                return;
            }
            if (TiamatReady && Utility.CountEnemysInRange(350) >= 1)
            {
                Items.UseItem(Tiamat);
                return;
            }
            if (HydraReady && (Utility.CountEnemysInRange(350) >= 2 || (Player.GetAutoAttackDamage(target) < target.Health && Utility.CountEnemysInRange(350) == 1)))
            {
                Items.UseItem(Hydra);
                return;
            }
            if (RandReady && Utility.CountEnemysInRange(450) >= 1)
            {
                Items.UseItem(Rand);
                return;
            }
        }

        static void GenModelPacket(string champ, int skinId)
        {
            var p = Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, skinId, champ));
            p.Process();
        }

        static bool skinChanged()
        {
            return (Config.Item("skin1").GetValue<Slider>().Value != lastSkin);
        }

        static void LaneJungClear()
        {
            var jungleMin = MinionManager.GetMinions(ObjectManager.Player.Position, 600, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            foreach (var farmMinion in jungleMin)
            {
                if (farmMinion.IsValidTarget(600))
                {
                    if (Player.HasBuff("blindmonkpassive_cosmetic", true) && farmMinion.IsValidTarget(Player.AttackRange))
                    {
                        Player.IssueOrder(GameObjectOrder.AttackUnit, farmMinion);
                        return;
                    }
                    if (TiamatReady && Player.Distance(farmMinion) < 350) Items.UseItem(Tiamat);
                    if (HydraReady && Player.Distance(farmMinion) < 350) Items.UseItem(Hydra);
                    if (SkillQ.IsReady() && QData.Name == "BlindMonkQOne" && Config.Item("useClearQ").GetValue<bool>())
                    {
                        SkillQ.Cast(farmMinion, PacketCast);
                        return;
                    }
                    else if (farmMinion.HasBuff("BlindMonkQOne", true) && Player.Distance(farmMinion) < 500)
                    {
                        SkillQ.Cast();
                        return;
                    }
                    if (SkillE.IsReady() && EData.Name == "BlindMonkEOne" && Config.Item("useClearE").GetValue<bool>() && farmMinion.IsValidTarget(200))
                    {
                        SkillE.Cast();
                        return;
                    }
                    else if (Player.Distance(farmMinion) < 500)
                    {
                        SkillE.Cast();
                        return;
                    }
                    if (SkillW.IsReady() && WData.Name == "BlindMonkWOne" && Config.Item("useClearW").GetValue<bool>())
                    {
                        SkillW.Cast(Player, PacketCast);
                        return;
                    }
                    else if (Player.Distance(farmMinion) < 200)
                    {
                        SkillW.Cast();
                        return;
                    }
                    Player.IssueOrder(GameObjectOrder.AttackUnit, farmMinion);
                    return;
                }
            }
        }
    }
}