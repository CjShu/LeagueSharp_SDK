namespace Flowers_Series
{
    using Common;
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using Plugings;
    using System;
    using System.Linq;
    using System.Reflection;

    public class Program
    {
        public static Menu Menu { get; set; }
        public static Obj_AI_Hero Me { get; set; }
        public static Menu ChampionMenu { get; set; }

        private static readonly string[] SupportList = 
        {
            "Ahri", "Akali", "Ashe", "Blitzcrank", "Darius", "Ezreal", "Graves",
            "Hecarim", "Illaoi", "Karma", "Morgana", "Riven", "Ryze", "Sivir", "Tristana",
            "TwistedFate", "Twitch", "Vayne", "Viktor", "Vladimir"
        };

        static void Main(string[] Args)
        {
            Bootstrap.Init(Args);
            Events.OnLoad += Events_OnLoad;
        }

        private static void Events_OnLoad(object obj, EventArgs Args)
        {
            if (!SupportList.Contains(GameObjects.Player.ChampionName))
            {
                Manager.WriteConsole(GameObjects.Player.ChampionName + " Not Support!", true);
                DelayAction.Add(2000, () => Variables.Orbwalker.Enabled = false);
                return;
            }

            Manager.WriteConsole(GameObjects.Player.ChampionName + " Load!  Version: " + Assembly.GetExecutingAssembly().GetName().Version, true);

            Me = GameObjects.Player;

            Menu = new Menu("Flowers_Series", "Flowers' Series", true).Attach();
            Menu.Add(new MenuSeparator("Credit", "Credit: NightMoon"));
            Menu.Add(new MenuSeparator("Version", "Version: " + Assembly.GetExecutingAssembly().GetName().Version));

            Utility.Tools.Inject();

            var PredMenu = Menu.Add(new Menu("Prediction", "Prediction"));
            {
                PredMenu.Add(new MenuList<string>("SelectPred", "Select Prediction: ", new[] { "Logic Prediction", "SDK Prediction", "OKTW Prediction", "xcsoft AIO Prediction" }));
                PredMenu.Add(new MenuSeparator("AboutLogPred", "Logic Prediction -> More Faster Cast"));
                PredMenu.Add(new MenuSeparator("AboutSDKPred", "SDK Prediction -> LeagueSharp.SDKEx Prediction"));
                PredMenu.Add(new MenuSeparator("AboutOKTWPred", "OKTW Prediction -> Sebby' Prediction"));
                PredMenu.Add(new MenuSeparator("AboutxcsoftAIOPred", "xcsoft AIO Prediction -> xcsoft ALL In One Prediction"));
            }

            ChampionMenu = Menu.Add(new Menu(GameObjects.Player.ChampionName, GameObjects.Player.ChampionName));

            switch (Me.ChampionName)
            {
                case "Ahri":
                    Ahri.Init();
                    break;
                case "Akali":
                    Akali.Init();
                    break;
                case "Ashe":
                    Ashe.Init();
                    break;
                case "Blitzcrank":
                    Blitzcrank.Init();
                    break;
                case "Darius":
                    Darius.Init();
                    break;
                case "Ezreal":
                    Ezreal.Init();
                    break;
                case "Graves":
                    Graves.Init();
                    break;
                case "Hecarim":
                    Hecarim.Init();
                    break;
                case "Illaoi":
                    Illaoi.Init();
                    break;
                case "Karma":
                    Karma.Init();
                    break;
                case "Morgana":
                    Morgana.Init();
                    break;
                case "Riven":
                    Riven.Init();
                    break;
                case "Ryze":
                    Ryze.Init();
                    break;
                case "Sivir":
                    Sivir.Init();
                    break;
                case "Tristana":
                    Tristana.Init();
                    break;
                case "TwistedFate":
                    TwistedFate.Init();
                    break;
                case "Twitch":
                    Twitch.Init();
                    break;
                case "Vayne":
                    Vayne.Init();
                    break;
                case "Viktor":
                    Viktor.Init();
                    break;
                case "Vladimir":
                    Vladimir.Init();
                    break;
            }
        }
    }
}
