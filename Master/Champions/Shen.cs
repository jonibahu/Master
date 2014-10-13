using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
namespace Master
{
    class Shen : Program
    {
        private const String Version = "1.0.0";
        private Obj_AI_Hero Player = ObjectManager.Player, targetObj = null;
        private Spell SkillQ, SkillW, SkillE, SkillR;
        private SpellDataInst QData, WData, EData, RData, IData;
        private Boolean IReady = false;
        private Boolean PacketCast = false;

        public Shen()
        {
            QData = Player.Spellbook.GetSpell(SpellSlot.Q);
            WData = Player.Spellbook.GetSpell(SpellSlot.W);
            EData = Player.Spellbook.GetSpell(SpellSlot.E);
            RData = Player.Spellbook.GetSpell(SpellSlot.R);
            IData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonerdot"));
            SkillQ = new Spell(QData.Slot, 475);
            SkillW = new Spell(WData.Slot);
            SkillE = new Spell(EData.Slot, 600);
            SkillR = new Spell(RData.Slot);
            SkillE.SetSkillshot(-EData.SData.SpellCastTime, EData.SData.LineWidth, EData.SData.MissileSpeed, false, SkillshotType.SkillshotLine);
        }
    }
}