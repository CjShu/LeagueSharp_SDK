namespace Flowers__TwistedFate
{
    using LeagueSharp;
    using LeagueSharp.SDK;
    using System;
    using System.Linq;

    public static class Card
    {
        public static Cards Select;
        public static int PickCardTickCount = 0;
        public static SelectStatus Status { get; set; }

        private static Spell W => Program.W;

        static Card()
        {
            Game.OnUpdate += CardSelect;
        }

        private static void CardSelect(EventArgs args)
        {
            var wName = ObjectManager.Get<Obj_GeneralParticleEmitter>().Where
                (x => x.IsValid && x.Name.Contains("TwistedFate_Base_W")).
                Select(x => x.Name).FirstOrDefault();
            var wState = GameObjects.Player.Spellbook.CanUseSpell(SpellSlot.W);

            if (wName != null && ObjectManager.Player.HasBuff("pickacard_tracker") && Variables.TickCount - PickCardTickCount > Program.GetRamdonTime())
            {
                if (Select == Cards.Blue &&
                    wName.Equals("TwistedFate_Base_W_BlueCard.troy", StringComparison.InvariantCultureIgnoreCase))
                {
                    W.Cast();
                }
                else if (Select == Cards.Yellow &&
                    wName.Equals("TwistedFate_Base_W_GoldCard.troy", StringComparison.InvariantCultureIgnoreCase))
                {
                    W.Cast();
                }
                else if (Select == Cards.Red && wName.
                    Equals("TwistedFate_Base_W_RedCard.troy", StringComparison.InvariantCultureIgnoreCase))
                {
                    W.Cast();
                }
            }
            else if (wName == null)
            {
                if (wState == SpellState.Ready)
                    Status = SelectStatus.Ready;
                else if ((wState == SpellState.Cooldown || wState == SpellState.Disabled ||
                    wState == SpellState.NoMana || wState == SpellState.NotLearned ||
                    wState == SpellState.Surpressed || wState == SpellState.Unknown)
                    && !IsSelect)
                    Status = SelectStatus.CoolDown;
                else if (IsSelect)
                    Status = SelectStatus.Selected;
            }
        }

        public static void ToSelect(Cards Card)
        {
            if (!GameObjects.Player.HasBuff("pickacard_tracker") && Status == SelectStatus.Ready)
            {
                Select = Card;
                if (Variables.TickCount - PickCardTickCount > 170 + Game.Ping / 2)
                {
                    W.Cast();
                    PickCardTickCount = Variables.TickCount;
                }
            }
            Select = Card;
        }

        private static bool IsSelect => GameObjects.Player.HasBuff("GoldCardPreAttack") || 
            GameObjects.Player.HasBuff("BlueCardPreAttack") || 
            GameObjects.Player.HasBuff("RedCardPreAttack");
    }
}
