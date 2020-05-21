using Microsoft.Win32;
using NAudio.CoreAudioApi;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace QuietOnTheSetUI
{
    public partial class Form1 : Form
    {
        private readonly MMDeviceEnumerator _mmde = new MMDeviceEnumerator();
        private readonly MMDevice _mmDevice;
        private bool _isLocked;
        private string _password;
        private int _maxVolume;
        private bool _exitAllowed;

        public Form1()
        {
            InitializeComponent();

            Icon = Properties.Resources.appicon;
            _mmDevice = _mmde.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            notifyIcon1.Icon = Properties.Resources.appicon;
            volumeTrackBar.ValueChanged += VolumeTrackBar_ValueChanged;
            _mmDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
            _maxVolume = Convert.ToInt16(Properties.Settings.Default["MaxVolume"]);
            volumeTrackBar.Value = _maxVolume;
            _isLocked = Convert.ToBoolean(Properties.Settings.Default["IsLocked"]);
            _password = Properties.Settings.Default["UnlockCode"].ToString();
            currentVolumeLabel.Text = Convert.ToInt16(_mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100).ToString();
            notifyIcon1.BalloonTipTitle = @"Quiet on the Set";

            if (_isLocked)
            {
                LockVolume(true);
            }
            else
            {
                UnlockVolume();
            }

            FormClosing += Form1_FormClosing;
            Resize += Form1_Resize;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(500);
                Hide();
            }
            else if (FormWindowState.Normal == WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_exitAllowed == false)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
            }
        }

        private void LockVolume (bool initializing=false)
        {
            _isLocked = true;
            lockButton.Text = @"Unlock";
            volumeTrackBar.Enabled = false;
            if (!initializing)
            {
                _maxVolume = volumeTrackBar.Value;
                Properties.Settings.Default["MaxVolume"] = _maxVolume.ToString();
                Properties.Settings.Default["IsLocked"] = true;
                Properties.Settings.Default["UnlockCode"] = passwordTextBox.Text;
                Properties.Settings.Default.Save();
            }
            _password = passwordTextBox.Text;
            passwordTextBox.Text = string.Empty;
            confirmPasswordTextBox.Text = string.Empty;
            notifyIcon1.BalloonTipText = BalloonTipText;
            notifyIcon1.Text = BalloonTipText;
            if (_password.Length > 0) { lockButton.Enabled = false; }
            exitButton.Enabled = false;
            SetMaxVolume();
        }

        private void UnlockVolume()
        {
            _isLocked = false;
            lockButton.Text = @"Lock";
            volumeTrackBar.Enabled = true;
            Properties.Settings.Default["IsLocked"] = false;
            Properties.Settings.Default["UnlockCode"] = string.Empty;
            Properties.Settings.Default.Save();
            passwordTextBox.Text = string.Empty;
            confirmPasswordTextBox.Text = string.Empty;
            notifyIcon1.BalloonTipText = BalloonTipText;
            notifyIcon1.Text = BalloonTipText;
            exitButton.Enabled = true;
            _password = string.Empty;
        }

        private string BalloonTipText => _isLocked ? $"Maximum volume locked at {volumeTrackBar.Value}" : "No maximum volume is currently set";

        private void SetMaxVolume()
        {
            if (_mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar > _maxVolume / 100f)
            {
                _mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar = _maxVolume / 100f;
            }
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            var newVolume = Convert.ToInt32(data.MasterVolume * 100);
            if (_isLocked && newVolume >  _maxVolume)
            {
                SetMaxVolume();
            }

            currentVolumeLabel.Invoke(new MethodInvoker(delegate { currentVolumeLabel.Text = newVolume.ToString(); }));
        }

        private void VolumeTrackBar_ValueChanged(object sender, EventArgs e)
        {
            maxVolumeLabel.Text = volumeTrackBar.Value.ToString();
        }

        private void volumeTrackBar_Scroll(object sender, EventArgs e)
        {
            var trackBar = (TrackBar)sender;
            maxVolumeLabel.Text = trackBar.Value.ToString();
        }

        private void lockButton_Click(object sender, EventArgs e)
        {
            if (_isLocked)
            {
                UnlockVolume();
            }
            else
            {
                LockVolume();
            }
        }

        private void passwordTextBox_TextChanged(object sender, EventArgs e)
        {
            ValidatePasswords();
        }

        private void confirmPasswordTextBox_TextChanged(object sender, EventArgs e)
        {
            ValidatePasswords();
        }

        private void ValidatePasswords()
        {
            if (_isLocked)
            {
                lockButton.Enabled = passwordTextBox.Text.Equals(confirmPasswordTextBox.Text) && passwordTextBox.Text.Equals(_password);
            }
            else
            {
                lockButton.Enabled = passwordTextBox.Text.Equals(confirmPasswordTextBox.Text);
            }
        }

        private void showPasswordCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            var isChecked = ((CheckBox)sender).Checked;
            passwordTextBox.UseSystemPasswordChar = !isChecked;
            confirmPasswordTextBox.UseSystemPasswordChar = !isChecked;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Show must be called before setting WindowState,
            // otherwise the window loses its size and position
            Show();
            WindowState = FormWindowState.Normal;
            MaximizedFromTray();
        }

        private void MaximizedFromTray()
        {
            notifyIcon1.Visible = false;
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            var rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (checkBox1.Checked)
            {
                rk?.SetValue("QuietOnTheSet", Application.ExecutablePath);
            }
            else
            {
                rk?.DeleteValue("QuietOnTheSet", false);
            }
        }

    }
}
