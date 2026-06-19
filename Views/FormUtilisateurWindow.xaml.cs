using System;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace ElKharis.Views
{
    public partial class FormUtilisateurWindow : Window
    {
        private readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";
        private bool estModification = false;

        // Constructeur pour l'AJOUT
        public FormUtilisateurWindow()
        {
            InitializeComponent();
            estModification = false;
            TxtTitreFormulaire.Text = "Fiche Utilisateur";
        }

        // Constructeur pour la MODIFICATION (on reçoit les données existantes)
        public FormUtilisateurWindow(string id, string nom, string email, string role)
        {
            InitializeComponent();
            estModification = true;
            TxtTitreFormulaire.Text = "Modifier l'Utilisateur";

            // Afficher le champ ID uniquement en modification
            PanelId.Visibility = Visibility.Visible;
            TxtId.Text = id;

            // Pré-remplir les champs
            TxtNomUtilisateur.Text = nom;
            TxtEmail.Text = email;

            // Sélectionner le bon rôle dans la ComboBox
            foreach (ComboBoxItem item in CboRole.Items)
            {
                if (item.Content.ToString() == role)
                {
                    CboRole.SelectedItem = item;
                    break;
                }
            }
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validation des champs
            if (string.IsNullOrWhiteSpace(TxtNomUtilisateur.Text) || string.IsNullOrWhiteSpace(TxtEmail.Text) || CboRole.SelectedItem == null)
            {
                MessageBox.Show("Veuillez remplir tous les champs obligatoires.", "Champs requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nom = TxtNomUtilisateur.Text.Trim();
            string email = TxtEmail.Text.Trim();
            string role = ((ComboBoxItem)CboRole.SelectedItem)?.Content?.ToString() ?? "";

            // 2. Traitement en Base de Données
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "";

                    if (estModification)
                    {
                        // En modification, on ne touche pas au mot de passe existant
                        query = "UPDATE Utilisateurs SET nom_utilisateur = @nom, email = @email, role = @role WHERE id = @id";
                    }
                    else
                    {
                        // En ajout : on inclut le champ mot_de_passe (adapte le nom de la colonne si nécessaire, ex: password ou mot_de_passe)
                        query = "INSERT INTO Utilisateurs (nom_utilisateur, email, role, mot_de_passe) VALUES (@nom, @email, @role, @mdp)";
                    }

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@nom", nom);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@role", role);

                        if (estModification)
                        {
                            cmd.Parameters.AddWithValue("@id", TxtId.Text);
                        }
                        else
                        {
                            // Définition du mot de passe par défaut
                            string mdpParDefaut = "Pass1234";

                            // OPTION A : Si tu as installé BCrypt via NuGet (Recommandé pour la sécurité)
                            string mdpHache = BCrypt.Net.BCrypt.HashPassword(mdpParDefaut);
                            cmd.Parameters.AddWithValue("@mdp", mdpHache);

                            // OPTION B : Si tu es encore en texte brut pour tes tests (Décommente la ligne ci-dessous et commente l'option A)
                            // cmd.Parameters.AddWithValue("@mdp", mdpParDefaut);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                // Message personnalisé pour informer l'admin du mot de passe généré en cas d'ajout
                if (!estModification)
                {
                    MessageBox.Show("Utilisateur créé avec succès !\n\nSon mot de passe temporaire est : Pass1234",
                                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Profil utilisateur mis à jour avec succès !",
                                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                this.DialogResult = true; // Indique à la fenêtre parente que tout s'est bien passé
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}", "Erreur SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
