using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using ElKharis.Models;

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
        }

        private void ChargerCommandes()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Sélection des colonnes qui correspondent exactement à tes Bindings XAML
                    string query = @"SELECT id_commande, numero_commande, nom_client, date_commande, montant_total, statut_commande 
                                     FROM commandes 
                                     ORDER BY date_commande DESC";

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
        private void CalculerStatistiques()
        {
            if (dtCommandes == null) return;

            int total = dtCommandes.Rows.Count;
            int enAttente = 0;
            int enCours = 0;
            int pretes = 0;

            foreach (DataRow row in dtCommandes.Rows)
            {
                string statut = row["statut_commande"]?.ToString() ?? "";

                if (statut == "En attente")
                    enAttente++;
                else if (statut == "En cours de traitement" || statut == "En traitement")
                    enCours++;
                else if (statut == "Prête")
                    pretes++;
            }

            // Injection des valeurs calculées dans les TextBlocks du XAML
            TxtStatTotal.Text = total.ToString();
            TxtStatEnAttente.Text = enAttente.ToString();
            TxtStatEnCours.Text = enCours.ToString();
            TxtStatPretes.Text = pretes.ToString();

            // Mise à jour de la barre d'information en bas
            TxtPaginationInfo.Text = $"Affichage de {total} commande(s) enregistrée(s)";
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Sécurité pour éviter le crash au chargement du composant
            if (dtCommandes == null || dtCommandes.DefaultView == null || TxtRecherche == null)
                return;

            // Protection basique contre les injections de guillemets dans le filtre
            string filtre = TxtRecherche.Text.Replace("'", "''").Trim();

            if (string.IsNullOrEmpty(filtre))
            {
                dtCommandes.DefaultView.RowFilter = string.Empty;
            }
            else
            {
                // Filtre cumulatif : cherche soit dans le numéro de commande, soit dans le nom du client
                dtCommandes.DefaultView.RowFilter = $"numero_commande LIKE '%{filtre}%' OR nom_client LIKE '%{filtre}%'";
            }

            // Met à jour l'indicateur textuel du bas avec le nombre d'éléments actuellement visibles
            TxtPaginationInfo.Text = $"Affichage de {DgCommandes.Items.Count} commande(s) correspondante(s)";
        }

        // ================================================================= -->
        // 4. GESTION DES ACTIONS (AJOUT, MODIFICATION, SUPPRESSION)
        // ================================================================= -->

        // Clic sur "+ Nouvelle commande"
        private void BtnNouvelleCommande_Click(object sender, RoutedEventArgs e)
        {
            NouvelleCommandeWindow frm = new NouvelleCommandeWindow();
            frm.Owner = this;

            // Si le formulaire se ferme après une sauvegarde réussie (DialogResult = true)
            if (frm.ShowDialog() == true)
            {
                ChargerCommandes(); // Rafraîchissement automatique de la liste et des stats
            }
        }

        // Action de modification via le bouton 📝
        private void BtnModifierCommande_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int idCommande = Convert.ToInt32(btn.Tag);
                OuvrirFormulaireModification(idCommande);
            }
        }

        // Action de modification via un double-clic sur une ligne du tableau
        private void DgCommandes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgCommandes.SelectedItem is DataRowView rowView)
            {
                int idCommande = Convert.ToInt32(rowView["id_commande"]);
                OuvrirFormulaireModification(idCommande);
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
                    ChargerCommandes(); // Rafraîchit le tableau après modification
                }
            }
        }

        // Action de suppression via le bouton 🗑️
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

        // ================================================================= -->
        // 5. NAVIGATION SIDEBAR ET SYSTÈME
        // ================================================================= -->
        private void BtnRetourDashboard_Click(object sender, RoutedEventArgs e) => this.Close();

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
            ClientsWindow cltWin = new ClientsWindow();
            cltWin.Show();
            this.Close();
        }
    }
}