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
using MySql.Data.MySqlClient;
using ElKharis.Database;

namespace ElKharis.Views
{
    /// <summary>
    /// Logique d'interaction pour LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // Action du bouton Fermer (✕)
        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Action du bouton Se connecter
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password.Trim();

            // Validation rapide des champs vides
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Veuillez remplir tous les champs !", "Champs vides", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Connexion à la base de données et vérification
            using (MySqlConnection conn = DbConnection.GetConnection())
            {
                if (conn == null) return; // Si la connexion a échoué, on arrête.

                // Requête SQL sécurisée avec des paramètres
                string query = "SELECT COUNT(*) FROM utilisateurs WHERE nom_utilisateur = @username AND mot_de_passe = @password";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password); 

                    int result = Convert.ToInt32(cmd.ExecuteScalar());

                    if (result > 0)
                    {
                        MessageBox.Show("Connexion réussie ! Bienvenue chez El-Kharis.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                        // OUVRE LE DASHBOARD ET FERME LE LOGIN
                        DashboardWindow dashboard = new DashboardWindow();
                        dashboard.Show();

                        this.Close();
                    }
                    else
                    {
                        // Identifiants incorrects
                        MessageBox.Show("Nom d'utilisateur ou mot de passe incorrect.", "Échec de connexion", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
