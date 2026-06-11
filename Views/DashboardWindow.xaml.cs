using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ElKharis.Views
{
    /// <summary>
    /// Logique d'interaction pour DashboardWindow.xaml
    /// </summary>
    public partial class DashboardWindow : Window
    {
        public DashboardWindow()
        {
            InitializeComponent();
            ChargerStatistiques();
        }

        // Simuler ou charger les statistiques depuis la base de données plus tard
        private void ChargerStatistiques()
        {
            // Pour l'instant, les valeurs par défaut du XAML s'affichent.
            // On ajoutera les requêtes SQL COUNT(*) ici pour dynamiser l'affichage.
        }

        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ouverture de la gestion des clients...", "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Lier vers ta fenêtre de gestion de clients quand elle sera créée
        }

        private void BtnCommandes_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ouverture de la gestion des commandes...", "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Lier vers ta fenêtre de gestion de commandes
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Retour au Login
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close(); // Ferme le dashboard
        }

        private void BtnQuitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
