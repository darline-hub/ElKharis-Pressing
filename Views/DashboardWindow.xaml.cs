using System;
using System.Collections.Generic;
using System.Data;
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
using ElKharis.Database;

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
            TxtUserNom.Text = Session.NomUtilisateur;
            TxtUserRole.Text = Session.Role;
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
        }

        private void BtnCommandes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. On crée une instance de ta fenêtre de commandes
                ElKharis.Views.CommandesWindow fenetreCommandes = new ElKharis.Views.CommandesWindow();

                // 2. On l'affiche
                fenetreCommandes.Show();

                // 3. (Optionnel) On ferme le dashboard actuel si tu ne veux pas cumuler les fenêtres
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des commandes : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close(); 
        }

        private void BtnQuitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
