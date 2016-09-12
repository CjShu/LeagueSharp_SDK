namespace Flowers_Series.Prediction
{
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using SharpDX;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class xcsoftPrediction
    {
        public static float PredHealth(Obj_AI_Base Target, Spell spell) => Health.GetPrediction(Target, (int)(GameObjects.Player.Distance(Target) / spell.Speed), (int)(spell.Delay * 1000 + Game.Ping / 2));

        public static void CastCircle(this Spell spell, Obj_AI_Base target)
        {
            if (spell.Type == SkillshotType.SkillshotCircle || spell.Type == SkillshotType.SkillshotCone)
            {
                if (target == null)
                {
                    return;
                }

                var pred = Movement.GetPrediction(target, spell.Delay, spell.Width / 2, spell.Speed);
                var castVec = (pred.UnitPosition.ToVector2() + target.ServerPosition.ToVector2()) / 2;
                var castVec2 = GameObjects.Player.ServerPosition.ToVector2() + Vector2.Normalize(pred.UnitPosition.ToVector2() - GameObjects.Player.Position.ToVector2()) * (spell.Range);

                if (target.IsValidTarget(spell.Range))
                {
                    if (
                        !(target.MoveSpeed*
                          (Game.Ping/2000 + spell.Delay +
                           GameObjects.Player.ServerPosition.Distance(target.ServerPosition)/spell.Speed) <=
                          spell.Width*1/2))
                    {
                        if (pred.Hitchance < HitChance.VeryHigh ||
                            !(pred.UnitPosition.Distance(target.ServerPosition) < Math.Max(spell.Width, 300f)))
                        {
                            return;
                        }

                        if (!(target.MoveSpeed*
                              (Game.Ping/2000 + spell.Delay +
                               GameObjects.Player.ServerPosition.Distance(target.ServerPosition)/spell.Speed) <=
                              spell.Width*2/3) || !(castVec.Distance(pred.UnitPosition) <= spell.Width*1/2) ||
                            !(castVec.Distance(GameObjects.Player.ServerPosition) <= spell.Range))
                            if (castVec.Distance(pred.UnitPosition) > spell.Width*1/2 &&
                                GameObjects.Player.ServerPosition.Distance(pred.UnitPosition) <= spell.Range)
                            {
                                spell.Cast(pred.UnitPosition);
                            }
                            else
                            {
                                spell.Cast(pred.CastPosition);
                            }
                        else
                            spell.Cast(castVec);
                    }
                    else
                    {
                        spell.Cast(target.ServerPosition);
                    }
                }
                else if (target.IsValidTarget(spell.Range + spell.Width / 2))
                {
                    if (pred.Hitchance < HitChance.VeryHigh ||
                        !(GameObjects.Player.ServerPosition.Distance(pred.UnitPosition) <=
                          spell.Range + spell.Width*1/2) ||
                        !(pred.UnitPosition.Distance(target.ServerPosition) < Math.Max(spell.Width, 400f)))
                    {
                        return;
                    }

                    if (!(GameObjects.Player.ServerPosition.Distance(pred.UnitPosition) <= spell.Range))
                    {
                        if (!(GameObjects.Player.ServerPosition.Distance(pred.UnitPosition) <=
                              spell.Range + spell.Width*1/2) || !(target.MoveSpeed*
                                                                  (Game.Ping/2000 + spell.Delay +
                                                                   GameObjects.Player.ServerPosition.Distance(
                                                                       target.ServerPosition)/spell.Speed) <=
                                                                  spell.Width/2))
                        {
                            return;
                        }

                        if (GameObjects.Player.Distance(castVec2) <= spell.Range)
                        {
                            spell.Cast(castVec2);
                        }
                    }
                    else
                    {
                        if (GameObjects.Player.ServerPosition.Distance(pred.CastPosition) <= spell.Range)
                        {
                            spell.Cast(pred.CastPosition);
                        }
                    }
                }
            }
        }

        public static void CastLine(this Spell spell, Obj_AI_Base target, float alpha = 0f, float colmini = float.MaxValue, bool HeroOnly = false, float BombRadius = 0f)
        {
            if (spell.Type != SkillshotType.SkillshotLine)
            {
                return;
            }

            if (target == null)
            {
                return;
            }

            var pred = Movement.GetPrediction(target, spell.Delay, spell.Width / 2, spell.Speed);
            var collision = spell.GetCollision(GameObjects.Player.ServerPosition.ToVector2(), new List<Vector2> { pred.CastPosition.ToVector2() });
            var minioncol = collision.Count(x => (HeroOnly == false ? x.IsMinion : (x is Obj_AI_Hero)));
            var EditedVec = pred.UnitPosition.ToVector2() - Vector2.Normalize(pred.UnitPosition.ToVector2() - target.ServerPosition.ToVector2()) * (spell.Width * 2 / 5);
            var EditedVec2 = (pred.UnitPosition.ToVector2() + target.ServerPosition.ToVector2()) / 2;
            var collision2 = spell.GetCollision(GameObjects.Player.ServerPosition.ToVector2(), new List<Vector2> { EditedVec });
            var minioncol2 = collision2.Count(x => (HeroOnly == false ? x.IsMinion : (x is Obj_AI_Hero)));
            var collision3 = spell.GetCollision(GameObjects.Player.ServerPosition.ToVector2(), new List<Vector2> { EditedVec2 });
            var minioncol3 = collision3.Count(x => (HeroOnly == false ? x.IsMinion : (x is Obj_AI_Hero)));

            if (pred.Hitchance >= HitChance.VeryHigh)
            {
                if (
                    !target.IsValidTarget(spell.Range -
                                          target.MoveSpeed*
                                          (spell.Delay +
                                           GameObjects.Player.Distance(target.ServerPosition)/spell.Speed) + alpha) ||
                    !(minioncol2 <= colmini) || !(pred.UnitPosition.Distance(target.ServerPosition) > spell.Width))
                {
                    if (
                        target.IsValidTarget(spell.Range -
                                             target.MoveSpeed*
                                             (spell.Delay +
                                              GameObjects.Player.Distance(target.ServerPosition)/spell.Speed) + alpha) &&
                        minioncol3 <= colmini && pred.UnitPosition.Distance(target.ServerPosition) > spell.Width/2)
                    {
                        spell.Cast(EditedVec2);
                    }
                    else if (
                        target.IsValidTarget(spell.Range -
                                             target.MoveSpeed*
                                             (spell.Delay +
                                              GameObjects.Player.Distance(target.ServerPosition)/spell.Speed) + alpha) &&
                        minioncol <= colmini)
                    {
                        spell.Cast(pred.CastPosition);
                    }
                    else if (false == spell.Collision && colmini < 1 && minioncol >= 1)
                    {
                        var FirstMinion =
                            collision.OrderBy(o => o.Distance(GameObjects.Player.ServerPosition)).FirstOrDefault();

                        if (FirstMinion != null &&
                            FirstMinion.ServerPosition.Distance(pred.UnitPosition) <= BombRadius/4)
                        {
                            spell.Cast(pred.CastPosition);
                        }
                    }
                }
                else
                {
                    spell.Cast(EditedVec);
                }
            }
        }

        public static void CastCone(this Spell spell, Obj_AI_Base target, float alpha = 0f, float colmini = float.MaxValue, bool HeroOnly = false)
        {
            if (spell.Type != SkillshotType.SkillshotCone)
            {
                return;
            }

            if (target == null)
            {
                return;
            }

            var pred = Movement.GetPrediction(target, spell.Delay, spell.Width / 2, spell.Speed);
            var collision = spell.GetCollision(GameObjects.Player.ServerPosition.ToVector2(), new List<Vector2> { pred.CastPosition.ToVector2() });
            var minioncol = collision.Count(x => (HeroOnly == false ? x.IsMinion : x is Obj_AI_Hero));

            if (target.IsValidTarget(spell.Range - target.MoveSpeed * (spell.Delay + GameObjects.Player.Distance(target.ServerPosition) / spell.Speed) + alpha) && minioncol <= colmini && pred.Hitchance >= HitChance.VeryHigh)
            {
                spell.Cast(pred.CastPosition);
            }
        }

        public static void CastAOE(this Spell spell, Obj_AI_Base target)
        {
            if (spell == null || target == null)
            {
                return;
            }

            var pred = Movement.GetPrediction(target, spell.Delay > 0 ? spell.Delay : 0.25f, spell.Range);

            if (pred.Hitchance >= HitChance.High && pred.UnitPosition.Distance(GameObjects.Player.ServerPosition) <= spell.Range)
            {
                spell.Cast();
            }
        }

        public static void RMouse(this Spell spell)
        {
            var ReverseVec = GameObjects.Player.ServerPosition.ToVector2() - Vector2.Normalize(Game.CursorPos.ToVector2() - GameObjects.Player.Position.ToVector2()) * (spell.Range);

            if (!spell.IsReady())
            {
                return;
            }

            spell.Cast(ReverseVec);
        }

        public static void NMouse(this Spell spell)
        {
            var NVec = GameObjects.Player.ServerPosition.ToVector2() + Vector2.Normalize(Game.CursorPos.ToVector2() - GameObjects.Player.Position.ToVector2()) * (spell.Range);

            if (!spell.IsReady())
            {
                return;
            }

            spell.Cast(NVec);
        }

        public static void RTarget(this Spell spell, Obj_AI_Base Target)
        {
            var ReverseVec = GameObjects.Player.ServerPosition.ToVector2() - Vector2.Normalize(Target.ServerPosition.ToVector2() - GameObjects.Player.Position.ToVector2()) * (spell.Range);

            if (!spell.IsReady())
            {
                return;
            }

            spell.Cast(ReverseVec);
        }

        public static void NTarget(this Spell spell, Obj_AI_Base Target)
        {
            var Vec = GameObjects.Player.ServerPosition.ToVector2() + Vector2.Normalize(Target.ServerPosition.ToVector2() - GameObjects.Player.Position.ToVector2()) * (spell.Range);

            if (!spell.IsReady())
            {
                return;
            }

            spell.Cast(Vec);
        }

        public static bool CanHit(this Spell spell, Obj_AI_Base T, float Drag = 0f)
        {
            return T.IsValidTarget(spell.Range + Drag - ((T.Distance(GameObjects.Player.ServerPosition) - spell.Range) / spell.Speed + spell.Delay) * T.MoveSpeed);
        }
    }
}
