using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using ElKharis.Models;
using ElKharis.Services;

namespace ElKharis.Views
{
    public partial class CommandesWindow : Window
    {
        private readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";
        private DataTable dtCommandes = new DataTable();

        public CommandesWindow()
        {
            InitializeComponent();
            TxtUserNom.Text = Session.NomUtilisateur;
            TxtUserRole.Text = Session.Role;
            ChargerCommandes();
            AppliquerRestrictionsDroits();
        }

        private void AppliquerRestrictionsDroits()
        {
            if (Session.Role == "Réceptionniste")
            {
                BtnServices.Visibility = Visibility.Collapsed;
                BtnArticles.Visibility = Visibility.Collapsed;
                // BtnUtilisateurs.Visibility = Visibility.Collapsed;
            }
            // Si c'est l'Administrateur, ils restent visibles par défaut (Visibility.Visible)
        }


        private void CalculerStatistiques()
        {
            if (dtCommandes == null) return;

            int total = dtCommandes.Rows.Count;
            int enAttente = 0;
            int enCours = 0;
            int pretes = 0;

            foreach (DataRow row in dtCommandes.Rows)
            {
                // On transforme en minuscules et on nettoie les espaces pour éviter les surprises
                string statut = (Convert.ToString(row["statut_commande"]) ?? "").Trim().ToLower();

                if (statut == "en attente" || statut == "creer" || statut == "créer" || statut == "créée")
                    enAttente++;
                else if (statut == "en cours de traitement" || statut == "en traitement")
                    enCours++;
                else if (statut == "prête" || statut == "prete")
                    pretes++;
            }

            TxtStatTotal.Text = total.ToString();
            TxtStatEnAttente.Text = enAttente.ToString();
            TxtStatEnCours.Text = enCours.ToString();
            TxtStatPretes.Text = pretes.ToString();

            TxtPaginationInfo.Text = $"Affichage de {total} commande(s) enregistrée(s)";
        }

        private void BtnSuivantStatut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int idCommande = Convert.ToInt32(btn.Tag);

                if (btn.DataContext is DataRowView ligneSelectionnee)
                {
                    // On récupère le statut proprement en minuscules
                    string statutActuel = (Convert.ToString(ligneSelectionnee["statut_commande"]) ?? "en attente").Trim().ToLower();
                    string statutSuivant = "";

                    // Machine à états tolérante aux variantes d'écriture
                    switch (statutActuel)
                    {
                        case "creer":
                        case "créer":
                        case "créée":
                        case "en attente":
                            statutSuivant = "En traitement";
                            break;
                        case "en cours de traitement":
                        case "en traitement":
                            statutSuivant = "Prête";
                            break;
                        case "prête":
                        case "prete":
                            statutSuivant = "Livrée";
                            break;
                        default:
                            statutSuivant = "En traitement";
                            break;
                    }

                    MessageBoxResult result = MessageBox.Show(
                        $"Passer la commande au statut '{statutSuivant}' ?",
                        "Changement rapide de statut",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            using (MySqlConnection conn = new MySqlConnection(connectionString))
                            {
                                conn.Open();
                                string query = "UPDATE commandes SET statut_commande = @statut WHERE id_commande = @id";

                                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                                {
                                    cmd.Parameters.AddWithValue("@statut", statutSuivant);
                                    cmd.Parameters.AddWithValue("@id", idCommande);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            // Recharger le tableau et recalculer les compteurs du haut
                            ChargerCommandes();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erreur lors du changement de statut : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }



        private void ChargerCommandes()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // CORRECTION : Ajout de la jointure INNER JOIN et de la fusion Nom + Prénom avec CONCAT
                    string query = @"SELECT c.id_commande, c.numero_commande, c.id_client, c.date_commande, 
                                    c.date_livraison_prevue, c.montant_total, c.reduction, c.avance, 
                                    c.statut_commande, c.reste_a_payer,
                                    CONCAT(cl.nom, ' ', IFNULL(cl.prenom, '')) AS nom_complet
                             FROM commandes c
                             INNER JOIN clients cl ON c.id_client = cl.id_client
                             ORDER BY c.date_commande DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            dtCommandes.Clear();
                            da.Fill(dtCommandes);

                            // Liaison de la DataTable filtrable à la DataGrid
                            DgCommandes.ItemsSource = dtCommandes.DefaultView;
                        }
                    }
                }

                CalculerStatistiques();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des commandes : {ex.Message}", "Erreur de base de données", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtCommandes == null || dtCommandes.DefaultView == null || TxtRecherche == null)
                return;

