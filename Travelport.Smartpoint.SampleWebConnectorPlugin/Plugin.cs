using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using Travelport.MvvmHelper;
using Travelport.Smartpoint.Common;
using Travelport.Smartpoint.Controls;
using Travelport.Smartpoint.Helpers.Core;
using Travelport.Smartpoint.Helpers.UI;

namespace Travelport.Smartpoint.SampleWebConnectorPlugin
{
    [SmartPointPlugin(
         Id = "{F6367AA2-251B-4BED-923D-5CD436BEC7CC}",
         Organisation = "{0613EB37-52BB-4B9E-9F8A-1447CA70677D}",
         Developer = "Budi Mulyawan", Company = "Travelport, Inc",
         Description = "SDK WEB CONNECTOR DEMO")]
    public class Plugin : HostPlugin
    {

        private const string BROWSER_ID = "DEMO_WEB_CONNECTOR";
        /// <summary>
        /// Executes the load actions for the plugin.
        /// </summary>
        public override void Execute()
        {
            // Attach a handler to execute when the Smartpoint application is ready and all windows are loaded.
            CoreHelper.Instance.OnSmartpointReady += this.OnSmartpointReady;
        }

        #region Handlers

        /// <summary>
        /// Handles the Smartpoint Ready event of the Instance control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Travelport.Smartpoint.Helpers.Core.CoreHelperEventArgs"/> instance containing the event data.</param>
        private void OnSmartpointReady(object sender, CoreHelperEventArgs e)
        {
            // Hook into any terminal commands we are interested in
            // Commands entered by the user before they are executed
            CoreHelper.Instance.TerminalCommunication.OnTerminalPreSend += this.OnTerminalPreSend;

            // Output before it is displayed
            CoreHelper.Instance.TerminalCommunication.OnTerminalPreResponse += this.OnTerminalPreResponse;

            addCustomToolbarTerminalItem();
        }

        /// <summary>
        /// Traps plugin specific cryptic entries and process them.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Arguments from the terminal.</param>
        private void OnTerminalPreSend(object sender, TerminalEventArgs e)
        {
        }

        /// <summary>
        /// Have terminal output
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The terminal event arguments</param>
        private void OnTerminalPreResponse(object sender, TerminalEventArgs e)
        {
        }

        internal ISmartBrowserWindow GetSmartBrowserWindowById(string id)
        {
            ISmartBrowserWindow window = null;

            if (UIHelper.Instance.SmartpointWindows.Any(p => p.SmartWindowID == id))
            {
                window = UIHelper.Instance.SmartpointWindows.OfType<ISmartBrowserWindow>().FirstOrDefault(p => p.SmartWindowID == id);
            }

            return window;
        }

        private void addCustomToolbarTerminalItem()
        {
            // Create a SmartCustomToolbarButton
            var button = new SmartCustomToolbarButton();
            button.Style = (Style)button.FindResource("CustomToolbarButton");
            // Add a command
            button.Command = new RelayCommand((a) =>
            {
                    LoadPortal();
            });

            // Set the default content 
            button.ButtonContentDefault = "WEB";

            // Set the default content 
            button.ButtonContentSelected = button.ButtonContentDefault;

            //Set the Hover button content
            button.ButtonContentHover = new TextBlock(new Bold(new Run("WEB CONNECTOR Plugin")));

            // Set the tooltip
            button.ToolTip = "SDK WEB CONNECTOR Plugin";

            button.Content = "WEB";
            UIHelper.Instance.CurrentSmartTerminalWindow.CustomToolbar.Add(button);
        }

