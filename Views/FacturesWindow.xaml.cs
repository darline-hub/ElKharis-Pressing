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
using ElKharis.Models;
using MySql.Data.MySqlClient;
using ElKharis.Database;

namespace ElKharis.Views
{
    /// <summary>
    /// Logique d'interaction pour FacturesWindow.xaml
    /// </summary>
    public partial class FacturesWindow : Window
    {
        private readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";
        public FacturesWindow()
        {
            InitializeComponent();
            ChargerDonnees();
            TxtUserNom.Text = Session.NomUtilisateur;
            TxtUserRole.Text = Session.Role;
        }

        private void ChargerDonnees(string filtre = "")
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Requête SQL de base regroupant les informations financières calculées
                    string baseQuery = @"SELECT c.id_commande, c.numero_commande, c.date_commande, c.montant_total, 
                                                c.reduction, c.avance, c.statut_commande,
                                                (c.montant_total - c.reduction) AS net_a_payer,
                                                (c.montant_total - c.reduction - c.avance) AS reste_a_payer,
                                                CONCAT(cl.nom, ' ', IFNULL(cl.prenom, '')) AS nom_client
                                         FROM commandes c
                                         INNER JOIN clients cl ON c.id_client = cl.id_client";

                    // Injection du filtre de recherche si existant
                    if (!string.IsNullOrEmpty(filtre))
                    {
                        baseQuery += " WHERE (c.numero_commande LIKE @filtre OR cl.nom LIKE @filtre OR cl.prenom LIKE @filtre)";
                    }

                    baseQuery += " ORDER BY c.date_commande DESC";

                    using (MySqlCommand cmd = new MySqlCommand(baseQuery, conn))
                    {
                        if (!string.IsNullOrEmpty(filtre))
                        {
                            cmd.Parameters.AddWithValue("@filtre", "%" + filtre + "%");
                        }

                        MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                        DataTable dtGlobal = new DataTable();
                        da.Fill(dtGlobal);

                        // Répartition dans les structures DataGrid grâce aux DataView filtrés
                        // 1. Onglet Reçus : Il reste un montant à régler
                        DataView viewRecus = new DataView(dtGlobal);
                        viewRecus.RowFilter = "reste_a_payer > 0";
                        DgRecus.ItemsSource = viewRecus;

                        // 2. Onglet Factures : Le solde est entièrement à zéro
                        DataView viewFactures = new DataView(dtGlobal);
                        viewFactures.RowFilter = "reste_a_payer <= 0";
                        DgFactures.ItemsSource = viewFactures;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des listes de facturation : {ex.Message}", "Erreur SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Action de recherche instantanée
        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            ChargerDonnees(TxtRecherche.Text.Trim());
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            TxtRecherche.Text = string.Empty;
            ChargerDonnees();
        }

        // ACTION 1 : Visualiser le document PDF associé
        private void BtnVoirPDF_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            if (btn.DataContext is DataRowView ligneSelectionnee)
            {
                int idCommande = Convert.ToInt32(ligneSelectionnee["id_commande"]);
                decimal reste = Convert.ToDecimal(ligneSelectionnee["reste_a_payer"]);

                string typeDocument = reste > 0 ? "REÇU" : "FACTURE";

                // Régénère ou ouvre directement le document PDF ciblé via notre service centralisé
                DocumentService.GenererDocumentPDF(idCommande, typeDocument);
            }
        }

        // ACTION 2 : Encaisser le solde restant (Livraison ou règlement intermédiaire)
        private void BtnEncaisserSolde_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            if (btn.DataContext is DataRowView ligneSelectionnee)
            {
                int idCommande = Convert.ToInt32(ligneSelectionnee["id_commande"]);
                string numeroCmd = ligneSelectionnee["numero_commande"]?.ToString()?? "";
                decimal resteAPayer = Convert.ToDecimal(ligneSelectionnee["reste_a_payer"]);
                decimal totalActuel = Convert.ToDecimal(ligneSelectionnee["montant_total"]);
                decimal avanceActuelle = Convert.ToDecimal(ligneSelectionnee["avance"]);

                MessageBoxResult result = MessageBox.Show(
                    $"Confirmez-vous le paiement du solde restant de {resteAPayer:N0} FCFA pour la commande {numeroCmd} ?",
                    "Encaisser le Reste à Payer", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        MySqlTransaction tx = conn.BeginTransaction();

                        try
                        {
                            // 1. Mettre à jour la table commandes : l'avance cumulée devient égale au net à payer
                            string queryUpdateCommande = @"UPDATE commandes 
                                                           SET avance = avance + @solde, 
                                                               statut_commande = 'Payée' 
                                                           WHERE id_commande = @id";
                            using (MySqlCommand cmdUp = new MySqlCommand(queryUpdateCommande, conn, tx))
                            {
                                cmdUp.Parameters.AddWithValue("@solde", resteAPayer);
                                cmdUp.Parameters.AddWithValue("@id", idCommande);
                                cmdUp.ExecuteNonQuery();
                            }

                            // 2. Insérer une nouvelle ligne d'enregistrement dans la table `paiements`
                            string queryInsertPaiement = @"INSERT INTO paiements 
                                                           (id_commande, montant_verse, mode_paiement, statut_reglement) 
                                                           VALUES (@id, @montant, 'Espèces', 'Soldé')";
                            using (MySqlCommand cmdPay = new MySqlCommand(queryInsertPaiement, conn, tx))
                            {
                                cmdPay.Parameters.AddWithValue("@id", idCommande);
                                cmdPay.Parameters.AddWithValue("@montant", resteAPayer);
                                cmdPay.ExecuteNonQuery();
                            }

                            tx.Commit();

                            // 3. Transformation automatique : Génération instantanée de sa FACTURE finale PDF (CA-GFacture-013)
                            DocumentService.GenererDocumentPDF(idCommande, "FACTURE");

                            MessageBox.Show($"Le règlement a été enregistré. La commande {numeroCmd} est maintenant SOLDÉE. La facture définitive a été émise.", "Règlement Validé", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Rafraîchir les listes
                            ChargerDonnees(TxtRecherche.Text.Trim());
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            MessageBox.Show($"Erreur technique lors de la validation du solde : {ex.Message}", "Échec", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClientsWindow cltWin = new ClientsWindow();
                cltWin.Owner = this; // Démarre la relation Parent -> Enfant
                cltWin.Show();
                this.Hide(); // Cache simplement le dashboard sans le détruire
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des clients : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnArticles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ArticlesWindow articles = new ArticlesWindow();
                articles.Owner = this;
                articles.Show();
                this.Hide();
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
                services.Owner = this;
                services.Show();
                this.Hide();
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
                fenetreCommandes.Owner = this; // Très important !
                fenetreCommandes.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des commandes : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Êtes-vous sûr de vouloir vous déconnecter ?", "Déconnexion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close(); // Ici le Close est normal car on quitte l'application vers le login
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
        private void BtnQuitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
