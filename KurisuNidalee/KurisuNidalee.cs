using LeagueSharp;
using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

namespace KurisuNidalee
{
    internal class KurisuNidalee
    {
        public KurisuNidalee()
        {
            Console.WriteLine("Kurisu assembly is loading...");
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static Menu _config;
        private static Obj_AI_Base _target;
        private static Obj_AI_Hero _player = ObjectManager.Player;
        private static Orbwalking.Orbwalker _orbwalker;
        private static bool isCougar = false;


        // Spells
        private static Spell javelin = new Spell(SpellSlot.Q, 1500f);
        private static Spell bushwack = new Spell(SpellSlot.W, 900f);
        private static Spell primalsurge = new Spell(SpellSlot.E, 650f);
        private static Spell takedown = new Spell(SpellSlot.Q, 200f);
        private static Spell pounce = new Spell(SpellSlot.W, 375f);
        private static Spell swipe = new Spell(SpellSlot.E, 300f);
        private static Spell aspectofcougar = new Spell(SpellSlot.R, float.MaxValue);

        private static SpellDataInst qdata = _player.Spellbook.GetSpell(SpellSlot.Q);
        private static List<Spell> cougarList = new List<Spell>();
        private static List<Spell> humanList = new List<Spell>();

        // Cooldown Stuff
        private static float[] humanQcd = { 6, 6, 6, 6, 6 };
        private static float[] humanWcd = { 13, 12, 11, 10, 9 };
        private static float[] humanEcd = { 12, 12, 12, 12, 12 };

        private static float[] cougarQcd = { 5, 5, 5, 5, 5 };
        private static float[] cougarWcd = { 5, 5, 5, 5, 5 };
        private static float[] cougarEcd = { 5, 5, 5, 5, 5 };

        private void InitSpells()
        {
            // Setup our drawings
            cougarList.AddRange(new[] { takedown, pounce, swipe });
            humanList.AddRange(new[] { javelin, bushwack, primalsurge });

            // Setup prediction
            javelin.SetSkillshot(0.50f, 70f, 1300f, true, SkillshotType.SkillshotLine);
            bushwack.SetSkillshot(0.50f, 100f, 1500f, false, SkillshotType.SkillshotCircle);
            swipe.SetSkillshot(0.50f, 375f, 1500f, false, SkillshotType.SkillshotCone);
            pounce.SetSkillshot(0.50f, 400f, 1500f, false, SkillshotType.SkillshotCone);
        }

        #region OnGameLoad
        private void Game_OnGameLoad(EventArgs args)
        {
            try
            {
                _config = new Menu("Kurisu: Nidaleee", "nidalee", true);

                Menu nidaOrb = new Menu("Orbwalker", "orbwalker");
                _orbwalker = new Orbwalking.Orbwalker(nidaOrb);
                _config.AddSubMenu(nidaOrb);

                Menu nidaTS = new Menu("Target Selector", "target selecter");
                SimpleTs.AddToMenu(nidaTS);
                _config.AddSubMenu(nidaTS);

                Menu nidaDraws = new Menu("Nidalee: Drawings", "drawings");
                nidaDraws.AddItem(new MenuItem("drawQ", "Draw Q")).SetValue(new Circle(true, Color.FromArgb(150, Color.OrangeRed)));
                nidaDraws.AddItem(new MenuItem("drawW", "Draw W")).SetValue(new Circle(true, Color.FromArgb(150, Color.OrangeRed)));
                nidaDraws.AddItem(new MenuItem("drawE", "Draw E")).SetValue(new Circle(true, Color.FromArgb(150, Color.DarkOrange)));
                nidaDraws.AddItem(new MenuItem(" ", " "));
                nidaDraws.AddItem(new MenuItem("drawT", "Draw line current target")).SetValue(true);
                _config.AddSubMenu(nidaDraws);

                Menu nidaSpells = new Menu("Nidalee: Spells", "spells");
                nidaSpells.AddItem(new MenuItem("usehumanq", "Use Javelin Toss")).SetValue(true);
                nidaSpells.AddItem(new MenuItem("usehumanw", "Use Bushwack")).SetValue(true);
                nidaSpells.AddItem(new MenuItem(" ", " "));
                nidaSpells.AddItem(new MenuItem("usecougarq", "Use Takedown")).SetValue(true);
                nidaSpells.AddItem(new MenuItem("usecougarw", "Use Pounce")).SetValue(true);
                nidaSpells.AddItem(new MenuItem("usecougare", "Use Swipe")).SetValue(true);
                nidaSpells.AddItem(new MenuItem("usecougarr", "Auto Switch Forms")).SetValue(true);
                _config.AddSubMenu(nidaSpells);

                Menu nidaHarass = new Menu("Nidalee: Harass", "harass");
                nidaHarass.AddItem(new MenuItem("usehumanq2", "Use Javelin Toss")).SetValue(true);
                nidaHarass.AddItem(new MenuItem("humanqpct", "Minimum Mana %")).SetValue(new Slider(70, 0, 100));
                _config.AddSubMenu(nidaHarass);

                Menu nidaClear = new Menu("Nidalee: Laneclear", "laneclear");
                nidaClear.AddItem(new MenuItem("clearhumanq", "Use Javelin")).SetValue(false);
                nidaClear.AddItem(new MenuItem(" ", " "));
                nidaClear.AddItem(new MenuItem("clearcougarq", "Use Takedown")).SetValue(true);
                nidaClear.AddItem(new MenuItem("clearcougarw", "Use Pounce")).SetValue(true);
                nidaClear.AddItem(new MenuItem("clearcougare", "Use Swipe")).SetValue(true);
                nidaClear.AddItem(new MenuItem("clearcougarr", "Auto Switch Forms")).SetValue(false);
                nidaClear.AddItem(new MenuItem("clearpct", "Minimum Mana %")).SetValue(new Slider(55, 0, 100));
                _config.AddSubMenu(nidaClear);

                Menu nidaJungle = new Menu("Nidalee: Jungleclear", "jungleclear");
                nidaJungle.AddItem(new MenuItem("jghumanq", "Use Javelin Toss")).SetValue(true);
                nidaJungle.AddItem(new MenuItem("jghumanw", "Use Bushwack")).SetValue(true);
                nidaJungle.AddItem(new MenuItem(" ", " "));
                nidaJungle.AddItem(new MenuItem("jgcougarq", "Use Takedown")).SetValue(true);
                nidaJungle.AddItem(new MenuItem("jgcougarw", "Use Pounce")).SetValue(true);
                nidaJungle.AddItem(new MenuItem("jgcougare", "Use Swipe")).SetValue(true);
                nidaJungle.AddItem(new MenuItem("jgcougarr", "Auto Switch Forms")).SetValue(true);
                nidaJungle.AddItem(new MenuItem("jgrpct", "Minimum Mana %")).SetValue(new Slider(55, 0, 100));
                _config.AddSubMenu(nidaJungle);

                Menu nidaMisc = new Menu("Nidalee: Misc", "nidamisc");
                nidaMisc.AddItem(new MenuItem("useignote", "Use Ignite")).SetValue(true);
                nidaMisc.AddItem(new MenuItem("usedfg", "Use DFG")).SetValue(true);
                nidaMisc.AddItem(new MenuItem("usebork", "Use Botrk")).SetValue(true);
                nidaMisc.AddItem(new MenuItem("usebw", "Use Bilgewater")).SetValue(true);
                nidaMisc.AddItem(new MenuItem(" ", " "));
                nidaMisc.AddItem(new MenuItem("usepackets", "Use Packets")).SetValue(true);
                nidaMisc.AddItem(new MenuItem("useks", "Killsteal")).SetValue(false);
                nidaMisc.AddItem(new MenuItem("swfks", "KS Switch Forms")).SetValue(false);
                _config.AddSubMenu(nidaMisc);

                // Add our menu to Main Menu
                _config.AddToMainMenu();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Game.PrintChat("Something went wrong with the Nidalee assembly(OnGameLoad)");
            }

            InitSpells();
            #region L# Reqs
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            #endregion

        }
        #endregion

        private bool TargetHunted(Obj_AI_Base target)
        {
            return target.HasBuff("nidaleepassivehunted", true);
        }

        private bool UsePacket()
        {
            return _config.Item("usepackets").GetValue<bool>();
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            useKS();
            InitCooldowns();
            _target = SimpleTs.GetTarget(1500, SimpleTs.DamageType.Magical);

            if (qdata.Name == "JavelinToss")
                isCougar = false;
            else
                isCougar = true;

            if (_orbwalker.ActiveMode.ToString() == "Combo")
                useCombo(_target);

            if (_orbwalker.ActiveMode.ToString() == "LaneClear")
            {
                useLaneclear(); // checks for low minion health
                useJungleclear(); // checks for jungle mobs at any health
            }
            if (!isCougar && _target != null && _target.Distance(_player) < 650f && TargetHunted(_target) && _orbwalker.ActiveMode.ToString() == "Combo")
            {
                if (_config.Item("usecougarr").GetValue<bool>())
                    aspectofcougar.Cast();
            }
            Utility.DrawCircle(_target.Position, _target.BoundingRadius, Color.DarkOrange, 4, 40);

        }

        private void Game_OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                GetCooldowns(args);
        }

