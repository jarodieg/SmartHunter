﻿using SmartHunter.Core;
using SmartHunter.Core.Helpers;
using SmartHunter.Core.Windows;
using SmartHunter.Game.Data.ViewModels;
using SmartHunter.Game.Helpers;
using System;
using System.IO;
using System.Net.Http;
using System.Text;

using System.Linq;
using System.Windows;
using System.Windows.Input;
using SmartHunter.Game.Data;

namespace SmartHunter.Game
{
    public class MhwOverlay : Overlay
    {
        MhwMemoryUpdater m_MemoryUpdater;

        protected override bool ShowWindows
        {
            get
            {
                return ConfigHelper.Main.Values.Overlay.ShowWindows;
            }
        }

        public MhwOverlay(Window mainWindow, params WidgetWindow[] widgetWindows) : base(mainWindow, widgetWindows)
        {
            ConfigHelper.Main.Loaded += (s, e) => { UpdateWidgetsFromConfig(); };
            ConfigHelper.Localization.Loaded += (s, e) => { RefreshWidgetsLayout(); };
            ConfigHelper.MonsterData.Loaded += (s, e) => { RefreshWidgetsLayout(); };
            ConfigHelper.PlayerData.Loaded += (s, e) => { RefreshWidgetsLayout(); };

            if (!ConfigHelper.Main.Values.Debug.UseSampleData)
            {
                m_MemoryUpdater = new MhwMemoryUpdater();
            }
        }

        protected override void InputReceived(Key key, bool isDown)
        {
            foreach (var controlKeyPair in ConfigHelper.Main.Values.Keybinds.Where(keybind => keybind.Value == key))
            {
                HandleControl(controlKeyPair.Key, isDown);
            }
        }

        private void HandleControl(InputControl control, bool isDown)
        {
            if (control == InputControl.ManipulateWidget && isDown && !OverlayViewModel.Instance.CanManipulateWindows)
            {
                OverlayViewModel.Instance.CanManipulateWindows = true;

                if (!ShowWindows)
                { 
                    // Make all the windows selectable
                    foreach (var widgetWindow in WidgetWindows)
                    {
                        WindowHelper.SetTopMostSelectable(widgetWindow as Window);
                    }
                }
            }
            else if (control == InputControl.ManipulateWidget && !isDown && OverlayViewModel.Instance.CanManipulateWindows)
            {
                OverlayViewModel.Instance.CanManipulateWindows = false;

                bool canSaveConfig = false;

                // Return all windows to their click through state
                foreach (var widgetWindow in WidgetWindows)
                {
                    if (!ShowWindows)
                    {
                        WindowHelper.SetTopMostTransparent(widgetWindow as Window);
                    }

                    if (widgetWindow.Widget.CanSaveConfig)
                    {
                        canSaveConfig = true;
                        widgetWindow.Widget.CanSaveConfig = false;
                    }
                }

                if (canSaveConfig)
                {
                    ConfigHelper.Main.Save();
                }
            }
            else if (control == InputControl.HideWidgets)
            {
                OverlayViewModel.Instance.HideWidgetsRequested = isDown;
            }
            else if (control == InputControl.SendDataToDiscord && isDown)
            {
                // Build the body with info from the MhwHelper

                String teamInfo = "";

                foreach( Player player in MhwHelper.TeamInfo.TeamList)
                {
                    int damage = player.Damage;
                    int damageFraction = (int)(player.DamageFraction*100);

                    teamInfo += player.Name + " " + damage + " " + damageFraction + "% \\n";
                }

                if (teamInfo == "")
                {
                    teamInfo = "No habéis dado ni una, payasos\\n";
                }

                String body = "{\"damage\" : \""+teamInfo+"\\n ========================== \\n\"}";
                this.post(body);

            }
        }


        public void post(string body)
        {

            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "http://localhost:3000/discord/damages/"))
                {
                    //request.Headers.TryAddWithoutValidation("Authorization", "6af7d2d213a3ba5e9bc64b80e02b000");
                    //request.Headers.TryAddWithoutValidation("OrgId", "671437200");

                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                    var response = httpClient.SendAsync(request).Result;
                    Console.WriteLine(response);
     
                }
            }

        }
    }
}
