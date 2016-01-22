using InstallerAnalyzer1_Host.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace InstallerAnalyzer1_Host.VMImpl
{
    public class VBoxVM : VirtualMachine
    {

        private string _name;
        private VMStatus _status;

        #region Properies
        public string Name
        {
            get
            {
                return _name;
            }
        }
        public VMStatus Status
        {
            get { return _status; }
        }
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        public VBoxVM(string name)
        {
            _name = name;
            _status = VMStatus.Uninitialized;
        }

        /*
        /// <summary>
        /// Creates a new VM given the name. It also set the Unique UUID.
        /// </summary>
        public void Create()
        {
            lock (this)
            {
                // Setting Base disk as immutable...
                Process p = new Process();
                p.StartInfo.FileName = VBOX_BIN_PATH;
                p.StartInfo.Arguments = "modifyhd " + VM_BASE_VDI_PATH + " --type immutable";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                p.WaitForExit();
                string output = p.StandardOutput.ReadToEnd();
                string err = p.StandardError.ReadToEnd();

                // If the VM is already present, shutdown and remove it
                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = VBOX_BIN_PATH;
                p.StartInfo.Arguments = "controlvm " + _name + " poweroff";
                p.Start();
                p.WaitForExit();
                output = p.StandardOutput.ReadToEnd();
                err = p.StandardError.ReadToEnd();

                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = VBOX_BIN_PATH;
                p.StartInfo.Arguments = "unregistervm " + _name + " --delete";
                p.Start();
                p.WaitForExit();
                output = p.StandardOutput.ReadToEnd();
                err = p.StandardError.ReadToEnd();

                try { Directory.Delete(VM_DIR_PATH + "\\" + _name, true); }
                catch (Exception e) { }
                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = VBOX_BIN_PATH;
                p.StartInfo.Arguments = "createvm --basefolder " + VM_DIR_PATH + " --name " + _name + " --ostype Windows7 --register";
                p.Start();
                p.WaitForExit();
                output = p.StandardOutput.ReadToEnd();
                err = p.StandardError.ReadToEnd();
                _uuid = output.Split(new String[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1].Split(new string[] { "\r" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = VBOX_BIN_PATH;
                p.StartInfo.Arguments = "modifyvm " + _name + " --memory " + VM_MEMORY + " --cpus " + VM_CPUS;
                p.Start();
                p.WaitForExit();
                output = p.StandardOutput.ReadToEnd();
                err = p.StandardError.ReadToEnd();

                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = VBOX_BIN_PATH;
                p.StartInfo.Arguments = "storagectl " + _name + " --name controller --add sata";
                p.Start();
                p.WaitForExit();
                output = p.StandardOutput.ReadToEnd();
                err = p.StandardError.ReadToEnd();


                // If the media exists, delete it
                if (File.Exists(VM_VDI_DIR_PATH + "\\" + _name + ".vdi"))
                    File.Delete((VM_VDI_DIR_PATH + "\\" + _name + ".vdi"));

                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = VBOX_BIN_PATH;
                p.StartInfo.Arguments = "createhd --filename \"" + VM_VDI_DIR_PATH + "\\" + _name + ".vdi\" --diffparent " + VM_BASE_VDI_PATH + " --format VDI";
                p.Start();
                p.WaitForExit();
                output = p.StandardOutput.ReadToEnd();
                err = p.StandardError.ReadToEnd();

                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = VBOX_BIN_PATH;
                p.StartInfo.Arguments = "modifyhd \"" + VM_VDI_DIR_PATH + "\\" + _name + ".vdi\" --autoreset on";
                p.Start();
                p.WaitForExit();
                output = p.StandardOutput.ReadToEnd();
                err = p.StandardError.ReadToEnd();

                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = VBOX_BIN_PATH;
                p.StartInfo.Arguments = "storageattach " + _name + " --type hdd --storagectl controller --port 0 --device 0 --medium \"" + VM_VDI_DIR_PATH + "\\" + _name + ".vdi\"";
                p.Start();
                p.WaitForExit();
                output = p.StandardOutput.ReadToEnd();
                err = p.StandardError.ReadToEnd();

                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = VBOX_BIN_PATH;
                p.StartInfo.Arguments = "guestproperty set \"" + _name + "\" VMNAME \"" + _name + "\"";
                p.Start();
                p.WaitForExit();
                output = p.StandardOutput.ReadToEnd();
                err = p.StandardError.ReadToEnd();

                Start();

                _status = VMStatus.Ready;
            }
        }
        */

        /// <summary>
        /// This method will create a new VM by simply cloning a given one.
        /// </summary>
        /// <param name="vmToClone"></param>
        public void InitiateByCloning()
        {
            lock (this)
            {
                Process p = new Process();
                ProcessStartInfo info = p.StartInfo;

                // Check if the machine file already exists. If so, skip everything! The machine is already cloned.
                if (Directory.Exists(Path.Combine(Settings.Default.VM_DIR_PATH, _name)))
                {
                    // check if that VM is already registered. If not, delete that directory and proceed with cloning
                    info.FileName = Settings.Default.VBOX_BIN_PATH;
                    info.Arguments = "showvminfo \"" + _name + "\"";
                    info.UseShellExecute = false;
                    info.RedirectStandardError = true;
                    info.RedirectStandardOutput = true;
                    p.Start();
                    p.WaitForExit();
                    if (p.ExitCode == 0)
                    {
                        Log("I have already found a registered machine with the specified name. I will skip cloning and use that after reverting it.");
                        _status = VMStatus.Ready;
                        Revert();
                        return;
                    }
                    else
                    {
                        // Machine not found!
                        Log("I have already found a machine with the specified name. but that machine seems to be unregistered. I will delete that folder and continue with cloning.");
                        Directory.Delete(Path.Combine(Settings.Default.VM_DIR_PATH, _name), true);
                    }
                }

                p = new Process();
                info = p.StartInfo;
                info.FileName = Settings.Default.VBOX_BIN_PATH;
                info.Arguments = "clonevm \"" + Settings.Default.BASE_VM_NAME + "\" --mode all --name \"" + _name + "\" --basefolder \"" + Settings.Default.VM_DIR_PATH + "\" --register";
                info.UseShellExecute = false;
                info.RedirectStandardError = true;
                info.RedirectStandardOutput = true;
                System.Timers.Timer t = new System.Timers.Timer();
                t.Interval = 1000;
                p.Exited += new EventHandler(delegate(object sender, EventArgs args)
                {
                    t.Stop();
                    t.Dispose();
                });
                p.Start();
                t.Enabled = true;
                t.Elapsed += new System.Timers.ElapsedEventHandler(delegate(object sender, System.Timers.ElapsedEventArgs args)
                {
                    Log('.');
                });


                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    Console.WriteLine("Cannot create the VM by cloning the previous one.");
                    throw new ApplicationException("Cannot create a VM by cloning the given one. \n" + p.StandardError.ReadToEnd());
                }
                else
                    Log("VM Cloned succesfully.");

                _status = VMStatus.Ready;
            }
        }

        private void Log(string p)
        {
            lock (Console.Out)
            {
                Console.WriteLine(p);
            }
        }

        private void Log(char p)
        {
            lock (Console.Out)
            {
                Console.Write(p);
            }
        }

        /// <summary>
        /// Resets the VM to the Vanilla snapshot. It will restart the VM.
        /// </summary>
        public void Revert()
        {
            lock (this)
            {
                bool revertOk = false;

                while (!revertOk)
                {
                    Process p = new Process();
                    ProcessStartInfo info = p.StartInfo;
                    info.FileName = Settings.Default.VBOX_BIN_PATH;
                    info.Arguments = "controlvm \"" + _name + "\" poweroff";
                    info.UseShellExecute = false;
                    info.RedirectStandardError = true;
                    info.RedirectStandardOutput = true;
                    p.Start();
                    p.WaitForExit();

                    p = new Process();
                    info = p.StartInfo;
                    info.FileName = Settings.Default.VBOX_BIN_PATH;
                    info.Arguments = "snapshot \"" + _name + "\" restorecurrent";
                    info.UseShellExecute = false;
                    info.RedirectStandardError = true;
                    info.RedirectStandardOutput = true;
                    p.Start();
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        String err = p.StandardError.ReadToEnd();
                        String output = p.StandardOutput.ReadToEnd();
                        Log("Cannot restore current VM state. VM " + _name);
                        Log("Output: " + output);
                        Log("Error: " + err);
                        revertOk = false;
                        Thread.Sleep(2000);
                        continue;
                    }

                    revertOk = true;
                    p.Dispose();
                }
                // If all had gone ok, change status
                _status = VMStatus.Ready;
            }
        }


        public void Start()
        {
            lock (this)
            {
                if (_status == VMStatus.Uninitialized)
                    throw new ApplicationException("You must first initialize this VM. See InitiateByCloning() method.");

                Process p = new Process();
                ProcessStartInfo info = p.StartInfo;
                info.FileName = Settings.Default.VBOX_BIN_PATH;
                info.Arguments = "startvm \"" + _name + "\"";
                info.UseShellExecute = false;
                info.RedirectStandardError = true;
                info.RedirectStandardOutput = true;
                p.Start();
                p.WaitForExit();
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();

                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = Settings.Default.VBOX_BIN_PATH;
                p.StartInfo.Arguments = "guestproperty set \"" + _name + "\" VMNAME \"" + _name + "\"";
                p.Start();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    Log("Cannot set the VM name property.");
                    throw new ApplicationException("Cannot set the VM name property.");
                }

                // Now disable and then re-enable network to start the trigger
                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = Settings.Default.VBOX_BIN_PATH;
                p.StartInfo.Arguments = "controlvm \"" + _name + "\" setlinkstate1 off";
                p.Start();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    Log("Cannot shutdown network");
                    throw new ApplicationException("Cannot shutdown network");
                }

                // Now run the baby
                p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = Settings.Default.VBOX_BIN_PATH;
                p.StartInfo.Arguments = "controlvm \"" + _name + "\" setlinkstate1 on";
                p.Start();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    Log("Cannot start network");
                    throw new ApplicationException("Cannot start network");
                }

                p.Dispose();



                // If all had gone ok, change status
                _status = VMStatus.Running;
            }
        }



        public VMStatus GetStatus()
        {
            throw new NotImplementedException();
        }
    }
}