        private IEnumerable<Obj_AI_Minion> getLaneMinions(Spell spell)
        {
            string[] jungleMinions = { "AncientGolem", "GreatWraith", "Wraith", "LizardElder", "Golem", "Worm", "Dragon", "GiantWolf" };
            var minionList = from minion in ObjectManager.Get<Obj_AI_Minion>()
                             where minion.Distance(_player.Position) < spell.Range &&
                                   minion.IsEnemy && !minion.IsDead && jungleMinions.Any(name => !minion.Name.StartsWith(name)) &&
                                  (minion.Health < DamageLib.getDmg(minion, DamageLib.SpellType.E) && _config.Item("clearcougare").GetValue<bool>() ||
                                   minion.Health < DamageLib.getDmg(minion, DamageLib.SpellType.W) && _config.Item("clearcougarw").GetValue<bool>() ||
                                   minion.Health < DamageLib.getDmg(minion, DamageLib.SpellType.Q) && _config.Item("clearcougarq").GetValue<bool>())
                             select minion;

            return minionList;
        }

        private IEnumerable<Obj_AI_Hero> getEnemies(Spell spell)
        {
            var heroList = from hero in ObjectManager.Get<Obj_AI_Hero>()
                           where hero.Distance(_player.Position) < spell.Range && hero.IsEnemy && !hero.IsDead &&
                                 hero.IsValid && hero.IsValid
                           select hero;
            return heroList;
        }

