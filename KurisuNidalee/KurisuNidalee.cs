using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace KurisuNidalee
{
    /*  _____ _   _     _         
     * |   | |_|_| |___| |___ ___ 
     * | | | | | . | .'| | -_| -_|
     * |_|___|_|___|__,|_|___|___|
     * 
     * Revision: 106 - 11/10/2014
     * + Hitchance now adjusts based on range
     * + Lag free drawings
     * 
     * Revision: 105 - 30/09/2014
     * + DamageLib update
     * + Aspect of Cougar tweaks
     * 
     * Revision: 104 - 27/09/2014
     * + Added frost queens claims
     * + New Laneclear method
     * 
     * Revision: 103 - 24/09/2014
     * + HealEngine added
     * + Tweaks and Optimization
     * 
     * Rivision: 102 - 24/09/2014
     * + Killsteal prediction fix
     * 
     * Revision: 100 - 24/09/2014
     * + Beta Release
     */

    internal class KurisuNidalee
    {
        public KurisuNidalee()
        {
            //if (Me.BaseSkinName != "Nidalee") return;
            Console.WriteLine("Kurisu assembly is loading...");
            CustomEvents.Game.OnGameLoad += Initialize;
        }

        #region Nidalee: Properties
        private static Menu Config;
        private static Obj_AI_Base Target;
        private static readonly Obj_AI_Hero Me = ObjectManager.Player;
        private static Orbwalking.Orbwalker Orbwalker;
        private static bool Kitty;
        private static HitChance hc;

        private static Spell javelin = new Spell(SpellSlot.Q, 1500f);
        private static Spell bushwack = new Spell(SpellSlot.W, 900f);
        private static Spell primalsurge = new Spell(SpellSlot.E, 650f);
        private static Spell takedown = new Spell(SpellSlot.Q, 200f);
        private static Spell pounce = new Spell(SpellSlot.W, 375f);
        private static Spell swipe = new Spell(SpellSlot.E, 300f);
        private static Spell aspectofcougar = new Spell(SpellSlot.R, float.MaxValue);

        private static readonly SpellDataInst spellData = Me.Spellbook.GetSpell(SpellSlot.Q);
        private static readonly List<Spell> cougarList = new List<Spell>();
        private static readonly List<Spell> humanList = new List<Spell>();

        private static bool Packets() { return Config.Item("usepackets").GetValue<bool>(); }
        private static bool TargetHunted(Obj_AI_Base target) { return target.HasBuff("nidaleepassivehunted", true); }
        private static readonly string[] JungleMinions =
        {
            "AncientGolem", "GreatWraith", "Wraith", "LizardElder", "Golem", "Worm", "Dragon", "GiantWolf" 
        
        };

        #endregion

        #region Nidalee: Initialize
        private void Initialize(EventArgs args)
        {            
            NidaMenu();

            cougarList.AddRange(new[] { takedown, pounce, swipe });
            humanList.AddRange(new[] { javelin, bushwack, primalsurge });

            javelin.SetSkillshot(0.50f, 70f, 1300f, true, SkillshotType.SkillshotLine);
            bushwack.SetSkillshot(0.50f, 100f, 1500f, false, SkillshotType.SkillshotCircle);
            swipe.SetSkillshot(0.50f, 375f, 1500f, false, SkillshotType.SkillshotCone);
            pounce.SetSkillshot(0.50f, 400f, 1500f, false, SkillshotType.SkillshotCone);

            Game.OnGameUpdate += NidaleeOnUpdate;
            Drawing.OnDraw += NidaleeOnDraw;
            Obj_AI_Base.OnProcessSpellCast += NidaleeTracker;
        }
        #endregion

        #region Nidalee: Menu
        private void NidaMenu()
        {
            Config = new Menu("Kurisu: Nidaleee", "nidalee", true);

            var nidaOrb = new Menu("Nidalee: Orbwalker", "orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(nidaOrb);
            Config.AddSubMenu(nidaOrb);

            var nidaTS = new Menu("Nidalee: Selector", "target selecter");
            SimpleTs.AddToMenu(nidaTS);
            Config.AddSubMenu(nidaTS);

            var nidaSpells = new Menu("Nidalee: Spells", "spells");
            nidaSpells.AddItem(new MenuItem("usehumanq", "Use Javelin Toss")).SetValue(true);
            nidaSpells.AddItem(new MenuItem("usehumanw", "Use Bushwack")).SetValue(true);
            nidaSpells.AddItem(new MenuItem(" ", " "));
            nidaSpells.AddItem(new MenuItem("usecougarq", "Use Takedown")).SetValue(true);
            nidaSpells.AddItem(new MenuItem("usecougarw", "Use Pounce")).SetValue(true);
            nidaSpells.AddItem(new MenuItem("pouncerange", "Pounce Min Distance")).SetValue(new Slider(125, 50, 300));
            nidaSpells.AddItem(new MenuItem("usecougare", "Use Swipe")).SetValue(true);
            nidaSpells.AddItem(new MenuItem("usecougarr", "Auto Switch Forms")).SetValue(true);
            Config.AddSubMenu(nidaSpells);

            var nidaHeals = new Menu("Nidalee: HealEngine", "hengine");
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly))
            {
                nidaHeals.AddItem(new MenuItem("heal" + hero.SkinName, hero.SkinName)).SetValue(true);
                nidaHeals.AddItem(new MenuItem("healpct" + hero.SkinName, hero.SkinName + " heal %")).SetValue(new Slider(50));
            }
            nidaHeals.AddItem(new MenuItem("healmanapct", "Minimum Mana %")).SetValue(new Slider(40));
            Config.AddSubMenu(nidaHeals);


            var nidaHarass = new Menu("Nidalee: Harass", "harass");
            nidaHarass.AddItem(new MenuItem("usehumanq2", "Use Javelin Toss")).SetValue(true);
            nidaHarass.AddItem(new MenuItem("humanqpct", "Minimum Mana %")).SetValue(new Slider(70));
            Config.AddSubMenu(nidaHarass);

            var nidaClear = new Menu("Nidalee: Laneclear", "laneclear");
            nidaClear.AddItem(new MenuItem("clearhumanq", "Use Javelin")).SetValue(false);
            nidaClear.AddItem(new MenuItem(" ", " "));
            nidaClear.AddItem(new MenuItem("clearcougarq", "Use Takedown")).SetValue(true);
            nidaClear.AddItem(new MenuItem("clearcougarw", "Use Pounce")).SetValue(true);
            nidaClear.AddItem(new MenuItem("clearcougare", "Use Swipe")).SetValue(true);
            nidaClear.AddItem(new MenuItem("clearcougarr", "Auto Switch Forms")).SetValue(false);
            nidaClear.AddItem(new MenuItem("clearpct", "Minimum Mana %")).SetValue(new Slider(55));
            Config.AddSubMenu(nidaClear);

            var nidaJungle = new Menu("Nidalee: Jungleclear", "jungleclear");
            nidaJungle.AddItem(new MenuItem("jghumanq", "Use Javelin Toss")).SetValue(true);
            nidaJungle.AddItem(new MenuItem("jghumanw", "Use Bushwack")).SetValue(true);
            nidaJungle.AddItem(new MenuItem(" ", " "));
            nidaJungle.AddItem(new MenuItem("jgcougarq", "Use Takedown")).SetValue(true);
            nidaJungle.AddItem(new MenuItem("jgcougarw", "Use Pounce")).SetValue(true);
            nidaJungle.AddItem(new MenuItem("jgcougare", "Use Swipe")).SetValue(true);
            nidaJungle.AddItem(new MenuItem("jgcougarr", "Auto Switch Forms")).SetValue(true);
            nidaJungle.AddItem(new MenuItem("jgrpct", "Minimum Mana %")).SetValue(new Slider(55, 0, 100));
            Config.AddSubMenu(nidaJungle);

            var nidaMisc = new Menu("Nidalee: Misc", "nidamisc");
            nidaMisc.AddItem(new MenuItem("usedfg", "Use DFG")).SetValue(true);
            nidaMisc.AddItem(new MenuItem("usebork", "Use Botrk")).SetValue(true);
            nidaMisc.AddItem(new MenuItem("usebw", "Use Bilgewater")).SetValue(true);
            nidaMisc.AddItem(new MenuItem("useclaim", "Frost Queens")).SetValue(true);
            nidaMisc.AddItem(new MenuItem(" ", " "));
            nidaMisc.AddItem(new MenuItem("useks", "Killsteal")).SetValue(true);
            nidaMisc.AddItem(new MenuItem("swfks", "KS Switch Forms")).SetValue(false);
            Config.AddSubMenu(nidaMisc);

            var nidaD = new Menu("Nidalee: Drawings", "drawings");
            nidaD.AddItem(new MenuItem("drawQ", "Draw Q")).SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            nidaD.AddItem(new MenuItem("drawW", "Draw W")).SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            nidaD.AddItem(new MenuItem("drawE", "Draw E")).SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            nidaD.AddItem(new MenuItem("drawcds", "Draw Cooldowns")).SetValue(true);
            Config.AddSubMenu(nidaD);

            Config.AddItem(new MenuItem("useignote", "Use Ignite")).SetValue(true);
            Config.AddItem(new MenuItem("usepackets", "Use Packets")).SetValue(true);
            Config.AddToMainMenu();

            Game.PrintChat("<font color=\"#FFAF4D\">[</font><font color=\"#FFA333\">Nidalee</font><font color=\"#FFAF4D\">]</font><font color=\"#FF8C00\"> - <u>the Bestial Huntress Rev106</u>  </font>- Kurisu");

        }
        #endregion

        #region Nidalee: OnTick
        private void NidaleeOnUpdate(EventArgs args)
        {
            Kitty = spellData.Name != "JavelinToss";
            Target = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Magical);


            ProcessCooldowns();
            PrimalSurge();
            Killsteal();

            if (Target != null)
            {
                if (Me.Distance(Target.Position) < 700f)
                    hc = HitChance.Medium;
                if (Me.Distance(Target.Position) > 700f)
                    hc = HitChance.High;
                if (Me.Distance(Target.Position) < 300f)
                    hc = HitChance.Low;
            }

            if (Target != null && !Kitty)
                if (Target.Distance(Me) < 650f && TargetHunted(Target) && Orbwalker.ActiveMode.ToString() == "Combo")
                    if (Config.Item("usecougarr").GetValue<bool>())
                        aspectofcougar.Cast();

            switch (Orbwalker.ActiveMode.ToString())
            {
                case "Combo":
                    UseCombo(Target);
                    break;
                case "Mixed":
                    UseHarass(Target);
                    break;
                case "LaneClear":
                    UseLaneclear();
                    UseJungleclear();
                    break;
            }
        }
        #endregion

        #region Nidalee: SBTW
        private void UseCombo(Obj_AI_Base target)
        {
            SpellSlot ignote = Me.GetSpellSlot("summonerdot");
            float minPounce = Config.Item("pouncerange").GetValue<Slider>().Value;

            if (Kitty)
            {
                // dfg, botrk, hydra, tiamat
                if ((Items.CanUseItem(3128) && Items.HasItem(3128) || Items.CanUseItem(3144) && Items.HasItem(3144) ||
                     Items.CanUseItem(3153) && Items.HasItem(3153)) && TargetHunted(target) && pounce.IsReady() && ComboDamage(target) > target.Health)
                {
                    if (Config.Item("usedfg").GetValue<bool>())
                        Items.UseItem(3128, target);
                    if (Config.Item("useignote").GetValue<bool>())
                        Me.SummonerSpellbook.CastSpell(ignote, target);
                    if (Config.Item("usebork").GetValue<bool>())
                        Items.UseItem(3153);
                    if (Config.Item("usebw").GetValue<bool>())
                        Items.UseItem(3144);
                }
                else if (TargetHunted(target) && pounce.IsReady() && ComboDamage(target) > target.Health)
                {
                    if (Config.Item("useignote").GetValue<bool>())
                        Me.SummonerSpellbook.CastSpell(ignote, target);
                }

                // frost claim
                if (Items.CanUseItem(3092) && Items.HasItem(3092) && Config.Item("useclaim").GetValue<bool>())
                    Items.UseItem(3092, target.Position);
                if (takedown.IsReady() && Config.Item("usecougarq").GetValue<bool>() && target.Distance(Me.Position) < takedown.Range)
                    takedown.Cast(target, Packets());
                if (pounce.IsReady() && Config.Item("usecougarw").GetValue<bool>() && target.Distance(Me.Position) < 750f && target.Distance(Me.Position) > minPounce)
                    pounce.Cast(target.Position, Packets());
                if (swipe.IsReady() && Config.Item("usecougare").GetValue<bool>())
                {
                    var prediction = swipe.GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.High && target.Distance(Me.Position) <= swipe.Range)
                        swipe.Cast(prediction.CastPosition, Packets());
                }
                if (target.Distance(Me.Position) > pounce.Range && Config.Item("usecougarr").GetValue<bool>())
                    aspectofcougar.Cast();
                if (!pounce.IsReady() && javelin.IsReady() && target.Distance(Me.Position) < pounce.Range && Config.Item("usecougarr").GetValue<bool>())
                    aspectofcougar.Cast();
            }
            else
            {
                if (javelin.IsReady() && Config.Item("usehumanq").GetValue<bool>())
                {
                    var prediction = javelin.GetPrediction(target);
                    if (prediction.Hitchance == hc && target.Distance(Me.Position) < javelin.Range)
                        javelin.Cast(prediction.CastPosition, true);
                }

                if (bushwack.IsReady() && Config.Item("usehumanw").GetValue<bool>() && target.Distance(Me.Position) <= bushwack.Range)
                    bushwack.Cast(target.Position, Packets());
            }
        }
        #endregion

        #region Nidalee: Harass
        private void UseHarass(Obj_AI_Base target)
        {
            var actualHeroManaPercent = (int)((Me.Mana / Me.MaxMana) * 100);
            var minPercent = Config.Item("humanqpct").GetValue<Slider>().Value;
            if (javelin.IsReady() && Config.Item("usehumanq2").GetValue<bool>())
            {
                var prediction = javelin.GetPrediction(target);
                if (prediction.Hitchance == hc && target.Distance(Me.Position) <= javelin.Range && actualHeroManaPercent > minPercent)
                    javelin.Cast(prediction.CastPosition, true);
            }
        }

        #endregion

        #region Nidalee: HealEngine
        private void PrimalSurge()
        {
            var actualHeroManaPercent = (int)((Me.Mana / Me.MaxMana) * 100);
            var selfManaPercent = Config.Item("healmanapct").GetValue<Slider>().Value;
            foreach (
                var hero in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            hero =>
                                hero.IsAlly && hero.Distance(Me.Position) < primalsurge.Range && !hero.IsDead &&
                                hero.IsValid && hero.IsVisible)) 
            {

                if (Config.Item("heal" + hero.SkinName).GetValue<bool>())
                {
                    var needed = Config.Item("healpct" +hero.SkinName).GetValue<Slider>().Value;
                    var hp = (int)((hero.Health / hero.MaxHealth) * 100);
                    if (actualHeroManaPercent > selfManaPercent && !Kitty && hp < needed)
                        primalsurge.CastOnUnit(hero, Packets());
                }
            }
        }

        #endregion

        #region Nidalee: Jungleclear
        private void UseJungleclear()
        {
            var actualHeroManaPercent = (int)((Me.Mana / Me.MaxMana) * 100);
            var minPercent = Config.Item("jgrpct").GetValue<Slider>().Value;

            foreach (
                var m in
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            m =>
                                m.Distance(Me) < 1500f && m.IsEnemy && m.IsValid && m.IsVisible &&
                                JungleMinions.Any(name => m.Name.StartsWith(name)))) 
            {
                if (Kitty)
                {
                    if (Config.Item("jgcougare").GetValue<bool>() && m.Distance(Me.Position) < swipe.Range)
                        swipe.Cast(m.Position);
                    if (Config.Item("jgcougarw").GetValue<bool>() && m.Distance(Me.Position) < pounce.Range)
                        pounce.Cast(m.Position);
                    if (Config.Item("jgcougarq").GetValue<bool>() && m.Distance(Me.Position) < takedown.Range)
                        takedown.Cast(m);
                }
                else
                {
                    if (Config.Item("jghumanq").GetValue<bool>() && actualHeroManaPercent > minPercent)
                        javelin.Cast(m.Position);
                    if (Config.Item("jghumanw").GetValue<bool>() && m.Distance(Me.Position) < bushwack.Range && actualHeroManaPercent > minPercent)
                        bushwack.Cast(m.Position);
                    if (!javelin.IsReady() && Config.Item("jgcougarr").GetValue<bool>() && m.Distance(Me.Position) < pounce.Range && actualHeroManaPercent > minPercent)
                        aspectofcougar.Cast();
                }
            }
        }

        #endregion

        #region Nidalee: Laneclear
        private void UseLaneclear()
        {
            var actualHeroManaPercent = (int)((Me.Mana / Me.MaxMana) * 100);
            var minPercent = Config.Item("clearpct").GetValue<Slider>().Value;

            foreach (
                var m in
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            m =>
                                m.Distance(Me.Position) < 1500f && m.IsEnemy && !m.IsDead &&
                                JungleMinions.Any(name => !m.Name.StartsWith(name)))) 
            {
                if (Kitty)
                {
                    if (Config.Item("clearcougare").GetValue<bool>() && m.Distance(Me.Position) < swipe.Range)
                        swipe.Cast(m);
                    if (Config.Item("clearcougarw").GetValue<bool>() && m.Distance(Me.Position) < pounce.Range)
                        pounce.Cast(m.Position);
                    if (Config.Item("clearcougarq").GetValue<bool>() && m.Distance(Me.Position) < takedown.Range)
                        takedown.Cast(m);
                }
                else
                {
                    if (Config.Item("clearhumanq").GetValue<bool>() && actualHeroManaPercent > minPercent)
                        javelin.Cast(m.Position);
                    if ((!javelin.IsReady() || !Config.Item("clearhumanq").GetValue<bool>()) && Config.Item("clearcougarr").GetValue<bool>() && m.Distance(Me.Position) < pounce.Range)
                        aspectofcougar.Cast();
                }
            }
        }
        #endregion

        #region Nidalee: Killsteal
        private void Killsteal()
        {
            if (!Config.Item("useks").GetValue<bool>()) return;
            foreach (
                var e in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            e =>
                                e.Distance(Me.Position) < 1500f && e.IsEnemy && !e.IsDead && e.IsValid &&
                                e.IsValid)) 
            {
                var qdmg = Me.GetSpellDamage(e, SpellSlot.Q);
                var wdmg = Me.GetSpellDamage(e, SpellSlot.W);
                var edmg = Me.GetSpellDamage(e, SpellSlot.E);


                if (takedown.IsReady() && e != null && e.Health < qdmg && e.Distance(Me.Position) < takedown.Range)
                    takedown.Cast(e, Packets());
                if (javelin.IsReady() && e != null && e.Health < qdmg)
                {
                    var javelinPrediction = javelin.GetPrediction(e);
                    if (javelinPrediction.Hitchance == hc)
                        javelin.Cast(javelinPrediction.CastPosition, Packets());
                }
                if (pounce.IsReady() && e != null && e.Health < wdmg && e.Distance(Me.Position) < pounce.Range)
                    pounce.Cast(e.Position, Packets());
                if (swipe.IsReady() && e != null && e.Health < edmg && e.Distance(Me.Position) < swipe.Range)
                    swipe.Cast(e.Position, Packets());
                if (javelin.IsReady() && e.Health < qdmg  && e.Distance(Me.Position) <= javelin.Range &&
                    Config.Item("swfks").GetValue<bool>())
                    aspectofcougar.Cast();
            }
        }

        #endregion

        #region Nidalee: Tracker

        // timer trackers credits to detuks
        private void NidaleeTracker(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                GetCooldowns(args);
        }

        private static readonly float[] humanQcd = { 6, 6, 6, 6, 6 };
        private static readonly float[] humanWcd = { 13, 12, 11, 10, 9 };
        private static readonly float[] humanEcd = { 12, 12, 12, 12, 12 };
        private static readonly float[] cougarQcd, cougarWcd, cougarEcd = { 5, 5, 5, 5, 5 };

        private static float CQRem, CWRem, CERem;
        private static float HQRem, HWRem, HERem;
        private static float CQ, CW, CE;
        private static float HQ, HW, HE;

        private void ProcessCooldowns()
        {
            CQ = ((CQRem - Game.Time) > 0) ? (CQRem - Game.Time) : 0;
            CW = ((CWRem - Game.Time) > 0) ? (CWRem - Game.Time) : 0;
            CE = ((CERem - Game.Time) > 0) ? (CERem - Game.Time) : 0;
            HQ = ((HQRem - Game.Time) > 0) ? (HQRem - Game.Time) : 0;
            HW = ((HWRem - Game.Time) > 0) ? (HWRem - Game.Time) : 0;
            HE = ((HERem - Game.Time) > 0) ? (HERem - Game.Time) : 0;
        }

        private static float CalculateCd(float time)
        {
            return time + (time * Me.PercentCooldownMod);
        }

        private void GetCooldowns(GameObjectProcessSpellCastEventArgs spell)
        {
            if (Kitty)
            {
                if (spell.SData.Name == "Takedown")
                    CQRem = Game.Time + CalculateCd(cougarQcd[javelin.Level]);
                if (spell.SData.Name == "Pounce")
                    CWRem = Game.Time + CalculateCd(cougarWcd[bushwack.Level]);
                if (spell.SData.Name == "Swipe")
                    CERem = Game.Time + CalculateCd(cougarEcd[primalsurge.Level]);
            }
            else
            {
                if (spell.SData.Name == "JavelinToss")
                    HQRem = Game.Time + CalculateCd(humanQcd[javelin.Level]);
                if (spell.SData.Name == "Bushwhack")
                    HWRem = Game.Time + CalculateCd(humanWcd[bushwack.Level]);
                if (spell.SData.Name == "PrimalSurge")
                    HERem = Game.Time + CalculateCd(humanEcd[primalsurge.Level]);
            }
        }

        #endregion

        #region Nidalee: DamageLib
        private static float ComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;
            var ignote = Me.GetSpellSlot("summonderdot");

            if (takedown.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.Q);
            if (swipe.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.E);
            if (pounce.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.W);
            if (javelin.IsReady() && !Kitty)
                damage += Me.GetSpellDamage(enemy, SpellSlot.Q);
            if (Me.SummonerSpellbook.CanUseSpell(ignote) == SpellState.Ready )
                damage += Me.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
            if (Items.HasItem(3128) && Items.CanUseItem(3128))
                damage += Me.GetItemDamage(enemy, Damage.DamageItems.Dfg); 
            if (Items.HasItem(3153) && Items.CanUseItem(3153))
                damage += Me.GetItemDamage(enemy, Damage.DamageItems.Botrk);
            if (Items.HasItem(3144) && Items.CanUseItem(3144))
                damage += Me.GetItemDamage(enemy, Damage.DamageItems.Bilgewater);
            return (float)damage;

        }

        #endregion

        #region Nidalee: On Draw
        private void NidaleeOnDraw(EventArgs args)
        {
            if (Target != null) Utility.DrawCircle(Target.Position, Target.BoundingRadius, Color.Red, 1, 1);

            foreach (var spell in cougarList)
            {
                var circle = Config.Item("draw" + spell.Slot.ToString()).GetValue<Circle>();
                if (circle.Active && Kitty && !Me.IsDead)
                    Utility.DrawCircle(Me.Position, spell.Range, circle.Color, 1, 1);
            }

            foreach (var spell in humanList)
            {
                var circle = Config.Item("draw" + spell.Slot.ToString()).GetValue<Circle>();
                if (circle.Active && !Kitty && !Me.IsDead)
                    Utility.DrawCircle(Me.Position, spell.Range, circle.Color, 1, 1);
            }

            if (!Config.Item("drawcds").GetValue<bool>()) return;

            Vector2 wts = Drawing.WorldToScreen(Me.Position);
            if (!Kitty) // lets show cooldown timers for the opposite form :)
            {
                if (CQ == 0)
                    Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q Ready");
                else
                    Drawing.DrawText(wts[0] - 80, wts[1], Color.Orange, "Q: " + CQ.ToString("0.0"));
                if (CW == 0)
                    Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W Ready");
                else
                    Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.Orange, "W: " + CW.ToString("0.0"));
                if (CE == 0)
                    Drawing.DrawText(wts[0], wts[1], Color.White, "E Ready");
                else
                    Drawing.DrawText(wts[0], wts[1], Color.Orange, "E: " + CE.ToString("0.0"));

            }
            else
            {
                if (HQ == 0)
                    Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q Ready");
                else
                    Drawing.DrawText(wts[0] - 80, wts[1], Color.Orange, "Q: " + HQ.ToString("0.0"));
                if (HW == 0)
                    Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W Ready");
                else
                    Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.Orange, "W: " + HW.ToString("0.0"));
                if (HE == 0)
                    Drawing.DrawText(wts[0], wts[1], Color.White, "E Ready");
                else
                    Drawing.DrawText(wts[0], wts[1], Color.Orange, "E: " + HE.ToString("0.0"));

            }
        }
        #endregion
    }
}
