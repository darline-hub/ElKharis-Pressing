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
        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            // 1. Sécurité de base
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

                // 2. L'approche universelle : on extrait via un conteneur dynamique
                // Cela fonctionne que ce soit un DataRowView, un objet anonyme ou une classe Utilisateur.
                dynamic ligne = DgUtilisateurs.SelectedItem;

                // On utilise l'opérateur de coalescence nulle (?.) pour intercepter le null AVANT le ToString()
                id = ConvertirEnChaine(ligne, "id");
                nom = ConvertirEnChaine(ligne, "nom_utilisateur");
                email = ConvertirEnChaine(ligne, "email");
                role = ConvertirEnChaine(ligne, "role");

                // 3. Lancement du formulaire
                FormUtilisateurWindow frm = new FormUtilisateurWindow(id, nom, email, role);
                frm.Owner = this;

                if (frm.ShowDialog() == true)
                {
                    ChargerTousLesUtilisateurs(); // Rafraîchit le tableau
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible de lire les informations de l'utilisateur sélectionné.\n\nDétails de l'erreur : {ex.Message}",
                                "Erreur critique de lecture", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 4. METHODE MAGIQUE DE SECOURS (A coller juste en dessous de la méthode du bouton)
        // Cette fonction intercepte absolument tous les types de valeurs nulles ou DBNull
        private string? ConvertirEnChaine(dynamic objetLigne, string nomPropriete)
        {
            if (objetLigne == null) return "";

            try
            {
                // Si c'est un DataRowView (liaison DataTable classique)
                if (objetLigne is System.Data.DataRowView drv)
                {
                    if (drv.Row == null || !drv.Row.Table.Columns.Contains(nomPropriete)) return "";
                    var val = drv[nomPropriete];
                    return (val == null || val == DBNull.Value) ? "" : val.ToString();
                }

                // Si c'est un objet standard ou anonyme (liaison par classe)
                // On tente de lire dynamiquement la propriété
                var propriete = objetLigne.GetType().GetProperty(nomPropriete);
                if (propriete != null)
                {
                    var val = propriete.GetValue(objetLigne, null);
                    return (val == null || val == DBNull.Value) ? "" : val.ToString();
                }
            }
            catch
            {
                // Si la réflexion dynamique échoue, on tente un accès direct par indexeur
                try
                {
                    var val = objetLigne[nomPropriete];
                    return (val == null || val == DBNull.Value) ? "" : val.ToString();
                }
                catch { }
            }

            return "";
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