        private IEnumerable<Obj_AI_Minion> getJungleMinions(Spell spell)
        {
            string[] jungleMinions = { "AncientGolem", "GreatWraith", "Wraith", "LizardElder", "Golem", "Worm", "Dragon", "GiantWolf" };
            var jungleList = from minion in ObjectManager.Get<Obj_AI_Minion>()
                             where minion.Distance(_player) < spell.Range &&
                                   minion.IsEnemy && minion.IsValid && minion.IsVisible &&
                                   jungleMinions.Any(name => minion.Name.StartsWith(name))
                             select minion;
            return jungleList;
        }

        private void useCombo(Obj_AI_Base target)
        {
            SpellSlot ignote = Utility.GetSpellSlot(_player, "summonerdot");
            if (isCougar)
            {
                if ((Items.CanUseItem(3128) && Items.HasItem(3128) || Items.CanUseItem(3144) && Items.HasItem(3144) ||
                     Items.CanUseItem(3153) && Items.HasItem(3153)) && TargetHunted(target) && ComboDamage(target) > target.Health)
                {
                    if (_config.Item("usedfg").GetValue<bool>())
                        Items.UseItem(3128, target);
                    if (_config.Item("useignote").GetValue<bool>())
                        _player.SummonerSpellbook.CastSpell(ignote, target);
                    if (_config.Item("usebork").GetValue<bool>())
                        Items.UseItem(3153);
                    if (_config.Item("usebw").GetValue<bool>())
                        Items.UseItem(3144);
                }

                if (takedown.IsReady() && _config.Item("usecougarq").GetValue<bool>() && target.Distance(_player.Position) < takedown.Range)
                    takedown.Cast(target, UsePacket());
                if (pounce.IsReady() && _config.Item("usecougarw").GetValue<bool>() && target.Distance(_player.Position) < pounce.Range)
                    pounce.Cast(target.Position, UsePacket());
                if (swipe.IsReady() && _config.Item("usecougare").GetValue<bool>())
                {
                    PredictionOutput prediction = swipe.GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.High && target.Distance(_player.Position) <= swipe.Range)
                        swipe.Cast(prediction.CastPosition, UsePacket());
                }
                PredictionOutput javelinpo = javelin.GetPrediction(target);
                if (target.Distance(_player.Position) > pounce.Range)
                    aspectofcougar.Cast();
            }
            else
            {
                if (javelin.IsReady() && _config.Item("usehumanq").GetValue<bool>())
                {
                    PredictionOutput prediction = javelin.GetPrediction(target);
                    if (prediction.Hitchance == HitChance.High && target.Distance(_player.Position) < javelin.Range)
                        javelin.Cast(prediction.CastPosition, true);
                }

                if (bushwack.IsReady() && _config.Item("usehumanw").GetValue<bool>() && target.Distance(_player.Position) <= bushwack.Range)
                    bushwack.Cast(target.Position, UsePacket());
            }
        }

