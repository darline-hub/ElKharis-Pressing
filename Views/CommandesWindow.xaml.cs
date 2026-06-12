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
using MySql.Data.MySqlClient;

namespace ElKharis.Views
{
    /// <summary>
    /// Logique d'interaction pour CommandesWindow.xaml
    /// </summary>
        public partial class CommandesWindow : Window
        {
            private DataTable tableCommandes = new DataTable();

        public CommandesWindow()
        {

            tableCommandes = new DataTable();
            InitializeComponent();
            ChargerDonnees();
            ActualiserTout();
            TxtUserNom.Text = Session.NomUtilisateur;
            TxtUserRole.Text = Session.Role;
        }

        public void ChargerDonnees()
            {
                using (MySqlConnection conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    string query = @"SELECT c.id_commande, c.numero_commande, c.date_commande, 
                                        c.montant_total, c.statut_commande, cl.nom
                                 FROM commandes c
                                 LEFT JOIN clients cl ON c.id_client = cl.id_client
                                 ORDER BY c.id_commande DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            tableCommandes = new DataTable();
                            adapter.Fill(tableCommandes);
                            DgCommandes.ItemsSource = tableCommandes.DefaultView;
                        }
                    }
                }
                CalculerStatistiques();
            }

            private void CalculerStatistiques()
            {
                if (tableCommandes == null) return;

                int total = tableCommandes.Rows.Count;
                int attente = tableCommandes.Select("statut_commande = 'En attente'").Length;
                int cours = tableCommandes.Select("statut_commande = 'En traitement'").Length;
                int pretes = tableCommandes.Select("statut_commande = 'Prête'").Length;

                TxtStatTotal.Text = total.ToString();
                TxtStatEnAttente.Text = attente.ToString();
                TxtStatEnCours.Text = cours.ToString();
                TxtStatPretes.Text = pretes.ToString();
            }

            private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
            {
                if (tableCommandes == null) return;

                string filtre = TxtRecherche.Text.Trim().Replace("'", "''");
                tableCommandes.DefaultView.RowFilter = $"numero_commande LIKE '%{filtre}%' OR nom_client LIKE '%{filtre}%'";
                if (string.IsNullOrEmpty(filtre))
                {
                    tableCommandes.DefaultView.RowFilter = "";
                }
                else
                {
                    tableCommandes.DefaultView.RowFilter = $"numero_commande LIKE '%{filtre}%' OR nom_client LIKE '%{filtre}%'";
                }

                TxtPaginationInfo.Text = $"Affichage de {DgCommandes.Items.Count} commande(s) filtrée(s)";
            }


        private void BtnNouvelleCommande_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NouvelleCommandeWindow win = new NouvelleCommandeWindow();

                if (win.ShowDialog() == true)
                {
                    ChargerDonnees();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur critique lors de l'ouverture du formulaire : {ex.Message}\n\nDétails : {ex.InnerException?.Message}",
                                "Erreur d'ouverture",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void ActualiserTout()
        {
            ChargerStatistiquesCommandes();
            ChargerListeCommandes();
        }

        private void ChargerStatistiquesCommandes()
        {
            try
            {
                using (MySqlConnection conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    string query = @"SELECT 
                                        COUNT(*) AS total,
                                        SUM(CASE WHEN statut_commande = 'En attente' THEN 1 ELSE 0 END) AS en_attente,
                                        SUM(CASE WHEN statut_commande IN ('En cours de traitement', 'En traitement') THEN 1 ELSE 0 END) AS en_cours,
                                        SUM(CASE WHEN statut_commande = 'Prête' THEN 1 ELSE 0 END) AS pretes
                                     FROM commandes";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            TxtStatTotal.Text = dr["total"].ToString();
                            TxtStatEnAttente.Text = dr["en_attente"] != DBNull.Value ? dr["en_attente"].ToString() : "0";
                            TxtStatEnCours.Text = dr["en_cours"] != DBNull.Value ? dr["en_cours"].ToString() : "0";
                            TxtStatPretes.Text = dr["pretes"] != DBNull.Value ? dr["pretes"].ToString() : "0";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur stats commandes : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 2. Chargement de la DataGrid principale
        private void ChargerListeCommandes()
        {
            try
            {
                using (MySqlConnection conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    // Jointure avec la table clients pour récupérer le nom complet
                    string query = @"SELECT 
                                        c.id_commande,
                                        c.numero_commande, 
                                        cl.nom AS nom_client, 
                                        c.statut_commande, 
                                        c.date_commande, 
                                        c.montant_total
                                     FROM commandes c
                                     INNER JOIN clients cl ON c.id_client = cl.id_client
                                     ORDER BY c.date_commande DESC";

                    MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                    tableCommandes.Clear();
                    da.Fill(tableCommandes);

                    DgCommandes.ItemsSource = tableCommandes.DefaultView;

                    TxtPaginationInfo.Text = $"Affichage de {tableCommandes.Rows.Count} commande(s)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur liste commandes : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnModifierCommande_Click(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn?.Tag != null)
            {
                int idCommande = Convert.ToInt32(btn.Tag);
                MessageBox.Show($"Ouvrir le formulaire de modification pour la commande ID : {idCommande}",
                                "Modifier", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSupprimerCommande_Click(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn?.Tag != null)
            {
                int idCommande = Convert.ToInt32(btn.Tag);
                MessageBoxResult result = MessageBox.Show(
                    "Êtes-vous sûr de vouloir supprimer cette commande ?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    SupprimerCommandeDuDb(idCommande);
                }
            }
        }
        private void SupprimerCommandeDuDb(int idCommande)
        {
            try
            {
                using (MySqlConnection conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    // 1. On supprime d'abord les détails de la commande (clé étrangère)
                    string queryDetails = "DELETE FROM detail_commandes WHERE id_commande = @id";
                    using (MySqlCommand cmdDetails = new MySqlCommand(queryDetails, conn))
                    {
                        cmdDetails.Parameters.AddWithValue("@id", idCommande);
                        cmdDetails.ExecuteNonQuery();
                    }

                    // 2. On supprime ensuite la commande elle-même
                    string queryCommande = "DELETE FROM commandes WHERE id_commande = @id";
                    using (MySqlCommand cmdCmd = new MySqlCommand(queryCommande, conn))
                    {
                        cmdCmd.Parameters.AddWithValue("@id", idCommande);
                        cmdCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Commande supprimée avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 3. On rafraîchit la DataGrid et les compteurs du haut instantanément
                    ActualiserTout();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la suppression en base de données : " + ex.Message,
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRetourDashboard_Click(object sender, RoutedEventArgs e)
            {
                DashboardWindow dashboard = new DashboardWindow();
                dashboard.Show();
                this.Close();
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