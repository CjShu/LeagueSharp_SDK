namespace Flowers_Series.Plugings
{
    using Common;
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using SharpDX;
    using System;
    using System.Linq;
    using static Common.Manager;

    public class Ahri
    {
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;
        private static MissileClient QObj, QReturn;
        private static HpBarDraw DrawHpBar = new HpBarDraw();

        private static Obj_AI_Hero Me => Program.Me;
        private static Menu Menu => Program.ChampionMenu;

        public static void Init()
        {
            Q = new Spell(SpellSlot.Q, 870f);
            W = new Spell(SpellSlot.W, 700f);
            E = new Spell(SpellSlot.E, 970f);
            R = new Spell(SpellSlot.R, 450f);

            Q.SetSkillshot(0.25f, 90, 1550, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 60, 1550, true, SkillshotType.SkillshotLine);

            var ComboMenu = Menu.Add(new Menu("Ahri_Combo", "Combo"));
            {
                ComboMenu.Add(new MenuBool("Q", "Use Q", true));
                ComboMenu.Add(new MenuBool("W", "Use W", true));
                ComboMenu.Add(new MenuBool("E", "Use E", true));
                ComboMenu.Add(new MenuBool("R", "Use R", true));
                ComboMenu.Add(new MenuBool("RMax", "Use R| Max Dash", true));
                ComboMenu.Add(new MenuBool("RTurret", "Use R| Dont Dash To Enemy Turret", true));
                ComboMenu.Add(new MenuBool("RWall", "Use R| Check Wall", true));
            }

            var HarassMenu = Menu.Add(new Menu("Ahri_Harass", "Harass"));
            {
                HarassMenu.Add(new MenuBool("Q", "Use Q", true));
                HarassMenu.Add(new MenuBool("W", "Use W"));
                HarassMenu.Add(new MenuBool("E", "Use E"));
                HarassMenu.Add(new MenuSlider("Mana", "When Player ManaPercent >= %", 60));
            }

            var LaneClearMenu = Menu.Add(new Menu("Ahri_LaneClear", "Lane Clear"));
            {
                LaneClearMenu.Add(new MenuSliderButton("Q", "Use Q| Min Hit Minions >= x", 3, 1, 5, true));
                LaneClearMenu.Add(new MenuBool("W", "Use W", true));
                LaneClearMenu.Add(new MenuSlider("Mana", "When Player ManaPercent >= %", 60));
            }

            var JungleMenu = Menu.Add(new Menu("Ahri_JungleClear", "Jungle Clear"));
            {
                JungleMenu.Add(new MenuBool("Q", "Use Q", true));
                JungleMenu.Add(new MenuBool("W", "Use W", true));
                JungleMenu.Add(new MenuBool("E", "Use E", true));
                JungleMenu.Add(new MenuSlider("Mana", "When Player ManaPercent >= %", 30));
            }

            var KillStealMenu = Menu.Add(new Menu("Ahri_KillSteal", "Kill Steal"));
            {
                KillStealMenu.Add(new MenuBool("Q", "Use Q", true));
                KillStealMenu.Add(new MenuBool("W", "Use W", true));
                KillStealMenu.Add(new MenuBool("E", "Use E", true));
            }

            var MiscMenu = Menu.Add(new Menu("Ahri_Misc", "Misc"));
            {
                MiscMenu.Add(new MenuBool("AutoQ", "Auto Q| When Enemies Can't Move!", true));
                MiscMenu.Add(new MenuBool("EInt", "Use E| Interrupt Spell", true));
                MiscMenu.Add(new MenuBool("EGap", "Use E| Anti GapCloser", true));
                MiscMenu.Add(new MenuSeparator("EGapName", "GapCloser Select"));
                if (GameObjects.EnemyHeroes.Any())
                {
                    GameObjects.EnemyHeroes.ForEach(i => MiscMenu.Add(new MenuBool(i.ChampionName.ToLower(), i.ChampionName, true)));
                }
            }

            var Draw = Menu.Add(new Menu("Ahri_Draw", "Draw"));
            {
                Draw.Add(new MenuBool("Q", "Q Range"));
                Draw.Add(new MenuBool("W", "W Range"));
                Draw.Add(new MenuBool("E", "E Range"));
                Draw.Add(new MenuBool("R", "R Range"));
                Draw.Add(new MenuBool("DrawDamage", "Draw Combo Damage", true));
            }

            Menu.Add(new MenuSeparator("Check", "Please Check Your Evade# or EzEvade"));
            Menu.Add(new MenuSeparator("Check1", "About Use R Evade Logic"));
            Menu.Add(new MenuSeparator("Check2", "I Suggest Set It Off!"));

            Events.OnInterruptableTarget += OnInterruptableTarget;
            Events.OnGapCloser += OnGapCloser;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnInterruptableTarget(object obj, Events.InterruptableTargetEventArgs Args)
        {
            if (!Menu["Ahri_Misc"]["EInt"] || !Args.Sender.IsEnemy || !Args.Sender.IsValidTarget(E.Range) ||
                !E.IsReady()) return;

            if (Args.DangerLevel >= LeagueSharp.Data.Enumerations.DangerLevel.High ||
                Args.Sender.IsCastingInterruptableSpell())
            {
                E.CastTo(Args.Sender);
            }
        }

        private static void OnGapCloser(object obj, Events.GapCloserEventArgs Args)
        {
            if (Args.IsDirectedToPlayer && Menu["Ahri_Misc"]["EGap"] &&
                E.IsReady() && Args.Sender.DistanceToPlayer() <= 300 &&
                Menu["Ahri_Misc"][Args.Sender.ChampionName.ToLower()])
            {
                E.CastTo(Args.Sender);
            }
        }

        private static void OnCreate(GameObject sender, EventArgs Args)
        {
            var client = sender as MissileClient;

            if (client == null) return;

            var QMissleClient = client;

            if (!QMissleClient.SpellCaster.IsMe || !QMissleClient.IsValid) return;

            var Name = QMissleClient.SData.Name;

            if (Name.Contains("AhriOrbMissile"))
            {
                QObj = QMissleClient;
            }

            if (Name.Contains("AhriOrbReturn"))
            {
                QReturn = QMissleClient;
            }
        }

        private static void OnDelete(GameObject sender, EventArgs Args)
        {
            if (!(sender is MissileClient)) return;

            var QMissleClient = (MissileClient) sender;

            if (!QMissleClient.SpellCaster.IsMe) return;

            var Name = QMissleClient.SData.Name;

            if (Name.Contains("AhriOrbMissile") && QMissleClient.IsValid)
            {
                QObj = null;
            }

            if (Name.Contains("AhriOrbReturn") && QMissleClient.IsValid)
            {
                QReturn = null;
            }
        }

        private static void OnUpdate(EventArgs Args)
        {
            if (Me.IsDead)
            {
                return;
            }

            if (InCombo)
            {
                ComboLogic();
            }

            if (InHarass && !Me.IsUnderEnemyTurret())
            {
                HarassLogic();
            }

            if (InClear)
            {
                LaneClearLogic();
                JungleClearLogic();
            }

            KillStealLogic();
            AutoQLogic();
        }

        private static void ComboLogic()
        {
            var target = GetTarget(900f, DamageType.Magical);

            if (!CheckTarget(target)) return;

            if (Menu["Ahri_Combo"]["R"] && R.IsReady() && target.IsValidTarget(900f))
            {
                RLogic(target);
            }

            if (Menu["Ahri_Combo"]["E"] && E.IsReady() && target.IsValidTarget(E.Range - 50))
            {
                E.CastTo(target);
            }

            if (Menu["Ahri_Combo"]["W"] && W.IsReady() && target.IsValidTarget(R.Range))//target.IsValidTarget(W.Range))
            {
                W.Cast();
            }

            if (Menu["Ahri_Combo"]["Q"] && Q.IsReady() && target.IsValidTarget(Q.Range - 50))
            {
                Q.CastTo(target);
            }
        }

        private static void RLogic(Obj_AI_Hero target)
        {
            if (!CheckTarget(target) || !target.IsValidTarget(900f)) return;

            var RDashPos = Vector3.Zero;

            RDashPos = Menu["Ahri_Combo"]["RMax"] ? Me.ServerPosition.Extend(Game.CursorPos, 450f) : Game.CursorPos;

            if ((RDashPos.IsWall() && Menu["Ahri_Combo"]["RWall"]) || 
                (RDashPos.IsUnderEnemyTurret() && Menu["Ahri_Combo"]["RTurret"]) ||
                (target.Health < GetDamage(target, false, true, true, true, false) && target.IsValidTarget(650)))
            {
                return;
            }

            if (Me.HasBuff("AhriTumble"))
            {
                var buffTime = Me.GetBuff("AhriTumble").EndTime;

                if (buffTime - Game.Time <= 3)
                {
                    R.Cast(RDashPos);
                }

                if (QReturn != null && QReturn.IsValid)
                {
                    var ReturnPos = QReturn.Position;

                    if (target.DistanceToPlayer() > ReturnPos.DistanceToPlayer())
                    {
                        var targetdis = target.Position.Distance(ReturnPos);
                        var QReturnEnd = QReturn.EndPosition;

                        if (!(targetdis < Q.Range)) return;

                        var CastPos = QReturnEnd.Extend(target.ServerPosition, target.ServerPosition.Distance(RDashPos));

                        if ((CastPos.IsWall() && Menu["Ahri_Combo"]["RWall"]) ||
                            Me.ServerPosition.Distance(CastPos) > R.Range
                            || CastPos.CountEnemyHeroesInRange(R.Range) > 2 ||
                            (RDashPos.IsUnderEnemyTurret() && Menu["Ahri_Combo"]["RTurret"]))
                        {
                            return;
                        }

                        R.Cast(CastPos);
                    }
                }
                else
                {
                    if (Q.IsReady() || RDashPos.CountEnemyHeroesInRange(R.Range) > 2 || !target.IsValidTarget(800f))
                        return;

                    if (Game.CursorPos.Distance(target.Position) > target.DistanceToPlayer() && target.IsValidTarget(R.Range))
                    {
                        R.Cast(RDashPos);
                    }
                    else if (Game.CursorPos.Distance(target.Position) < target.DistanceToPlayer() && !target.IsValidTarget(R.Range) && target.IsValidTarget(800f))
                    {
                        R.Cast(RDashPos);
                    }
                }
            }
            else
            {
                var AllDamage = GetDamage(target, false);
                var QDamage = Q.GetDamage(target) * 2;
                var WDamage = W.GetDamage(target);
                var RDamage = R.GetDamage(target) * 3;

                if (!target.IsValidTarget(800) || !(target.Distance(RDashPos) <= R.Range)) return;

                if (RDashPos.CountEnemyHeroesInRange(R.Range) > 2 || 
                    target.CountAllyHeroesInRange(R.Range) > 2)
                {
                    return;
                }

                if (target.Health >= AllDamage && target.Health <= QDamage + WDamage + RDamage)
                {
                    R.Cast(RDashPos);
                }
                else if (target.Health < RDamage + QDamage)
                {
                    R.Cast(RDashPos);
                }
                else if (target.Health < RDamage + WDamage)
                {
                    R.Cast(RDashPos);
                }
            }
        }

        private static void HarassLogic()
        {
            if (!(Me.ManaPercent >= Menu["Ahri_Harass"]["Mana"].GetValue<MenuSlider>().Value)) return;

            var target = GetTarget(Q.Range, DamageType.Magical);

            if (!CheckTarget(target)) return;

            if (Menu["Ahri_Harass"]["E"] && E.IsReady() && target.IsValidTarget(E.Range))
            {
                E.CastTo(target);
            }

            if (Menu["Ahri_Harass"]["W"] && W.IsReady() && target.IsValidTarget(R.Range))//target.IsValidTarget(W.Range))
            {
                W.Cast();
            }

            if (Menu["Ahri_Harass"]["Q"] && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.CastTo(target);
            }
        }

        private static void LaneClearLogic()
        {
            if (!(Me.ManaPercent >= Menu["Ahri_LaneClear"]["Mana"].GetValue<MenuSlider>().Value)) return;

            var minions = GetMinions(Me.Position, Q.Range);

            if (minions.Count <= 0) return;

            if (Menu["Ahri_LaneClear"]["Q"].GetValue<MenuSliderButton>().BValue && Q.IsReady())
            {
                var QFarm = Q.GetLineFarmLocation(minions, Q.Width);

                if (QFarm.MinionsHit >= Menu["Ahri_LaneClear"]["Q"].GetValue<MenuSliderButton>().SValue)
                {
                    Q.Cast(QFarm.Position);
                }
            }

            if (!Menu["Ahri_LaneClear"]["W"] || !W.IsReady() || QObj != null || QReturn != null) return;

            var Wminions = GetMinions(Me.Position, W.Range);

            if (Wminions.Count >= 3)
            {
                W.Cast();
            }
        }

        private static void JungleClearLogic()
        {
            if (Me.ManaPercent >= Menu["Ahri_JungleClear"]["Mana"].GetValue<MenuSlider>().Value)
            {
                var mobs = GetMobs(Me.Position, Q.Range, true);

                if (mobs.Count <= 0) return;

                var mob = mobs.FirstOrDefault();

                if (Menu["Ahri_JungleClear"]["E"] && E.IsReady())
                {
                    E.Cast(mob);
                    return;
                }

                if (Menu["Ahri_JungleClear"]["W"] && W.IsReady())
                {
                    W.Cast(mob);
                    return;
                }

                if (Menu["Ahri_JungleClear"]["Q"] && Q.IsReady())
                {
                    Q.Cast(mob);
                }
            }
        }

        private static void KillStealLogic()
        {
            foreach (var target in GetEnemies(Q.Range))
            {
                if (!CheckTarget(target)) continue;

                if (Menu["Ahri_KillSteal"]["Q"] && Q.IsReady() && target.IsValidTarget(Q.Range) && Q.GetDamage(target) > target.Health)
                {
                    Q.CastTo(target);
                    return;
                }

                if (Menu["Ahri_KillSteal"]["W"] && W.IsReady() && target.IsValidTarget(W.Range) && W.GetDamage(target) > target.Health)
                {
                    W.Cast();
                    return;
                }

                if (Menu["Ahri_KillSteal"]["E"] && E.IsReady() && target.IsValidTarget(E.Range) && E.GetDamage(target) > target.Health)
                {
                    E.CastTo(target);
                }
            }
        }

        private static void AutoQLogic()
        {
            if (!Menu["Ahri_Misc"]["AutoQ"] || !Q.IsReady() || Me.IsUnderEnemyTurret()) return;

            foreach (var target in GetEnemies(Q.Range).Where(x => x.IsValidTarget(Q.Range) && !x.HasBuffOfType(BuffType.SpellShield) && !CanMove(x)))
            {
                if (!CheckTarget(target)) continue;

                Q.CastTo(target);
                return;
            }
        }

        private static void OnDraw(EventArgs Args)
        {
            if (Me.IsDead)
                return;

            if (Menu["Ahri_Draw"]["Q"] && Q.IsReady())
                Render.Circle.DrawCircle(Me.Position, Q.Range, System.Drawing.Color.BlueViolet);

            if (Menu["Ahri_Draw"]["W"] && W.IsReady())
                Render.Circle.DrawCircle(Me.Position, W.Range, System.Drawing.Color.BlueViolet);

            if (Menu["Ahri_Draw"]["E"] && E.IsReady())
                Render.Circle.DrawCircle(Me.Position, E.Range, System.Drawing.Color.BlueViolet);

            if (Menu["Ahri_Draw"]["R"] && R.IsReady())
                Render.Circle.DrawCircle(Me.Position, 450f, System.Drawing.Color.BlueViolet);


            if (Menu["Ahri_Draw"]["DrawDamage"])
            {
                foreach (var e in ObjectManager.Get<Obj_AI_Hero>().Where(e => e.IsValidTarget() && e.IsValid && !e.IsDead && !e.IsZombie))
                {
                    HpBarDraw.Unit = e;
                    HpBarDraw.DrawDmg((float)GetDamage(e), new ColorBGRA(255, 204, 0, 170));
                }
            }
        }
    }
}
