using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GloryHoleCutter
{
    public partial class GloryHoleCutterWPF : Window
    {
        public string UseSleevesForRoundHolesButtonName;
        GloryHoleCutterSettings GloryHoleCutterSettingsItem;
        public GloryHoleCutterWPF()
        {
            GloryHoleCutterSettingsItem = new GloryHoleCutterSettings().GetSettings();
            InitializeComponent();
            if (GloryHoleCutterSettingsItem.UseSleevesForRoundHolesButtonName != null)
            {
                if (GloryHoleCutterSettingsItem.UseSleevesForRoundHolesButtonName == "radioButton_UseSleevesForRoundHolesYes")
                {
                    radioButton_UseSleevesForRoundHolesYes.IsChecked = true;
                }
                else
                {
                    radioButton_UseSleevesForRoundHolesNo.IsChecked = true;
                }
            }
        }
        private void radioButton_UseSleevesForRoundHoles_Checked(object sender, RoutedEventArgs e)
        {
            UseSleevesForRoundHolesButtonName = (this.groupBox_UseSleevesForRoundHoles.Content as System.Windows.Controls.Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;
        }

        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            this.DialogResult = true;
            this.Close();
        }
        private void GloryHoleCutterWPF_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                SaveSettings();
                this.DialogResult = true;
                this.Close();
            }

            else if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }
        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void SaveSettings()
        {
            GloryHoleCutterSettingsItem = new GloryHoleCutterSettings();
            GloryHoleCutterSettingsItem.UseSleevesForRoundHolesButtonName = UseSleevesForRoundHolesButtonName;
            GloryHoleCutterSettingsItem.SaveSettings();
        }
    }
}
