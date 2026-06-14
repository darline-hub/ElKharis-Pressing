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
using BCrypt.Net;

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

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password.Trim();

            // 1. Validation de surface
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Veuillez remplir tous les champs !", "Champs vides", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection? conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    // S'assurer que la connexion est bien ouverte
                    if (conn.State != System.Data.ConnectionState.Open)
                    {
                        conn.Open();
                    }

                    // CORRECTION : On retire 'AND mot_de_passe = @password' de la requête SQL.
                    // On demande en plus la colonne 'mot_de_passe' pour pouvoir faire la comparaison en C#.
                    string query = "SELECT id, nom_utilisateur, role, email, mot_de_passe FROM utilisateurs WHERE nom_utilisateur = @username";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            // L'utilisateur existe en base de données
                            if (reader.Read())
                            {
                                // Récupération du mot de passe haché stocké dans la table
                                string hashedPasswordFromDb = reader.GetString("mot_de_passe");

                                // CORRECTION : Utilisation de BCrypt pour valider le mot de passe tapé
                                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, hashedPasswordFromDb);

                                if (isPasswordValid)
                                {
                                    // Hydratation de la Session globale de l'application
                                    Session.IdUtilisateur = reader.GetInt32("id");
                                    Session.NomUtilisateur = reader.GetString("nom_utilisateur");
                                    Session.Role = reader.GetString("role");
                                    Session.Email = reader.GetString("email");

                                    MessageBox.Show($"Connexion réussie ! Bienvenue {Session.NomUtilisateur}.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                                    // Ouverture du tableau de bord
                                    DashboardWindow dashboard = new DashboardWindow();
                                    dashboard.Show();

                                    this.Close();
                                }
                                else
                                {
                                    // Le mot de passe haché ne correspond pas
                                    MessageBox.Show("Nom d'utilisateur ou mot de passe incorrect.", "Échec de connexion", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                // Le nom d'utilisateur n'existe pas du tout
                                MessageBox.Show("Nom d'utilisateur ou mot de passe incorrect.", "Échec de connexion", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la connexion à la base de données : " + ex.Message, "Erreur Critique", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnAllerAInscription_Click(object sender, RoutedEventArgs e)
        {
            SigninWindow signinWin = new SigninWindow();
            signinWin.Owner = this; 
            this.Hide();
            bool? result = signinWin.ShowDialog();
            this.Show();

            if (result == true)
            {
                TxtUsername.Text = signinWin.TxtUsername.Text; 
                TxtPassword.Password = ""; 
            }
        }

    }
}