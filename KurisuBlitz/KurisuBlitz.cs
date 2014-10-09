﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace KurisuBlitz
{
    class KurisuBlitz
    {
        private static Menu _menu;
        private static Obj_AI_Hero _target;
        private static Orbwalking.Orbwalker _orbwalker;
        private static readonly Obj_AI_Hero _player = ObjectManager.Player;

        private static readonly Spell Q = new Spell(SpellSlot.Q, 925f);
        private static readonly Spell R = new Spell(SpellSlot.R, 550f);

        private static readonly List<Spell> blitzDrawingList = new List<Spell>();
        private static List<InterruptableSpell> blitzInterruptList = new List<InterruptableSpell>();

        public KurisuBlitz()
        {
            
            if (_player.BaseSkinName != "Blitzcrank") return;
            Console.WriteLine("Blitzcrank assembly is loading...");
            CustomEvents.Game.OnGameLoad += BlitzOnLoad;
        }

        private void BlitzOnLoad(EventArgs args)
        {
            // Set Q Prediction
            Q.SetSkillshot(0.25f, 70f, 1800f, true, SkillshotType.SkillshotLine);

            // Drawing List
            blitzDrawingList.Add(Q);
            blitzDrawingList.Add(R);

            // Load Menu
            BlitzMenu();

            // Load Drawings
            Drawing.OnDraw += BlitzOnDraw;

            // OnTick
            Game.OnGameUpdate += BlitzOnUpdate;

            // Interrupter
            Interrupter.OnPossibleToInterrupt += BlitzOnInterrupt;

            // OnGapCloser
            AntiGapcloser.OnEnemyGapcloser += BlitzOnGapcloser;
        }

        private void BlitzOnInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            blitzInterruptList.Add(spell);

            Game.PrintChat(spell.SpellName);
            if (_menu.Item("interrupt").GetValue<bool>())
            {
                
                if (unit.Distance(_player.Position) < R.Range)
                    R.Cast();
            }
        }

        private void BlitzOnDraw(EventArgs args)
        {
            foreach (var spell in blitzDrawingList)
            {
                var circle = _menu.SubMenu("drawings").Item("draw" + spell.Slot.ToString()).GetValue<Circle>();
                if (circle.Active)
                    Utility.DrawCircle(_player.Position, spell.Range, circle.Color, 5, 55);
            }
        }

        private void BlitzOnGapcloser(ActiveGapcloser gapcloser)
        {
            if (_menu.Item("gapcloser").GetValue<bool>())
            {
                Q.Cast(gapcloser.Sender);
            }
        }

        private void BlitzOnUpdate(EventArgs args)
        {
            try
            {
                // get target
                _target = SimpleTs.GetTarget(1000, SimpleTs.DamageType.Physical);

                // do KS
                GodKS();

                int actualHealthSetting = _menu.Item("hneeded").GetValue<Slider>().Value;
                int actualHealthPercent = (int) (_player.Health/_player.MaxHealth*100);

                if (actualHealthPercent < actualHealthSetting) return;

                // use the god hand
                TheGodHand(_target);

                // powerfist that hoe
                foreach (
                    var e in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                e =>
                                    e.Team != _player.Team && e.IsValid && !e.IsDead &&
                                    e.Distance(_player.Position) <= _player.AttackRange))
                {
                    if (_menu.Item("useE").GetValue<bool>())
                        _player.Spellbook.CastSpell(SpellSlot.E);
                }
            }
            catch (Exception ex)
            {
                Game.PrintChat(ex.Message);
                Console.WriteLine(ex);
            }

        }

        private void TheGodHand(Obj_AI_Base target)
        {
            bool keydown = _menu.Item("combokey").GetValue<KeyBind>().Active;

            if (target != null && Q.IsReady())
            {
                PredictionOutput prediction = Q.GetPrediction(target);
                if (keydown)
                {
                    if (_menu.Item("dograb" + target.SkinName).GetValue<StringList>().SelectedIndex == 0) return;
                    if (prediction.Hitchance == HitChance.High && _menu.Item("hitchance").GetValue<StringList>().SelectedIndex == 2)
                            Q.Cast(prediction.CastPosition);
                    else if (prediction.Hitchance == HitChance.Medium && _menu.Item("hitchance").GetValue<StringList>().SelectedIndex == 1)
                        Q.Cast(prediction.CastPosition);
                    else if (prediction.Hitchance == HitChance.Low && _menu.Item("hitchance").GetValue<StringList>().SelectedIndex == 0)
                        Q.Cast(prediction.CastPosition);      
                    
                }
            }

            foreach (
                   var e in
                       ObjectManager.Get<Obj_AI_Hero>()
                           .Where(
                               e =>
                                   e.Team != _player.Team && !e.IsDead && e.IsValid &&
                                   Vector2.DistanceSquared(_player.Position.To2D(), e.ServerPosition.To2D()) <
                                   Q.Range * Q.Range && _menu.Item("dograb" + e.SkinName).GetValue<StringList>().SelectedIndex == 2))
            {
                PredictionOutput prediction = Q.GetPrediction(e);
                if (prediction.Hitchance == HitChance.Immobile && _menu.Item("immobile").GetValue<bool>())
                    Q.Cast(prediction.CastPosition);
                if (prediction.Hitchance == HitChance.Dashing && _menu.Item("dashing").GetValue<bool>())
                    Q.Cast(prediction.CastPosition);
            }
        }

                        
        private void GodKS()
        {
            if (_menu.Item("killsteal").GetValue<bool>() && R.IsReady())
            {
                foreach (
                    var enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(e => e.Team != _player.Team && e.Distance(_player.Position) < R.Range))
                {
                    var rDmg = _player.GetSpellDamage(enemy, SpellSlot.R);
                    if (rDmg > enemy.Health)
                    {
                            R.Cast();
                    }

                }
            }
        }

        private void BlitzMenu()
        {
            _menu = new Menu("Kurisu: Blitz", "blitz", true);

            Menu blitzOrb = new Menu("Blitz: Orbwalker", "orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(blitzOrb);
            _menu.AddSubMenu(blitzOrb);
            
            Menu menuD = new Menu("Blitz: Drawings", "drawings");
            menuD.AddItem(new MenuItem("drawQ", "Draw Q")).SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            menuD.AddItem(new MenuItem("drawR", "Draw R")).SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            _menu.AddSubMenu(menuD);
            
            Menu menuG = new Menu("Blitz: GodHand", "autograb");
            menuG.AddItem(new MenuItem("hitchance", "Hitchance"))
                .SetValue(new StringList(new[] {"Low", "Medium", "High"}, 2));
            menuG.AddItem(new MenuItem("dashing", "Auto Q dashing enemies")).SetValue(true);
            menuG.AddItem(new MenuItem("immobile", "Auto Q immobile enemies")).SetValue(true);
            menuG.AddItem(new MenuItem("sep", ""));
            menuG.AddItem(new MenuItem("hneeded", "Dont grab below health %")).SetValue(new Slider(0));
            
            foreach (
                var e in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            e =>
                                e.Team != _player.Team))
            {
                menuG.AddItem(new MenuItem("dograb" + e.SkinName, e.SkinName))
                    .SetValue(new StringList(new[] {"Dont Grab ", "Normal Grab ", "Auto Grab "}, 1));
            }
            _menu.AddSubMenu(menuG);

            _menu.AddItem(new MenuItem("interrupt", "Auto-Interrupt")).SetValue(true);
            _menu.AddItem(new MenuItem("useE", "Auto-E after grab")).SetValue(true);
            _menu.AddItem(new MenuItem("killsteal", "Killsteal with Ult")).SetValue(false);
            _menu.AddItem(new MenuItem("combokey", "Combo Key")).SetValue(new KeyBind(32, KeyBindType.Press));
            _menu.AddToMainMenu();


        }
    }
}