            string filtre = TxtRecherche.Text.Replace("'", "''").Trim();

            if (string.IsNullOrEmpty(filtre))
            {
                dtCommandes.DefaultView.RowFilter = string.Empty;
            }
            else
            {
                // CORRECTION : On filtre maintenant sur 'nom_complet' (le texte) et non plus sur l'ID numérique !
                dtCommandes.DefaultView.RowFilter = $"numero_commande LIKE '%{filtre}%' OR nom_complet LIKE '%{filtre}%'";
            }
            TxtPaginationInfo.Text = $"Affichage de {DgCommandes.Items.Count} commande(s) correspondante(s)";
        }

        // LA MÉTHODE PARFAITE UNIQUE (L'autre doublon a été supprimé)
        private void BtnModifierCommande_Click(object sender, RoutedEventArgs e)
        {
            // 1. Récupération du bouton cliqué
            Button btn = (Button)sender;

            // 2. Récupération de la ligne du tableau associée (DataRowView)
            if (btn.DataContext is DataRowView ligneSelectionnee)
            {
                // Extraction de la ligne brute (DataRow)
                DataRow commandeRow = ligneSelectionnee.Row;

                // 3. SÉCURITÉ : Vérification du statut avant d'ouvrir le formulaire
                string statut = commandeRow["statut_commande"]?.ToString() ?? "";

                if (statut.Equals("Livrée", StringComparison.OrdinalIgnoreCase) ||
                    statut.Equals("Livre", StringComparison.OrdinalIgnoreCase))
                {
                    // On affiche le message d'avertissement
                    MessageBox.Show($"Cette commande est déjà '{statut}'. Elle ne peut plus être modifiée (Règle CA-GCommande-009).",
                                    "Modification impossible",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);

                    return; 
                }

                NouvelleCommandeWindow modifWindow = new NouvelleCommandeWindow(commandeRow);
                modifWindow.Owner = this; 
                if (modifWindow.ShowDialog() == true)
                {
                    ChargerCommandes();
                }
            }
            else
            {
                MessageBox.Show("Impossible de récupérer les données de la commande sélectionnée.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Centralisation de l'ouverture pour la modification
        private void OuvrirFormulaireModification(int idCommande)
        {
            DataRow[] rows = dtCommandes.Select($"id_commande = {idCommande}");
            if (rows.Length > 0)
            {
                NouvelleCommandeWindow frm = new NouvelleCommandeWindow(rows[0]);
                frm.Owner = this;
                if (frm.ShowDialog() == true)
                {
                    ChargerCommandes();
                }
            }
        }

        private void BtnSupprimerCommande_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int idCommande = Convert.ToInt32(btn.Tag);

                MessageBoxResult result = MessageBox.Show(
                    "Voulez-vous vraiment supprimer définitivement cette commande ?",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM commandes WHERE id_commande = @id";

                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", idCommande);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Recharger les données mises à jour
                        ChargerCommandes();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la suppression : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
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

        private void BtnRetourDashboard_Click(object sender, RoutedEventArgs e)
        {
            // Si cette fenêtre a été ouverte depuis le Dashboard (Owner existant)
            if (this.Owner is DashboardWindow dashboard)
            {
                dashboard.RafraichirDashboard();
                dashboard.Show();                
            }
            else
            {
                // Sécurité au cas où la fenêtre aurait été ouverte directement sans Owner
                DashboardWindow fallbackDashboard = new DashboardWindow();
                fallbackDashboard.Show();
            }

            this.Close(); // Ferme et détruit proprement la fenêtre secondaire devenue inutile
        }

        private void BtnQuitter_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Êtes-vous sûr de vouloir vous déconnecter ?", "Déconnexion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClientsWindow cltWin = new ClientsWindow();
                cltWin.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des clients : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des articles : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des services : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNouvelleCommande_Click(object sender, RoutedEventArgs e)
        {
            NouvelleCommandeWindow saisieWindow = new NouvelleCommandeWindow();
            saisieWindow.Owner = this;
            if (saisieWindow.ShowDialog() == true)
            {
                ChargerCommandes();
            }
        }

        private void DgCommandes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgCommandes.SelectedItem is DataRowView ligneSelectionnee)
            {
                DataRow commandeRow = ligneSelectionnee.Row;
                NouvelleCommandeWindow modifWindow = new NouvelleCommandeWindow(commandeRow);
                modifWindow.Owner = this;
                if (modifWindow.ShowDialog() == true)
                {
                    ChargerCommandes();
                }
            }
        }

       

    }
}