        private void useHarass(Obj_AI_Base target)
        {
            int manaPercent = ((int)_player.Mana / (int)_player.MaxMana) * _config.Item("humanqpct").GetValue<Slider>().Value;
            if (javelin.IsReady() && _config.Item("usehumanq2").GetValue<bool>())
            {
                // add mana setting
                PredictionOutput prediction = javelin.GetPrediction(target);
                if (prediction.Hitchance >= HitChance.Medium && target.Distance(_player.Position) <= javelin.Range)
                    javelin.Cast(prediction.CastPosition, true);
            }
        }

        private void useForce(Obj_AI_Base target) // luke
        {
            // TODO:
            // 
            // check aggro and force cougar on another target if priority target is not in range

        }

        private void useJungleclear()
        {
            int actualHeroManaPercent = (int)((_player.Mana / _player.MaxMana) * 100);
            int minPercent = _config.Item("jgrpct").GetValue<Slider>().Value;
            var jmobs = getJungleMinions(javelin);
            if (jmobs.Count() > 0)
            {
                foreach (Obj_AI_Minion m in jmobs)
                {
                    if (isCougar)
                    {
                        if (_config.Item("jgcougare").GetValue<bool>() && m.Distance(_player.Position) < swipe.Range)
                            swipe.Cast(m.Position);
                        if (_config.Item("jgcougarw").GetValue<bool>() && m.Distance(_player.Position) < pounce.Range)
                            pounce.Cast(m.Position);
                        if (_config.Item("jgcougarq").GetValue<bool>() && m.Distance(_player.Position) < takedown.Range)
                            takedown.Cast(m);
                    }
                    else
                    {
                        if (_config.Item("jghumanq").GetValue<bool>() && actualHeroManaPercent > minPercent)
                            javelin.Cast(m.Position);
                        if (_config.Item("jghumanw").GetValue<bool>() && m.Distance(_player.Position) < bushwack.Range && actualHeroManaPercent > minPercent)
                            bushwack.Cast(m.Position);
                        if (!javelin.IsReady() && _config.Item("jgcougarr").GetValue<bool>() && m.Distance(_player.Position) < pounce.Range && actualHeroManaPercent > minPercent)
                            aspectofcougar.Cast();
                    }
                }
            }
        }

