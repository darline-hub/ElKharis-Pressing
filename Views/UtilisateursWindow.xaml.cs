using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace ElKharis.Views
{
    public partial class UtilisateursWindow : Window
    {
        private readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";
        private DataTable dtUtilisateurs = new DataTable();

        public UtilisateursWindow()
        {
            InitializeComponent();
            ChargerTousLesUtilisateurs();

            // Simuler l'affichage de l'utilisateur connecté (Exemple à lier avec ton application)
            TxtUserNom.Text = "Administrateur Principal";
            TxtUserRole.Text = "Administrateur";
        }

        private void ChargerTousLesUtilisateurs()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, nom_utilisateur, email, role FROM Utilisateurs ORDER BY role ASC, nom_utilisateur ASC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            dtUtilisateurs.Clear();
                            da.Fill(dtUtilisateurs);
                        }
                    }
                    DgUtilisateurs.ItemsSource = dtUtilisateurs.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des utilisateurs : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtUtilisateurs.DefaultView != null)
            {
                string filtre = TxtRecherche.Text.Trim().Replace("'", "''");

                if (string.IsNullOrEmpty(filtre))
                {
                    dtUtilisateurs.DefaultView.RowFilter = string.Empty;
                }
                else
                {
                    dtUtilisateurs.DefaultView.RowFilter = $"nom_utilisateur LIKE '%{filtre}%' OR email LIKE '%{filtre}%' OR Convert(id, 'System.String') LIKE '%{filtre}%'";
                }
            }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (DgUtilisateurs.SelectedItem is DataRowView ligneSelectionnee)
            {
                int idUser = Convert.ToInt32(ligneSelectionnee["id"]);
                string nomUser = ligneSelectionnee["nom_utilisateur"].ToString() ?? "";

                MessageBoxResult resultat = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer définitivement l'utilisateur '{nomUser}' ?\nCette action est irréversible.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (resultat == MessageBoxResult.Yes)
                {
                    SupprimerUtilisateurDeLaBase(idUser);
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un utilisateur dans la liste.",
                                "Sélection requise", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SupprimerUtilisateurDeLaBase(int idUtilisateur)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM Utilisateurs WHERE id = @id";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idUtilisateur);
                        int lignesModifiees = cmd.ExecuteNonQuery();

                        if (lignesModifiees > 0)
                        {
                            MessageBox.Show("L'utilisateur a été supprimé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                            TxtRecherche.Text = string.Empty;
                            ChargerTousLesUtilisateurs();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Une erreur est survenue : {ex.Message}", "Erreur SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // BOUTON AJOUTER
        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            // On ouvre le formulaire d'ajout en mode "Dialogue"
            FormUtilisateurWindow frm = new FormUtilisateurWindow();
            frm.Owner = this; // Centre la sous-fenêtre par rapport à la fenêtre principale

            if (frm.ShowDialog() == true)
            {
                ChargerTousLesUtilisateurs(); // Rafraîchit le DataGrid automatiquement si validé
            }
        }

        // BOUTON MODIFIER
        // BOUTON MODIFIER
        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            // On vérifie que la sélection n'est pas nulle
            if (DgUtilisateurs.SelectedItem == null)
            {
                MessageBox.Show("Veuillez d'abord sélectionner un utilisateur dans le tableau à modifier.",
                                "Sélection requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string id = "";
                string nom = "";
                string email = "";
                string role = "";

                if (DgUtilisateurs.SelectedItem is System.Data.DataRowView drv)
                {
                    // Le point d'interrogation drv.Row?. protège contre le Warning CS8602
                    System.Data.DataColumnCollection? colonnes = drv.Row?.Table?.Columns;

                    if (colonnes != null)
                    {
                        // Vérification stricte des colonnes pour éviter tout plantage
                        id = colonnes.Contains("id") && drv["id"] != DBNull.Value ? drv["id"].ToString() ?? "" : "";
                        nom = colonnes.Contains("nom_utilisateur") && drv["nom_utilisateur"] != DBNull.Value ? drv["nom_utilisateur"].ToString() ?? "" : "";
                        email = colonnes.Contains("email") && drv["email"] != DBNull.Value ? drv["email"].ToString() ?? "" : "";
                        role = colonnes.Contains("role") && drv["role"] != DBNull.Value ? drv["role"].ToString() ?? "" : "";
                    }
                }

                // Ouverture sécurisée du formulaire
                FormUtilisateurWindow frm = new FormUtilisateurWindow(id, nom, email, role);
                frm.Owner = this;

                if (frm.ShowDialog() == true)
                {
                    ChargerTousLesUtilisateurs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la lecture des données : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnClients_Click(object sender, RoutedEventArgs e) { 
            try{
                ClientsWindow cw = new ClientsWindow(); 
                cw.Show(); 
                this.Close(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des clients : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        private void BtnFactures_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FacturesWindow factures = new FacturesWindow();
                factures.Owner = this; // Très important !
                factures.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre de facturation : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DashboardWindow dashboard = new DashboardWindow();
                dashboard.Show();
                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre du tableau de bord : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void BtnArticles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ArticlesWindow articles = new ArticlesWindow();
                articles.Show();
                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des articles : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        private void BtnServices_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ServicesWindow services = new ServicesWindow();
                services.Show();
                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des services : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCommandes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CommandesWindow fenetreCommandes = new CommandesWindow();
                fenetreCommandes.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des commandes : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnUtilisateurs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UtilisateursWindow utilisateur = new UtilisateursWindow();
                utilisateur.Show();
                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture des utilisateurs : {ex.Message}",
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