        private void LoadPortal()
        {
            var url = GetConfigValue("URL");
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            var window = CreateWebBrowserWindow(new Uri(url), BROWSER_ID, String.Format("SDK Web Connector v{0}", version));
            var wb = window.Content as SmartBrowserControl;
            var bf = UIHelper.Instance.CurrentTEControl.Connection.CommunicationFactory.RetrieveCurrentBookingFile();
            var names = !String.IsNullOrEmpty(bf?.RecordLocator) ?
                bf.Passengers.Select(p => p.FirstName + " " + p.LastName)
                .Aggregate((a, b) => a + System.Environment.NewLine + b) : String.Empty;

            wb.WebBrowserControl.FrameLoadEnd += (sender, eventArgs) =>
            {
                var htmlElement = GetConfigValue("PasteHTMLElement");

                var js = String.Format("document.querySelector('{0}').value = '{1}'", htmlElement, names);

                var host = eventArgs.Browser.GetHost();
                if (host != null)
                {
                    host.ShowDevTools();
                }
                //Wait for the MainFrame to finish loading
                if (eventArgs.Frame.IsMain)
                {
                    eventArgs.Frame.ExecuteJavaScriptAsync(js);
                }
            };
            window.Show();
        }
        /// <summary>
        /// Create a standard web browser window
        /// </summary>
        /// <param name="url">Url to be opened in the browser</param>
        /// <param name="browserId">Browser identificator</param>
        /// <param name="title">Title to be displayed in the browser header</param>
        /// <returns></returns>
        private ISmartBrowserWindow CreateWebBrowserWindow(Uri url, string browserId, string title)
        {
            if (string.IsNullOrEmpty(browserId))
            {
                browserId = "Browser";
            }

            var browserWindow = UIHelper.Instance.CreateNewSmartBrowserWindow(browserId);
            browserWindow.WindowStyle = new WindowStyle();
            browserWindow.Title = title;
            browserWindow.NoClose = false;
            browserWindow.Width = 1350;
            browserWindow.Height = 750;
            browserWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            browserWindow.Owner = UIHelper.Instance.GetOwnerWindow(UIHelper.Instance.CurrentTEControl.SmartTerminalWindow);

            var wb = browserWindow.Content as SmartBrowserControl;

            wb = new SmartBrowserControl();
            try
            {
                wb.WebBrowserControl.RegisterJsObject("spHelper", this);
            }
            catch (Exception ex)
            {

            }
            wb.NavigateTo(url);

            browserWindow.Content = wb;

            browserWindow.Closed += delegate
            {
                wb.Close();
            };
            wb.WebBrowserControl.FrameLoadEnd += (sender, eventArgs) =>
            {
                var host = eventArgs.Browser.GetHost();
                host.ShowDevTools();

                wb.WebBrowserControl.Dispatcher.BeginInvoke(
               DispatcherPriority.Normal,
               new Action(() => { browserWindow.Title = wb.WebBrowserControl.Title; }));

            };

            return browserWindow;
        }


        public void sendTerminalCommand(string command)
        {
            UIHelper.Instance.CurrentTEControl.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(
                    () => { SendTerminalCommand(command); }
                    ));
        }

        /// <summary>
        /// Send a command to Smart point to execute
        /// </summary>
        /// <param name="command">The command line to send to Smart point</param>
        /// <param name="showInTE">If Smart point should display the command in the output window, default true</param>
        public static void SendTerminalCommand(string command, bool showInTE = true)
        {
            // Sends the command to Host
            CoreHelper.Instance.SendHostCommand(command, UIHelper.Instance.CurrentTEControl, showInTE);
        }

        private string GetConfigValue(string key)
        {
            var configuration = GetDllConfiguration(this.GetType().Assembly);
            var section = (System.Configuration.ClientSettingsSection)(configuration.GetSection("applicationSettings/Travelport.Smartpoint.SampleWebConnectorPlugin.Properties.Production"));
            return section.Settings.Get(key).Value.ValueXml.LastChild.InnerText.ToString();
        }

        private Configuration GetDllConfiguration(Assembly targetAsm)
        {
            var configFile = targetAsm.Location + ".config";
            var map = new ExeConfigurationFileMap
            {
                ExeConfigFilename = configFile
            };
            return ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
        }

        #endregion
    }
}