        private void useLaneclear()
        {
            int actualHeroManaPercent = (int)((_player.Mana / _player.MaxMana) * 100);
            int minPercent = _config.Item("clearpct").GetValue<Slider>().Value;
            var minions = getLaneMinions(javelin);
            if (minions.Count() > 0)
            {
                foreach (Obj_AI_Minion m in minions)
                {
                    if (isCougar)
                    {
                        if (_config.Item("clearcougare").GetValue<bool>() && m.Distance(_player.Position) < swipe.Range)
                            swipe.Cast(m);
                        if (_config.Item("clearcougarw").GetValue<bool>() && m.Distance(_player.Position) < pounce.Range)
                            pounce.Cast(m.Position);
                        if (_config.Item("clearcougarq").GetValue<bool>() && m.Distance(_player.Position) < takedown.Range)
                            takedown.Cast(m);
                    }
                    else
                    {
                        if (_config.Item("clearhumanq").GetValue<bool>() && actualHeroManaPercent > minPercent)
                            javelin.Cast(m.Position);
                        if ((!javelin.IsReady() || !_config.Item("clearhumanq").GetValue<bool>()) && _config.Item("clearcougarr").GetValue<bool>() && m.Distance(_player.Position) < pounce.Range)
                            aspectofcougar.Cast();
                    }
                }
            }
        }

        private void useKS()
        {
            if (_config.Item("useks").GetValue<bool>())
            {
                foreach (Obj_AI_Hero e in getEnemies(javelin)) // get enemies within javelin range
                {
                    // should get both form q damage
                    var qdmg = DamageLib.getDmg(e, DamageLib.SpellType.Q);
                    var wdmg = DamageLib.getDmg(e, DamageLib.SpellType.W);
                    var edmg = DamageLib.getDmg(e, DamageLib.SpellType.E);

                    if (takedown.IsReady() && e != null && e.Health < qdmg && e.Distance(_player.Position) < takedown.Range)
                        takedown.Cast(e, UsePacket());
                    if (javelin.IsReady() && e != null && e.Health < qdmg)
                    {
                        PredictionOutput javelinPrediction = javelin.GetPrediction(e);
                        if (javelinPrediction.Hitchance == HitChance.High)
                            javelin.Cast(javelinPrediction.CastPosition, UsePacket());
                    }                       
                    if (pounce.IsReady() && e != null && e.Health < wdmg && e.Distance(_player.Position) < pounce.Range)
                        pounce.Cast(e.Position, UsePacket());
                    if (swipe.IsReady() && e != null && e.Health < edmg && e.Distance(_player.Position) < swipe.Range)
                        swipe.Cast(e.Position, UsePacket());
                    if (javelin.IsReady() && e.Health < qdmg && e.Distance(_player.Position) <= javelin.Range && _config.Item("swfks").GetValue<bool>())
                        aspectofcougar.Cast();
                }
            }
        }

        private static float catQcd = 0, catWcd = 0, catEcd = 0;
        private static float humQcd = 0, humWcd = 0, humEcd = 0;
        private static float _catQcd = 0, _catWcd = 0, _catEcd = 0;
        private static float _humQcd = 0, _humWcd = 0, _humEcd = 0;

        private void InitCooldowns()
        {
            _catQcd = ((catQcd - Game.Time) > 0) ? (catQcd - Game.Time) : 0;
            _catWcd = ((catWcd - Game.Time) > 0) ? (catWcd - Game.Time) : 0;
            _catEcd = ((catEcd - Game.Time) > 0) ? (catEcd - Game.Time) : 0;
            _humQcd = ((humQcd - Game.Time) > 0) ? (humQcd - Game.Time) : 0;
            _humWcd = ((humWcd - Game.Time) > 0) ? (humWcd - Game.Time) : 0;
            _humEcd = ((humEcd - Game.Time) > 0) ? (humEcd - Game.Time) : 0;
        }

        private static float CalculateCD(float time)
        {
            return time + (time * _player.PercentCooldownMod);
        }

