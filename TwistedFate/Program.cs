namespace Flowers__TwistedFate
{
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using SharpDX;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class Program
    {
        private static Obj_AI_Hero Me;
        private static Menu Menu;
        public static Spell Q, W, R;
        private static SpellSlot Flash;
        private static HpBarDraw DrawHpBar = new HpBarDraw();
        internal static Random Random = new Random();

        static void Main(string[] Args)
        {
            Bootstrap.Init(Args);
            Events.OnLoad += OnLoad;
        }

        private static void OnLoad(object obj, EventArgs Args)
        {
            if (GameObjects.Player.ChampionName != "TwistedFate")
                return;

            Me = GameObjects.Player;

            Flash = Me.GetSpellSlot("SummonerFlash");
            Q = new Spell(SpellSlot.Q, 1450f);
            W = new Spell(SpellSlot.W, 1000f);
            R = new Spell(SpellSlot.R, 5500f);

            Q.SetSkillshot(0.25f, 40f, 1000, false, SkillshotType.SkillshotLine);

            Menu = new Menu("NightMoon", "Flowers' TwistedFate", true).Attach();

            Menu.Add(new MenuSeparator("OLD", "This Is Old Version and i dont update it"));
            Menu.Add(new MenuSeparator("OLD1", "Please Use Flowers' Series"));

            var QMenu = Menu.Add(new Menu("QMenu", "Q Config"));
            QMenu.Add(new MenuSeparator("ComboSeparatorQ", "Combo"));
            QMenu.Add(new MenuBool("ComboQ", "Use Q"));
            QMenu.Add(new MenuBool("ComboQMana", "Save Mana to Cast W", true));
            QMenu.Add(new MenuSeparator("HarassSeparatorQ", "Harass"));
            QMenu.Add(new MenuBool("HarassQ", "Use Q", true));
            QMenu.Add(new MenuSeparator("LaneClearSeparatorQ", "LaneClear"));
            QMenu.Add(new MenuBool("LaneClearQ", "Use Q", true));
            QMenu.Add(new MenuSlider("LaneClearQMin", "Min Q Hit", 3, 1, 5));
            QMenu.Add(new MenuSeparator("JungleClearSeparatorQ", "JungleClear"));
            QMenu.Add(new MenuBool("JungleClearQ", "Use Q", true));
            QMenu.Add(new MenuSeparator("OhterSeparatorQ", "JungleClear"));
            QMenu.Add(new MenuBool("KillStealQ", "KillSteal Q", true));
            QMenu.Add(new MenuBool("DebuffQ", "If Target Have Debuff Auto Q", true));
            QMenu.Add(new MenuBool("DashQ", "If Target Is Dashing Auto Q", true));
            QMenu.Add(new MenuKeyBind("QKey", "Cast Q Key (Press)", System.Windows.Forms.Keys.D3, KeyBindType.Press));

            var WMenu = Menu.Add(new Menu("WMenu", "W Config"));
            WMenu.Add(new MenuSeparator("CardSeparator", "SelectCard"));
            WMenu.Add(new MenuSeparator("Card1Separator", "I suggest you not set W is Use W Spell"));
            WMenu.Add(new MenuKeyBind("SelectBlue", "Select Blue Card", System.Windows.Forms.Keys.E, KeyBindType.Press));
            WMenu.Add(new MenuKeyBind("SelectYellow", "Select Yellow Card", System.Windows.Forms.Keys.W, KeyBindType.Press));
            WMenu.Add(new MenuKeyBind("SelectRed", "Select Red Card", System.Windows.Forms.Keys.T, KeyBindType.Press));
            WMenu.Add(new MenuSeparator("Mode", "Smart"));
            WMenu.Add(new MenuBool("ComboWRed", "Pick A Red Card In Combo!", true));
            WMenu.Add(new MenuBool("ComboW", "If Low Mana Auto Blue Card In Combo", true));
            WMenu.Add(new MenuBool("ComboWBlue", "If Target Can Kill And In AA Range Auto Blue Card", true));
            WMenu.Add(new MenuBool("HarassW", "Blue Card To Harass", true));
            WMenu.Add(new MenuBool("LaneClearW", "Smart Blue/Red Cast to LaneClear", true));
            WMenu.Add(new MenuBool("JungleClearW", "Smart Blue/Red Cast to JungleClearClear", true));
            WMenu.Add(new MenuSeparator("HumanizerSeparator", "Humanizer")); 
            WMenu.Add(new MenuBool("EnableHumanizer", "Enable Humanizer Pick!", false));
            WMenu.Add(new MenuSlider("MinHumanizer", "Min Humanizer Pick Time(ms)", 750, 500, 1500));
            WMenu.Add(new MenuSlider("MaxHumanizer", "Max Humanizer Pick Time(ms)", 1500, 1500, 3500));
            WMenu.Add(new MenuSliderButton("LowHp", "If Player Hp <= % Disable Humanizer!", 30, 0, 100, true));
            WMenu.Add(new MenuSeparator("OthersSeparator", "Others"));
            WMenu.Add(new MenuBool("ComboDisableAA", "Selecting Card Disable AA In Combo", true));
            WMenu.Add(new MenuBool("AntiW", "Auto W To Anti Gapcloser", true));
            WMenu.Add(new MenuBool("InterW", "Auto W To Interrupt", true));
            WMenu.Add(new MenuBool("AutoYellow", "Select Yellow Card After Ult", true));

            var ManaControl = Menu.Add(new Menu("Mana", "Mana Control"));
            ManaControl.Add(new MenuSlider("HarassMana", "Min Harass Mana", 40));
            ManaControl.Add(new MenuSlider("LaneClearMana", "Min LaneClear Mana", 40));
            ManaControl.Add(new MenuSlider("JungleClearMana", "Min JungleClear Mana"));

            var ADMode = Menu.Add(new Menu("ADMode", "AD Mode"));
            ADMode.Add(new MenuBool("EnableADMode", "Enable AD Mode", false));
            ADMode.Add(new MenuBool("UseItem", "Use Botrk, YouMuu, CutGlass...", false));

            var DrawMenu = Menu.Add(new Menu("DrawMenu", "Drawing"));
            DrawMenu.Add(new MenuBool("DrawQ", "Q Range"));
            DrawMenu.Add(new MenuBool("DrawW", "Beautiful W Draw", true));
            DrawMenu.Add(new MenuBool("DrawR", "R Range"));
            DrawMenu.Add(new MenuBool("DrawRMin", "MinMap&Map R Range"));
            DrawMenu.Add(new MenuBool("DrawAF", "AA + Flash Range"));
            DrawMenu.Add(new MenuBool("DrawComboDamage", "Draw Combo Damage(Only Spell Ready!)", true));

            Game.OnUpdate += OnUpdate;
            GameObject.OnCreate += OnCreate;
            Variables.Orbwalker.OnAction += OnAction;
            Events.OnInterruptableTarget += OnInterruptableTarget;
            Events.OnGapCloser += OnGapCloser;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
            //Obj_AI_Base.OnProcessSpellCast += (sender, EventArgs) => 
            //{
            //    if (sender.IsMe && EventArgs.Slot == SpellSlot.W && EventArgs.SData.Name.Equals("PickACard", StringComparison.InvariantCultureIgnoreCase))
            //    {
            //        Card.PickCardTickCount = Variables.TickCount;
            //    }
            //};
        }

        private static void OnUpdate(EventArgs Args)
        {
            if (Me.IsDead)
                return;

            Auto(Args);

            QLogic();
            WLogic();

            ADMode();
        }

        private static void Auto(EventArgs args)
        {
            if (Menu["QMenu"]["QKey"].GetValue<MenuKeyBind>().Active)
            {
                var e = Variables.TargetSelector.GetTarget(Q.Range, Q.DamageType);

                if (e != null && e.IsHPBarRendered)
                {
                    Q.Cast(e);
                    return;
                }
            }

            foreach (var e in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(Q.Range) && !e.IsZombie && e.IsHPBarRendered))
            {
                if (Menu["QMenu"]["DebuffQ"].GetValue<MenuBool>().Value && HaveDebuff(e))
                {
                    Q.Cast(e);
                }

                if (Menu["QMenu"]["KillStealQ"].GetValue<MenuBool>().Value && Q.GetDamage(e) > e.Health + 40)
                {
                    Q.Cast(e);
                }
            }
        }

        private static void QLogic()
        {
            if (Q.IsReady())
            {
                switch (Variables.Orbwalker.ActiveMode)
                {
                    case OrbwalkingMode.Combo:
                        if (Menu["QMenu"]["ComboQ"].GetValue<MenuBool>().Value)
                        {
                            var e = Variables.TargetSelector.GetTarget(Q.Range, Q.DamageType);

                            if (e != null && e.IsHPBarRendered)
                            {
                                if (Menu["QMenu"]["ComboQMana"].GetValue<MenuBool>().Value && Me.Mana > Q.Instance.ManaCost + W.Instance.ManaCost)
                                {
                                    Q.Cast(e, false, true);
                                }
                                else
                                {
                                    Q.Cast(e, false, true);
                                }
                            }
                        }
                        break;
                    case OrbwalkingMode.Hybrid:
                        if (Menu["QMenu"]["HarassQ"].GetValue<MenuBool>().Value && Menu["Mana"]["HarassMana"].GetValue<MenuSlider>().Value < Me.ManaPercent)
                        {
                            var e = Variables.TargetSelector.GetTarget(Q.Range, Q.DamageType);

                            if (e != null && e.IsHPBarRendered)
                            {
                                Q.Cast(e, false, true);
                            }
                        }
                        break;
                    case OrbwalkingMode.LaneClear:
                        var min = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsMinion && m.IsEnemy && m.Team != GameObjectTeam.Neutral && m.IsValidTarget(Q.Range)).ToList();
                        var mob = GameObjects.Jungle.Where(j => j.IsValidTarget(Q.Range) && !min.Contains(j)).ToList();

                        if (min.Count() > 0 && Menu["QMenu"]["LaneClearQ"].GetValue<MenuBool>().Value && Menu["Mana"]["LaneClearMana"].GetValue<MenuSlider>().Value < Me.ManaPercent)
                        {    
                            var QFarm = Q.GetLineFarmLocation(min, Q.Width);

                            if (QFarm.MinionsHit >= Menu["QMenu"]["LaneClearQMin"].GetValue<MenuSlider>().Value)
                            {
                                Q.Cast(QFarm.Position);
                            }
                        }

                        if (Menu["QMenu"]["JungleClearQ"].GetValue<MenuBool>().Value && Menu["Mana"]["JungleClearMana"].GetValue<MenuSlider>().Value < Me.ManaPercent)
                        {
                            if (mob.Count() > 0 && mob.FirstOrDefault().Health >= Me.GetAutoAttackDamage(mob.FirstOrDefault()))
                            {
                                Q.Cast(mob.FirstOrDefault());
                            }
                        }
                        break;
                }
            }
        }

        private static void WLogic()
        {
            if (W.IsReady())
            {
                PickACard();

                switch (Variables.Orbwalker.ActiveMode)
                {
                    case OrbwalkingMode.Combo:
                        var target = Variables.TargetSelector.GetTarget(W.Range, W.DamageType);

                        if (target != null && !target.IsZombie && target.IsHPBarRendered)
                        {
                            if (Menu["WMenu"]["ComboWRed"] && target.CountAllyHeroesInRange(220) > 0)
                            {
                                Card.ToSelect(Cards.Red);
                            }
                            else if ((Me.Mana + W.Instance.ManaCost) <= (Q.Instance.ManaCost + W.Instance.ManaCost) && Menu["WMenu"]["ComboW"].GetValue<MenuBool>().Value)
                            {
                                Card.ToSelect(Cards.Blue);
                            }
                            else if (Menu["WMenu"]["ComboWBlue"].GetValue<MenuBool>().Value && target.Health < W.GetDamage(target) - 50 && target.DistanceToPlayer() < 590)
                            {
                                Card.ToSelect(Cards.Blue);

                                if (Card.Status == SelectStatus.Selected && target.DistanceToPlayer() < 590)
                                {
                                    Me.IssueOrder(GameObjectOrder.AttackUnit, target);
                                }
                            }
                            else
                            {
                                Card.ToSelect(Cards.Yellow);
                            }
                        }
                        break;
                    case OrbwalkingMode.Hybrid:
                        if (Menu["WMenu"]["HarassW"].GetValue<MenuBool>().Value && Menu["Mana"]["HarassMana"].GetValue<MenuSlider>().Value < Me.ManaPercent)
                        {
                            var HarassTarget = Variables.TargetSelector.GetTarget(W.Range, W.DamageType);

                            if (Me.Mana >= (Q.Instance.ManaCost + W.Instance.ManaCost))
                            {
                                if (HarassTarget != null && HarassTarget.IsHPBarRendered)
                                {
                                    Card.ToSelect(Cards.Blue);

                                    if (Card.Status == SelectStatus.Selected && HarassTarget.DistanceToPlayer() < 590)
                                    {
                                        Me.IssueOrder(GameObjectOrder.AttackUnit, HarassTarget);
                                    }
                                }
                            }
                        }
                        break;
                    case OrbwalkingMode.LaneClear:
                        var min = ObjectManager.Get<Obj_AI_Base>().Where(m => m.IsValidTarget(590)).OrderBy(m => m.IsMinion).ToList();
                        var mob = GameObjects.Jungle.Where(j => j.IsValidTarget(W.Range)).ToList();

                        if (min.Count > 0 && Menu["WMenu"]["LaneClearW"].GetValue<MenuBool>().Value && Menu["Mana"]["LaneClearMana"].GetValue<MenuSlider>().Value < Me.ManaPercent)
                        {
                            if (min.Count >= 3)
                            {
                                Card.ToSelect(Cards.Red);
                            }
                            else if (min.Count < 3)
                            {
                                Card.ToSelect(Cards.Blue);
                            }
                        }

                        if (mob.Count() > 0 && Menu["WMenu"]["JungleClearW"].GetValue<MenuBool>().Value && Menu["Mana"]["JungleClearMana"].GetValue<MenuSlider>().Value < Me.ManaPercent)
                        {
                            if (mob.Count >= 2)
                            {
                                Card.ToSelect(Cards.Red);
                            }
                            else if (mob.Count < 2)
                            {
                                Card.ToSelect(Cards.Blue);
                            }
                            else if (mob[0].Health < W.GetDamage(mob[0]))
                            {
                                Card.ToSelect(Cards.Blue);

                                if (Card.Status == SelectStatus.Selected)
                                {
                                    Me.IssueOrder(GameObjectOrder.AttackUnit, mob[0]);
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private static void PickACard()
        {
            if (Menu["WMenu"]["SelectBlue"].GetValue<MenuKeyBind>().Active)
            {
                Card.ToSelect(Cards.Blue);
            }

            if (Menu["WMenu"]["SelectYellow"].GetValue<MenuKeyBind>().Active)
            {
                Card.ToSelect(Cards.Yellow);
            }

            if (Menu["WMenu"]["SelectRed"].GetValue<MenuKeyBind>().Active)
            {
                Card.ToSelect(Cards.Red);
            }
        }

        private static void ADMode()
        {
            if (Menu["ADMode"]["EnableADMode"].GetValue<MenuBool>().Value && Menu["ADMode"]["UseItem"].GetValue<MenuBool>().Value && Variables.Orbwalker.ActiveMode == OrbwalkingMode.Combo)
            {
                var Botrk = Items.HasItem(3153);
                var CutGlass = Items.HasItem(3144);
                var YouMuu = Items.HasItem(3142);
                var target = Variables.Orbwalker.GetTarget() as Obj_AI_Hero;

                if (target != null && target.IsValidTarget() && target.IsHPBarRendered)
                {
                    if (target.DistanceToPlayer() < 590 && Items.HasItem(3142))
                    {
                        Items.UseItem(3142);
                    }

                    if (target.DistanceToPlayer() < 450)
                    {
                        if (Items.HasItem(3144) || Items.HasItem(3153))
                        {
                            var ItemID = Items.HasItem(3144) ? 3144 : 3153;

                            if (Items.CanUseItem(ItemID) && Me.HealthPercent < 80)
                            {
                                Items.UseItem(ItemID, target);
                            }
                        }
                    }
                }
            }
        }

        private static void OnCreate(GameObject sender, EventArgs Args)
        {
            if (Menu["WMenu"]["AntiW"].GetValue<MenuBool>().Value)
            {
                var Tryndamere = GameObjects.EnemyHeroes.Find(heros => heros.ChampionName.Equals("Tryndamere"));

                if (Tryndamere != null)
                {
                    if (sender.Position.Distance(Me.Position) < 590)
                    {
                        if (Tryndamere.HasBuff("UndyingRage"))
                        {
                            if (sender.Position.Distance(Me.Position) < 350)
                            {
                                Card.ToSelect(Cards.Yellow);

                                if (Me.HasBuff("GoldCardPreAttack") && Tryndamere.ServerPosition.Distance(Me.Position) < 590)
                                {
                                    Me.IssueOrder(GameObjectOrder.AttackUnit, Tryndamere);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void OnAction(object sender, OrbwalkingActionArgs Args)
        {
            if (Args.Type == OrbwalkingType.BeforeAttack)
            {
                if (Variables.Orbwalker.ActiveMode == OrbwalkingMode.Combo && Args.Target is Obj_AI_Hero && Menu["WMenu"]["ComboDisableAA"].GetValue<MenuBool>().Value && !Menu["ADMode"]["EnableADMode"].GetValue<MenuBool>().Value)
                {
                    if (Card.Status == SelectStatus.Selecting && Variables.TickCount - Card.PickCardTickCount > 300)
                    {
                        Args.Process = false;
                    }
                }

                if (Variables.Orbwalker.ActiveMode == OrbwalkingMode.LaneClear && Args.Target is Obj_AI_Turret && Me.CountEnemyHeroesInRange(800) < 1)
                {
                    if (W.IsReady() && Card.Status == SelectStatus.Ready)
                    {
                        Card.ToSelect(Cards.Blue);

                        if (Card.Status == SelectStatus.Selected && Args.Target.InAutoAttackRange())
                        {
                            Me.IssueOrder(GameObjectOrder.AttackUnit, Args.Target);
                        }
                    }
                }
            }
        }

        private static void OnInterruptableTarget(object sender, Events.InterruptableTargetEventArgs Args)
        {
            if (Args.Sender.IsEnemy)
            {
                if (W.IsReady() && Args.Sender.IsValidTarget(W.Range) && Menu["WMenu"]["InterW"].GetValue<MenuBool>().Value)
                {
                    Card.ToSelect(Cards.Yellow);

                    if (Card.Status == SelectStatus.Selected && Args.Sender.ServerPosition.Distance(Me.ServerPosition) < 590)
                    {
                        Me.IssueOrder(GameObjectOrder.AttackUnit, Args.Sender);
                    }
                }
            }
        }

        private static void OnGapCloser(object sender, Events.GapCloserEventArgs Args)
        {
            if (Args.Sender.IsEnemy)
            {
                if (W.IsReady() && W.IsInRange(Args.End) && Menu["WMenu"]["AntiW"].GetValue<MenuBool>().Value)
                {
                    Card.ToSelect(Cards.Yellow);

                    if (Card.Status == SelectStatus.Selected && Args.Sender.ServerPosition.Distance(Me.ServerPosition) < 590)
                    {
                        Me.IssueOrder(GameObjectOrder.AttackUnit, Args.Sender);
                    }
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (sender.IsMe && Args.Slot == SpellSlot.R && Args.SData.Name.Equals("Gate", StringComparison.InvariantCultureIgnoreCase))
            {
                if (Menu["WMenu"]["AutoYellow"].GetValue<MenuBool>().Value && W.IsReady())
                {
                    Card.ToSelect(Cards.Yellow);
                }
            }
        }

        private static void OnEndScene(EventArgs Args)
        {
            var DrawRMin = Menu["DrawMenu"]["DrawRMin"].GetValue<MenuBool>().Value;

            if (R.IsReady() && DrawRMin)
            {
                // Part in LeagueSharp.Common
                var pointList = new List<Vector3>();
                for (var i = 0; i < 30; i++)
                {
                    var angle = i * Math.PI * 2 / 30;
                    pointList.Add(
                        new Vector3(
                            Me.Position.X + R.Range * (float)Math.Cos(angle), Me.Position.Y + R.Range * (float)Math.Sin(angle),
                            Me.Position.Z));
                }

                for (var i = 0; i < pointList.Count; i++)
                {
                    var a = pointList[i];
                    var b = pointList[i == pointList.Count - 1 ? 0 : i + 1];

                    var aonScreen = Drawing.WorldToMinimap(a);
                    var bonScreen = Drawing.WorldToMinimap(b);
                    var aon1Screen = Drawing.WorldToScreen(a);
                    var bon1Screen = Drawing.WorldToScreen(b);

                    Drawing.DrawLine(aon1Screen.X, aon1Screen.Y, bon1Screen.X, bon1Screen.Y, 1, System.Drawing.Color.White);
                    Drawing.DrawLine(aonScreen.X, aonScreen.Y, bonScreen.X, bonScreen.Y, 1, System.Drawing.Color.White);
                }
            }
        }

        private static void OnDraw(EventArgs Args)
        {
            var DrawQ = Menu["DrawMenu"]["DrawQ"].GetValue<MenuBool>().Value;
            var DrawW = Menu["DrawMenu"]["DrawW"].GetValue<MenuBool>().Value;
            var DrawR = Menu["DrawMenu"]["DrawR"].GetValue<MenuBool>().Value;
            var DrawAF = Menu["DrawMenu"]["DrawAF"].GetValue<MenuBool>().Value;
            var DrawDamage = Menu["DrawMenu"]["DrawComboDamage"].GetValue<MenuBool>().Value;

            if (Q.IsReady() && DrawQ)
            {
                Render.Circle.DrawCircle(Me.Position, Q.Range, System.Drawing.Color.AliceBlue);
            }

            if (DrawW)
            {
                System.Drawing.Color FlowersStyle = System.Drawing.Color.LightGreen;

                var WBuff = Me.Spellbook.GetSpell(SpellSlot.W).Name;

                if (WBuff == "GoldCardLock")
                {
                    FlowersStyle = System.Drawing.Color.Gold;
                }
                else if (WBuff == "BlueCardLock")
                {
                    FlowersStyle = System.Drawing.Color.Blue;
                }
                else if (WBuff == "RedCardLock")
                {
                    FlowersStyle = System.Drawing.Color.Red;
                }
                else if (WBuff == "PickACard")
                {
                    FlowersStyle = System.Drawing.Color.Teal;
                }

                Render.Circle.DrawCircle(Me.Position, 590, FlowersStyle, 1);
            }

            if (Flash.IsReady() && DrawAF)
            {
                Render.Circle.DrawCircle(Me.Position, 590 + 475, System.Drawing.Color.Gold, 1);
            }

            if (R.IsReady() && DrawR)
            {
                Render.Circle.DrawCircle(Me.Position, R.Range, System.Drawing.Color.White, 1);
            }

            if (DrawDamage)
            {
                foreach (var e in ObjectManager.Get<Obj_AI_Hero>().Where(e => e.IsValidTarget() && !e.IsZombie))
                {
                    DrawHpBar.Unit = e;
                    DrawHpBar.DrawDmg(GetComboDamage(e), new ColorBGRA(255, 204, 0, 170));
                }
            }
        }

        private static float GetComboDamage(Obj_AI_Hero target)
        {
            float Damage = 0f;

            if (Q.IsReady())
                Damage += Q.GetDamage(target);

            if (W.IsReady())
                Damage += W.GetDamage(target);

            return Damage;
        }

        private static bool HaveDebuff(Obj_AI_Hero target)
        {
            if (target.IsStunned || target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Snare) || 
                target.HasBuffOfType(BuffType.Knockup) || target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Charm) || 
                target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression))
            {
                return true;
            }
            else
                return false;
        }

        internal static int GetRamdonTime()
        {
            if (Menu["WMenu"]["EnableHumanizer"])
            {
                if (Menu["WMenu"]["LowHp"].GetValue<MenuSliderButton>().BValue && Me.HealthPercent <= Menu["WMenu"]["LowHp"].GetValue<MenuSliderButton>().SValue)
                {
                    return 0;
                }
                else
                    return Random.Next(Menu["WMenu"]["MinHumanizer"].GetValue<MenuSlider>().Value, Menu["WMenu"]["MaxHumanizer"].GetValue<MenuSlider>().Value);
            }
            else
                return 0;
        }
    }
}
