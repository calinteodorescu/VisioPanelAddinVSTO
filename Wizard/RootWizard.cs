﻿
using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.Win32;

namespace PanelAddinWizard
{
	/// <summary>
	/// Summary description for WizardForm.
	/// </summary>
	public abstract class RootWizard 
        : IWizard
	{
        // Use to communicate $saferootprojectname$ to ChildWizard
        public static Dictionary<string, string> GlobalDictionary =
            new Dictionary<string, string>();

	    private DTE _dte;

	    protected abstract Image HeaderImage { get; }

        // Add global replacement parameters
        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            _dte = automationObject as DTE;

            if (_dte != null)
            {
                var vstoKey = string.Format(@"HKEY_LOCAL_MACHINE\{0}\Setup\VSTO", _dte.RegistryRoot);
                var val = Registry.GetValue(vstoKey, "ProductDir", null);
                
                if (null == val)
                {
                    MessageBox.Show(
                        "Visual Studio Tools for Office installation is not detected. " +
                        "This project template is based on VSTO project, thus it is required. " +
                        "Please install Tools for Office, or consder using COM Visio Addin project template.",
                        "Visio VSTO Panel Addin: VSTO not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                    throw new WizardBackoutException();
                }
            }

            var wizardForm = new WizardForm
            {
                TaskPane = true, 
                Ribbon = true,
                HeaderImage = HeaderImage
            };

            if (wizardForm.ShowDialog() == DialogResult.Cancel)
                throw new WizardBackoutException();

            GlobalDictionary["$csprojectname$"] = replacementsDictionary["$safeprojectname$"];
            GlobalDictionary["$progid$"] = replacementsDictionary["$safeprojectname$"] + ".Addin";
            GlobalDictionary["$clsid$"] = replacementsDictionary["$guid1$"];
            GlobalDictionary["$wixproject$"] = replacementsDictionary["$guid2$"];
            GlobalDictionary["$csprojectguid$"] = replacementsDictionary["$guid3$"];

            GlobalDictionary["$mergeguid$"] = replacementsDictionary["$guid4$"];

            GlobalDictionary["$ribbon$"] = wizardForm.Ribbon ? "true" : "false";
            GlobalDictionary["$commandbars$"] = wizardForm.CommandBars ? "true" : "false";

            GlobalDictionary["$ribbonORcommandbars$"] = wizardForm.Ribbon || wizardForm.CommandBars ? "true" : "false";
            GlobalDictionary["$ribbonANDcommandbars$"] = wizardForm.Ribbon && wizardForm.CommandBars ? "true" : "false";
            GlobalDictionary["$commandbarsANDtaskpane$"] = wizardForm.CommandBars && wizardForm.TaskPane ? "true" : "false";
            GlobalDictionary["$taskpane$"] = wizardForm.TaskPane ? "true" : "false";
            GlobalDictionary["$ui$"] = (wizardForm.CommandBars || wizardForm.Ribbon) ? "true" : "false";
            GlobalDictionary["$taskpaneANDui$"] = (wizardForm.TaskPane && (wizardForm.CommandBars || wizardForm.Ribbon)) ? "true" : "false";
            GlobalDictionary["$taskpaneORui$"] = (wizardForm.TaskPane || (wizardForm.CommandBars || wizardForm.Ribbon)) ? "true" : "false";

            GlobalDictionary["$office$"] = GetOfficeVersion();

            GlobalDictionary["$wixSetup$"] = wizardForm.GenerateWixSetup ? "true" : "false";
        }

        static void GetVisioPath(RegistryKey key, string version, ref string path)
        {
            var subKey = key.OpenSubKey(string.Format(@"Software\Microsoft\Office\{0}\Visio\InstallRoot", version));
            if (subKey == null)
                return;

            var value = subKey.GetValue("Path", null);
            if (value == null)
                return;

            path = Path.Combine(value.ToString(), "Visio.exe");
        }

        public static string GetVisioPath32()
        {
            var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            string path = null;
            foreach (var item in new[] { "11.0", "12.0", "14.0", "15.0", "16.0" })
                GetVisioPath(key, item, ref path);
            return path;
        }

        public static string GetVisioPath64()
        {
            if (!Environment.Is64BitOperatingSystem)
                return null;

            var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            string path = null;
            foreach (var item in new [] {"14.0", "15.0", "16.0"})
                GetVisioPath(key, item, ref path);
            return path;
        }

        // Don't return the latest version, even if it is installed, because it will cause problems by auto-upgrade
	    static string GetOfficeVersion()
	    {
            return Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Office\14.0\Visio\InstallRoot", "Path", null) != null
                ? "14.0" : "12.0";
	    }

        public void RunFinished()
        {
        }

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }
    }
}