        private static float ComboDamage(Obj_AI_Base enemy)
        {
            // TODO:
            // get comdo damage
            // calculate combo damage based on hunted target and spear range
            var damage = 0d;
            var ignote = Utility.GetSpellSlot(_player, "summonderdot");

            if (takedown.IsReady())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.Q);
            if (pounce.IsReady())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.W);
            if (swipe.IsReady())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.E);
            if (javelin.IsReady() && !isCougar)
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.Q);
            if (ignote != SpellSlot.Unknown && _config.Item("useignite").GetValue<bool>())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.IGNITE);
            if (Items.HasItem(3128) && Items.CanUseItem(3128) && _config.Item("usedfg").GetValue<bool>())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.DFG);
            if (Items.HasItem(3128) && Items.CanUseItem(3128) && _config.Item("usedfg").GetValue<bool>())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.DFG);
            if (Items.HasItem(3153) && Items.CanUseItem(3153) && _config.Item("usebork").GetValue<bool>())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.BOTRK);
            if (Items.HasItem(3144) && Items.CanUseItem(3144) && _config.Item("usebw").GetValue<bool>())
                damage += DamageLib.getDmg(enemy, DamageLib.SpellType.BILGEWATER);
            return (float)damage;
        }

        private void GetCooldowns(GameObjectProcessSpellCastEventArgs spell)
        {
            if (isCougar)
            {
                if (spell.SData.Name == "Takedown")
                    catQcd = Game.Time + CalculateCD(cougarQcd[javelin.Level]);
                if (spell.SData.Name == "Pounce")
                    catWcd = Game.Time + CalculateCD(cougarWcd[bushwack.Level]);
                if (spell.SData.Name == "Swipe")
                    catEcd = Game.Time + CalculateCD(cougarEcd[primalsurge.Level]);
            }
            else
            {
                if (spell.SData.Name == "JavelinToss")
                    humQcd = Game.Time + CalculateCD(humanQcd[javelin.Level]);
                if (spell.SData.Name == "Bushwhack")
                    humWcd = Game.Time + CalculateCD(humanWcd[bushwack.Level]);
                if (spell.SData.Name == "PrimalSurge")
                    humEcd = Game.Time + CalculateCD(humanEcd[primalsurge.Level]);
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {

            foreach (var spell in cougarList)
            {
                var circle = _config.Item("draw" + spell.Slot.ToString()).GetValue<Circle>();
                if (circle.Active && isCougar && !_player.IsDead)
                    Utility.DrawCircle(_player.Position, spell.Range, circle.Color, 5, 55);
            }

            foreach (var spell in humanList)
            {
                var circle = _config.Item("draw" + spell.Slot.ToString()).GetValue<Circle>();
                if (circle.Active && !isCougar && !_player.IsDead)
                    Utility.DrawCircle(_player.Position, spell.Range, circle.Color, 5, 55);
            }

            var wts = Drawing.WorldToScreen(_player.Position);
            if (!isCougar) // lets show cooldown timers for the opposite form :)
            {
                if (_catQcd == 0)
                    Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q Ready");
                else
                    Drawing.DrawText(wts[0] - 80, wts[1], Color.Orange, "Q: " + _catQcd.ToString("0.0"));
                if (_catWcd == 0)
                    Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W Ready");
                else
                    Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.Orange, "W: " + _catWcd.ToString("0.0"));
                if (_catEcd == 0)
                    Drawing.DrawText(wts[0], wts[1], Color.White, "E Ready");
                else
                    Drawing.DrawText(wts[0], wts[1], Color.Orange, "E: " + _catEcd.ToString("0.0"));

            }
            else
            {
                if (_humQcd == 0)
                    Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q Ready");
                else
                    Drawing.DrawText(wts[0] - 80, wts[1], Color.Orange, "Q: " + _humQcd.ToString("0.0"));
                if (_humWcd == 0)
                    Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W Ready");
                else
                    Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.Orange, "W: " + _humWcd.ToString("0.0"));
                if (_humEcd == 0)
                    Drawing.DrawText(wts[0], wts[1], Color.White, "E Ready");
                else
                    Drawing.DrawText(wts[0], wts[1], Color.Orange, "E: " + _humEcd.ToString("0.0"));

            }

        }
    }
}