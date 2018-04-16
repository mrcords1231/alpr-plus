using LSPD_First_Response.Mod.API;
using Rage;
using Stealth.Common.Functions;
using Stealth.Plugins.ALPRPlus.Common;
using Stealth.Plugins.ALPRPlus.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stealth.Plugins.ALPRPlus
{
    internal sealed class Main : Plugin
    {
        public override void Initialize()
        {
            Config.Init();
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LSPDFRResolveEventHandler);
            Functions.OnOnDutyStateChanged += Functions_OnOnDutyStateChanged;
        }

        internal static void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            if (!onDuty)
            {
                Globals.IsPlayerOnDuty = false;
                return;
            }

            if (!Funcs.PreloadChecks())
            {
                return;
            }

            if (Globals.IsBeta)
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    string fileSecretUUID = "26cb7dc4-f777-4848-996a-733ebbed9000";

                    bool isBetaKeyValid = await BetaFuncs.IsValidKey(Constants.LCPDFRDownloadID, fileSecretUUID, File.ReadAllText(""));

                    if (isBetaKeyValid)
                    {
                        StartPlugin(onDuty);
                    }
                    else
                    {
                        Logger.LogTrivial("ERROR: Beta key authorization failed!");
                        Funcs.DisplayNotification("BETA KEY CHECK", "~r~AUTHENTICATION FAILED!");
                    }
                });
            }
            else
            {
                StartPlugin(onDuty);
            }
        }

        private static void StartPlugin(bool onDuty)
        {
            Globals.IsPlayerOnDuty = onDuty;

            if (onDuty)
            {
                Logger.LogTrivial(String.Format("{0} v{1} has been loaded!", Globals.VersionInfo.ProductName, Globals.VersionInfo.FileVersion));

                GameFiber.StartNew(delegate
                {
                    GameFiber.Sleep(3000);
                    Funcs.DisplayNotification("~b~Developed By Stealth22", String.Format("~b~{0} v{1} ~w~has been ~g~loaded!", Globals.VersionInfo.ProductName, Globals.VersionInfo.FileVersion));
                    Funcs.CheckForUpdates();
                });

                Driver mDriver = new Driver();
                GameFiber.StartNew(new ThreadStart(mDriver.Launch));
                GameFiber.StartNew(new ThreadStart(mDriver.ListenForToggleKey));
                GameFiber.StartNew(new ThreadStart(mDriver.MonitorActiveTrafficStop));
                GameFiber.StartNew(new ThreadStart(mDriver.MonitorActivePursuit));
            }
        }

        public override void Finally()
        {
            Globals.IsPlayerOnDuty = false;
        }

        internal static Assembly LSPDFRResolveEventHandler(object sender, ResolveEventArgs args)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                if (args.Name.ToLower().Contains(assembly.GetName().Name.ToLower()))
                {
                    return assembly;
                }
            }
            return null;
        }
    }
}
