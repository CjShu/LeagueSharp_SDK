namespace VST_Auto_Carry_Standalone_Graves
{
    using LeagueSharp.SDK;
    using System;

    static class Program
    {
        static void Main(string[] args)
        {
            Bootstrap.Init();
            Events.OnLoad += OnLoad;
        }

        private static void OnLoad(object sender, EventArgs e)
        {
            if (GameObjects.Player.ChampionName != "Graves")
            {
                return;
            }

            new Graves();
        }
    }
}
