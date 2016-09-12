namespace Flowers_Series.Plugings
{
    using Common;
    using LeagueSharp;
    using LeagueSharp.Data.Enumerations;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using SharpDX;
    using System;
    using System.Linq;
    using static Common.Manager;

    public static class Vayne
    {
        private static Spell Q;
        private static Spell E;
        private static Spell R;
        private static HpBarDraw HpBarDraw = new HpBarDraw();

        private static Menu Menu => Program.ChampionMenu;
        private static Obj_AI_Hero Me => Program.Me;

        public static void Init()
        {
            Q = new Spell(SpellSlot.Q, 325f);
            E = new Spell(SpellSlot.E, 650f);
            R = new Spell(SpellSlot.R);

            E.SetTargetted(0.25f, 1600f);

            var ComboMenu = Menu.Add(new Menu("Vayne_Combo", "Combo"));
            {
                ComboMenu.Add(new MenuSeparator("QLogic", "Q Logic"));
                ComboMenu.Add(new MenuBool("Q", "Use Q", true));
                ComboMenu.Add(new MenuBool("AQA", "A-Q-A", true));
                ComboMenu.Add(new MenuBool("SafeCheck", "Safe Q Check", true));
                ComboMenu.Add(new MenuBool("QTurret", "Dont Cast In Turret", true));
                ComboMenu.Add(new MenuSeparator("ELogic", "E Logic"));
                ComboMenu.Add(new MenuBool("E", "Use E", true));
                ComboMenu.Add(new MenuSeparator("RLogic", "R Logic"));
                ComboMenu.Add(new MenuBool("R", "Use R", true));
                ComboMenu.Add(new MenuSlider("RCount", "When Enemies Counts >= ", 2, 1, 5));
                ComboMenu.Add(new MenuSlider("RHp", "Or Player HealthPercent <= %", 45));
            }

            var HarassMenu = Menu.Add(new Menu("Vayne_Harass", "Harass"));
            {
                HarassMenu.Add(new MenuBool("Q", "Use Q", true));
                HarassMenu.Add(new MenuBool("Q2Passive", "Use Q| Only Target have 2 Passive", true));
                HarassMenu.Add(new MenuBool("SafeCheck", "Safe Q Check", true));
                HarassMenu.Add(new MenuBool("QTurret", "Dont Cast In Turret", true));
                HarassMenu.Add(new MenuBool("E", "Use E | Only Target have 2 Passive"));
            }

            var LaneClearMenu = Menu.Add(new Menu("Vayne_LaneClear", "LaneClear"));
            {
                LaneClearMenu.Add(new MenuBool("Q", "Use Q", true));
                LaneClearMenu.Add(new MenuBool("QTurret", "Use Q To Attack Tower", true));
                LaneClearMenu.Add(new MenuSlider("Mana", "Min LaneClear Mana >= %", 50));
            }

            var JungleClearMenu = Menu.Add(new Menu("Vayne_JungleClear", "JungleClear"));
            {
                JungleClearMenu.Add(new MenuBool("Q", "Use Q", true));
                JungleClearMenu.Add(new MenuBool("E", "Use E", true));
                JungleClearMenu.Add(new MenuSlider("Mana", "Min JungleClear Mana >= %", 30));
            }

            var AutoMenu = Menu.Add(new Menu("Vayne_Auto", "Auto"));
            {
                AutoMenu.Add(new MenuSeparator("ELogic", "E Logic"));
                AutoMenu.Add(new MenuBool("E", "Use E"));
                if (GameObjects.EnemyHeroes.Any())
                {
                    GameObjects.EnemyHeroes.ForEach(i => AutoMenu.Add(new MenuBool("CastE" + i.ChampionName, "Cast To :" + i.ChampionName, AutoEnableList.Contains(i.ChampionName))));
                }
                AutoMenu.Add(new MenuSeparator("RLogic", "R Logic"));
                AutoMenu.Add(new MenuBool("R", "Use R", true));
                AutoMenu.Add(new MenuSlider("RCount", "When Enemies Counts >= ", 3, 1, 5));
                AutoMenu.Add(new MenuSlider("RRange", "Search Enemies Range ", 600, 500, 1200));
            }

            var EMenu = Menu.Add(new Menu("Vayne_E", "E Settings"));
            {
                EMenu.Add(new MenuList<string>("EMode", "E Mode: ", new[] { "FS", "VHR", "Marksman", "SharpShooter", "OKTW"}));
                EMenu.Add(new MenuBool("Under", "Dont Cast(Under Turret)", true));
                EMenu.Add(new MenuSlider("Push", "Push Tolerance", 0, -100));
            }

            var MiscMenu = Menu.Add(new Menu("Vayne_Misc", "Misc"));
            {
                MiscMenu.Add(new MenuBool("Forcus", "Force 2 Passive Target", true));
                MiscMenu.Add(new MenuBool("Gapcloser", "Anti Gapcloser", true));
                MiscMenu.Add(new MenuBool("AntiAlistar", "Anti Alistar", true));
                MiscMenu.Add(new MenuBool("AntiRengar", "Anti Rengar", true));
                MiscMenu.Add(new MenuBool("AntiKhazix", "Anti Khazix", true));
                MiscMenu.Add(new MenuBool("Interrupt", "Interrupt Danger Spells", true));
            }

            var Draw = Menu.Add(new Menu("Vayne_Draw", "Draw"));
            {
                Draw.Add(new MenuBool("E", "Draw E Range"));
                Draw.Add(new MenuBool("Damage", "Draw Combo Damage", true));
            }

            WriteConsole(GameObjects.Player.ChampionName + " Inject!");

            GameObject.OnCreate += OnCreate;
            Game.OnUpdate += OnUpdate;
            Variables.Orbwalker.OnAction += OnAction;
            Events.OnInterruptableTarget += OnInterruptableTarget;
            Events.OnGapCloser += OnGapCloser;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnCreate(GameObject sender, EventArgs Args)
        {
            if (Menu["Vayne_Misc"]["Gapcloser"] && E.IsReady())
            {
                var Rengar = GameObjects.EnemyHeroes.Find(heros => heros.ChampionName.Equals("Rengar"));
                var Khazix = GameObjects.EnemyHeroes.Find(heros => heros.ChampionName.Equals("Khazix"));

                if (Rengar != null && Menu["Vayne_Misc"]["AntiRengar"])
                {
                    if (sender.Name == "Rengar_LeapSound.troy" && sender.Distance(Me) < E.Range)
                        E.CastOnUnit(Rengar);
                }

                if (Khazix != null && Menu["Vayne_Misc"]["AntiKhazix"])
                {
                    if (sender.Name == "Khazix_Base_E_Tar.troy" && sender.Distance(Me) <= 300)
                        E.CastOnUnit(Khazix);
                }
            }
        }

        private static void OnUpdate(EventArgs Args)
        {
            if (Me.IsDead)
                return;

            var ForcusTarget = GameObjects.EnemyHeroes.FirstOrDefault(x => x.IsValidTarget(GetAttackRange(Me)) && x.HasBuff("VayneSilveredDebuff") && x.GetBuffCount("VayneSilveredDebuff") == 2);

            if (Menu["Vayne_Misc"]["Forcus"] && (InCombo || InHarass) && CheckTarget(ForcusTarget))
            {
                Variables.Orbwalker.ForceTarget = ForcusTarget;
            }

            if (!E.IsReady())
            {
                return;
            }

            if (InCombo && Menu["Vayne_Combo"]["E"] && E.IsReady())
            {
                var target = GetTarget(E.Range);

                if (CheckTarget(target))
                {
                    ELogic(target);
                }
            }

            if (InHarass && Menu["Vayne_Harass"]["E"] && E.IsReady())
            {
                var target = GetTarget(E.Range);

                if (CheckTarget(target) && target.HasBuff("VayneSilveredDebuff") && target.GetBuffCount("VayneSilveredDebuff") == 2)
                {
                    E.CastOnUnit(target);
                }
            }

            if (InClear && Menu["Vayne_JungleClear"]["E"] && E.IsReady() && 
                Me.ManaPercent >= Menu["Vayne_JungleClear"]["Mana"].GetValue<MenuSlider>().Value)
            {
                var mob = GetMobs(Me.Position, E.Range, true).FirstOrDefault();

                if (mob != null && mob.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(mob);
                }
            }

            if (Menu["Vayne_Auto"]["E"] && !InCombo)
            {
                var target = GetTarget(E.Range);

                if (CheckTarget(target) && Menu["Vayne_Auto"]["CastE" + target.ChampionName])
                {
                    ELogic(target);
                }
            }
        }

        private static void OnAction(object obj, OrbwalkingActionArgs Args)
        {
            if (!Q.IsReady())
            {
                return;
            }

            if (Args.Type == OrbwalkingType.AfterAttack)
            {
                AfterQLogic(Args);
            }

            if (Args.Type == OrbwalkingType.BeforeAttack)
            {
                BeforeQLogic(Args);
            }
        }

        private static void AfterQLogic(OrbwalkingActionArgs Args)
        {
            if (InCombo && Args.Target is Obj_AI_Hero && Menu["Vayne_Combo"]["Q"] && Menu["Vayne_Combo"]["AQA"])
            {
                var target = (Obj_AI_Hero) Args.Target;

                if (CheckTarget(target) && target.IsValidTarget(800f) && Q.IsReady())
                {
                    var AfterQPosition = Me.ServerPosition + (Game.CursorPos - Me.ServerPosition).Normalized() * 250;
                    var Distance = target.ServerPosition.Distance(AfterQPosition);

                    if (Menu["Vayne_Combo"]["QTurret"] && AfterQPosition.IsUnderEnemyTurret())
                    {
                        return;
                    }

                    if (Menu["Vayne_Combo"]["SafeCheck"] && AfterQPosition.CountEnemyHeroesInRange(300) >= 3)
                    {
                        return;
                    }

                    if (Distance <= 650 && Distance >= 300)
                    {
                        Q.Cast(Game.CursorPos);
                    }
                }
            }
            else if (InClear)
            {
                if (Args.Target is Obj_AI_Turret && Menu["Vayne_LaneClear"]["Q"]
                    && Me.ManaPercent >= Menu["Vayne_LaneClear"]["Mana"].GetValue<MenuSlider>().Value &&
                    Menu["Vayne_LaneClear"]["QTurret"] && Me.CountEnemyHeroesInRange(900) == 0 && Q.IsReady())
                {
                    Q.Cast(Game.CursorPos);
                }

                if (Args.Target is Obj_AI_Minion)
                {
                    LaneClearQ(Args);
                    JungleQ(Args);
                }
            }
        }

        private static void LaneClearQ(OrbwalkingActionArgs Args)
        {
            if (Menu["Vayne_LaneClear"]["Q"] && Me.ManaPercent >= Menu["Vayne_LaneClear"]["Mana"].GetValue<MenuSlider>().Value)
            {
                var minions = GetMinions(Me.Position, GetAttackRange(Me) + 175).Where(m => m.Health < Q.GetDamage(m) + Me.GetAutoAttackDamage(m));

                var minion = minions.FirstOrDefault();
                if (minion != null && Args.Target.NetworkId != minion.NetworkId)
                {
                    Q.Cast(Game.CursorPos);
                    Variables.Orbwalker.ForceTarget = minions.FirstOrDefault();
                }
            }
        }

        private static void JungleQ(OrbwalkingActionArgs Args)
        {
            if (Menu["Vayne_JungleClear"]["Q"] && Me.ManaPercent >= Menu["Vayne_JungleClear"]["Mana"].GetValue<MenuSlider>().Value)
            {
                var mobs = GetMobs(Me.Position, GetAttackRange(Me), true);

                if (mobs.Count > 0 && Q.IsReady())
                {
                    Q.Cast(Game.CursorPos);
                }
            }
        }

        private static void BeforeQLogic(OrbwalkingActionArgs Args)
        {
            if (!(Args.Target is Obj_AI_Hero))
            {
                return;
            }

            var target = (Obj_AI_Hero) Args.Target;

            if (InCombo && Menu["Vayne_Combo"]["Q"])
            {
                if (CheckTarget(target) && target.IsValidTarget(800f) && Q.IsReady())
                {
                    var AfterQPosition = Me.ServerPosition + (Game.CursorPos - Me.ServerPosition).Normalized() * 250;
                    var Distance = target.ServerPosition.Distance(AfterQPosition);

                    if (Menu["Vayne_Combo"]["QTurret"] && AfterQPosition.IsUnderEnemyTurret())
                    {
                        return;
                    }

                    if (Menu["Vayne_Combo"]["SafeCheck"] && AfterQPosition.CountEnemyHeroesInRange(300) >= 3)
                    {
                        return;
                    }

                    if (target.DistanceToPlayer() >= 600 && Distance <= 600)
                    {
                        Q.Cast(Game.CursorPos);
                        return;
                    }
                }
            }

            if (InHarass && Menu["Vayne_Harass"]["Q"])
            {
                if (CheckTarget(target) && target.IsValidTarget(800) && Q.IsReady())
                {
                    var AfterQPosition = Me.ServerPosition + (Game.CursorPos - Me.ServerPosition).Normalized() * 250;
                    var Distance = target.ServerPosition.Distance(AfterQPosition);

                    if (Menu["Vayne_Harass"]["QTurret"] && AfterQPosition.IsUnderEnemyTurret())
                    {
                        return;
                    }

                    if (Menu["Vayne_Harass"]["SafeCheck"] && AfterQPosition.CountEnemyHeroesInRange(300) >= 2)
                    {
                        return;
                    }

                    if (Menu["Vayne_Harass"]["Q2Passive"] && target.HasBuff("VayneSilveredDebuff") && target.GetBuffCount("VayneSilveredDebuff") == 2)
                    {
                        if (target.DistanceToPlayer() >= 600 && Distance <= 600)
                        {
                            Q.Cast(Game.CursorPos);
                        }
                    }
                    else if(!Menu["Vayne_Harass"]["Q2Passive"])
                    {
                        if (target.DistanceToPlayer() >= 600 && Distance <= 600)
                        {
                            Q.Cast(Game.CursorPos);
                        }
                    }
                }
            }
        }

        private static void OnInterruptableTarget(object obj, Events.InterruptableTargetEventArgs Args)
        {
            if (Menu["Vayne_Misc"]["Interrupt"] && E.IsReady() && Args.Sender.IsValidTarget(E.Range))
            {
                if (Args.Sender.IsCastingInterruptableSpell())
                {
                    E.CastOnUnit(Args.Sender);
                }

                if (Args.DangerLevel >= DangerLevel.Medium)
                {
                    E.CastOnUnit(Args.Sender);
                }
            }
        }

        private static void OnGapCloser(object obj, Events.GapCloserEventArgs Args)
        {
            if (Args.IsDirectedToPlayer)
            {
                if (Menu["Vayne_Misc"]["Gapcloser"] && E.IsReady())
                {
                    if (Menu["Vayne_Misc"]["AntiAlistar"] && Args.Sender.ChampionName == "Alistar" && Args.SkillType == GapcloserType.Targeted)
                    {
                        E.CastOnUnit(Args.Sender);
                    }
                    else if (Args.End.DistanceToPlayer() <= 250 && Args.Target.IsValid) 
                    {
                        E.CastOnUnit(Args.Sender);
                    }
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (!R.IsReady())
            {
                return;
            }

            if (InCombo && Menu["Vayne_Combo"]["R"])
            {
                if (Me.CountEnemyHeroesInRange(800) >= Menu["Vayne_Combo"]["RCount"].GetValue<MenuSlider>().Value)
                {
                    R.Cast();
                }

                if (Me.CountEnemyHeroesInRange(GetAttackRange(Me)) >= 1 && Me.HealthPercent <= Menu["Vayne_Combo"]["RHp"].GetValue<MenuSlider>().Value)
                {
                    R.Cast();
                }
            }

            if (Menu["Vayne_Auto"]["R"] &&
                Me.CountEnemyHeroesInRange(Menu["Vayne_Auto"]["RRange"].GetValue<MenuSlider>().Value) >= 
                Menu["Vayne_Auto"]["RCount"].GetValue<MenuSlider>().Value)
            {
                R.Cast();
            }
        }

        private static void OnDraw(EventArgs Args)
        {
            if (Me.IsDead)
                return;

            if (Menu["Vayne_Draw"]["E"] && E.IsReady())
            {
                Render.Circle.DrawCircle(Me.Position, E.Range, System.Drawing.Color.AliceBlue);
            }

            if (Menu["Vayne_Draw"]["Damage"])
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget() && !x.IsDead && !x.IsZombie))
                {
                    HpBarDraw.Unit = target;
                    HpBarDraw.DrawDmg((float)GetDamage(target), new ColorBGRA(255, 200, 0, 170));
                }
            }
        }

        private static void ELogic(Obj_AI_Base target)
        {
            if (Menu["Vayne_E"]["Under"] && Me.IsUnderEnemyTurret())
            {
                return;
            }

            if (target != null && target.IsHPBarRendered)
            {
                switch (Menu["Vayne_E"]["EMode"].GetValue<MenuList>().Index)
                {
                    case 0:
                        {
                            var EPred = E.GetPrediction(target);
                            var PD = 425 + Menu["Vayne_E"]["Push"].GetValue<MenuSlider>().Value;
                            var PP = EPred.UnitPosition.Extend(Me.Position, -PD);

                            for (int i = 1; i < PD; i += (int)target.BoundingRadius)
                            {
                                var VL = EPred.UnitPosition.Extend(Me.Position, -i);
                                var J4 = ObjectManager.Get<Obj_AI_Base>().Any(f => f.Distance(PP) <= target.BoundingRadius && f.Name.ToLower() == "beacon");
                                var CF = NavMesh.GetCollisionFlags(VL);

                                if (CF.HasFlag(CollisionFlags.Wall) || CF.HasFlag(CollisionFlags.Building) || J4)
                                {
                                    E.CastOnUnit(target);
                                    return;
                                }
                            }
                        }
                        break;
                    case 1:
                        {
                            var pushDistance = 425 + Menu["Vayne_E"]["Push"].GetValue<MenuSlider>().Value;
                            var Prediction = E.GetPrediction(target);
                            var endPosition = Prediction.UnitPosition.Extend(GameObjects.Player.ServerPosition, -pushDistance);

                            if (Prediction.Hitchance >= HitChance.VeryHigh)
                            {
                                if (endPosition.IsWall())
                                {
                                    var condemnRectangle = new Common.Geometry.Polygon.Rectangle(target.ServerPosition.ToVector2(), endPosition.ToVector2(), target.BoundingRadius);

                                    if (condemnRectangle.Points.Count(point => NavMesh.GetCollisionFlags(point.X, point.Y).HasFlag(CollisionFlags.Wall)) >= condemnRectangle.Points.Count * (20 / 100f))
                                    {
                                        E.CastOnUnit(target);
                                    }
                                }
                                else
                                {
                                    var step = pushDistance / 5f;
                                    for (float i = 0; i < pushDistance; i += step)
                                    {
                                        var endPositionEx = Prediction.UnitPosition.Extend(GameObjects.Player.ServerPosition, -i);
                                        if (endPositionEx.IsWall())
                                        {
                                            var condemnRectangle = new Common.Geometry.Polygon.Rectangle(target.ServerPosition.ToVector2(), endPosition.ToVector2(), target.BoundingRadius);

                                            if (condemnRectangle.Points.Count(point => NavMesh.GetCollisionFlags(point.X, point.Y).HasFlag(CollisionFlags.Wall)) >= condemnRectangle.Points.Count * (20 / 100f))
                                            {
                                                E.CastOnUnit(target);
                                            }
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case 2:
                        {
                            for (var i = 1; i < 8; i++)
                            {
                                var targetBehind = target.Position + Vector3.Normalize(target.ServerPosition - Me.Position) * i * 50;

                                if (targetBehind.IsWall() && target.IsValidTarget(E.Range))
                                {
                                    E.CastOnUnit(target);
                                    return;
                                }
                            }
                        }
                        break;
                    case 3:
                        {
                            var prediction = E.GetPrediction(target);

                            if (prediction.Hitchance >= HitChance.High)
                            {
                                var finalPosition = prediction.UnitPosition.Extend(Me.Position, -400);

                                if (finalPosition.IsWall())
                                {
                                    E.CastOnUnit(target);
                                    return;
                                }

                                for (var i = 1; i < 400; i += 50)
                                {
                                    var loc3 = prediction.UnitPosition.Extend(Me.Position, -i);

                                    if (loc3.IsWall())
                                    {
                                        E.CastOnUnit(target);
                                        return;
                                    }
                                }
                            }
                        }
                        break;
                    case 4:
                        {
                            var prepos = E.GetPrediction(target);
                            float pushDistance = 470;
                            var radius = 250;
                            var start2 = target.ServerPosition;
                            var end2 = prepos.CastPosition.Extend(Me.ServerPosition, -pushDistance);
                            var start = start2.ToVector2();
                            var end = end2.ToVector2();
                            var dir = (end - start).Normalized();
                            var pDir = dir.Perpendicular();
                            var rightEndPos = end + pDir * radius;
                            var leftEndPos = end - pDir * radius;
                            var rEndPos = new Vector3(rightEndPos.X, rightEndPos.Y, Me.Position.Z);
                            var lEndPos = new Vector3(leftEndPos.X, leftEndPos.Y, Me.Position.Z);
                            var step = start2.Distance(rEndPos) / 10;

                            for (var i = 0; i < 10; i++)
                            {
                                var pr = start2.Extend(rEndPos, step * i);
                                var pl = start2.Extend(lEndPos, step * i);

                                if (pr.IsWall() && pl.IsWall())
                                {
                                    E.CastOnUnit(target);
                                    return;
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}
