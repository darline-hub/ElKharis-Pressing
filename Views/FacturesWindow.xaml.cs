using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using ElKharis.Models;
using ElKharis.Services;
using Microsoft.Win32;
using MySql.Data.MySqlClient;

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
                                                CONCAT(cl.nom, ' ', IFNULL(cl.prenom, '')) AS nom
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
            // 1. On récupère le bouton qui a été cliqué
            if (sender is Button btn)
            {
                // 2. On extrait la ligne de données associée au bouton cliqué
                if (btn.DataContext is DataRowView ligneSelectionnee)
                {
                    // Récupération des informations de la base de données mappées dans votre DataGrid
                    int idCommande = Convert.ToInt32(ligneSelectionnee["id_commande"]);
                    string nomClient = ligneSelectionnee["nom"].ToString()?? "";

                    // On détermine dynamiquement le type de document en fonction du statut ou de l'onglet
                    string statut = ligneSelectionnee["statut_commande"].ToString() ?? "";
                    string typeDoc = (statut == "Terminée" || statut == "Payée") ? "FACTURE" : "REÇU DE DÉPÔT";

                    // 3. OUVRIR LA BOÎTE DE DIALOGUE "ENREGISTRER SOUS" (Choix du dossier et du nom)
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "Document PDF (*.pdf)|*.pdf";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    string nomFichierPropre = $"{typeDoc.Replace(" ", "_")}_{idCommande}_{nomClient.Replace(" ", "_")}";
                    saveFileDialog.FileName = nomFichierPropre;

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string cheminChoisi = saveFileDialog.FileName;

                        try
                        {
                            // 4. GENERATION DU BEAU PDF ÉPURÉ
                            DocumentService.GenererDocumentPDF(idCommande, typeDoc, cheminChoisi);

                            // 5. APPERÇU / VISUALISATION IMMÉDIATE (Comme le Ctrl+P du navigateur)
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(cheminChoisi)
                            {
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Impossible d'ouvrir le PDF pour prévisualisation : {ex.Message}", "Erreur d'ouverture", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }


        // ACTION 2 : Encaisser le solde restant (Livraison ou règlement intermédiaire)
        private void BtnEncaisserSolde_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            if (btn.DataContext is DataRowView ligneSelectionnee)
            {
                int idCommande = Convert.ToInt32(ligneSelectionnee["id_commande"]);
                string numeroCmd = ligneSelectionnee["numero_commande"]?.ToString() ?? "";
                decimal resteAPayer = Convert.ToDecimal(ligneSelectionnee["reste_a_payer"]);
                decimal totalActuel = Convert.ToDecimal(ligneSelectionnee["montant_total"]);
                decimal avanceActuelle = Convert.ToDecimal(ligneSelectionnee["avance"]);
                string nomClient = ligneSelectionnee["nom"]?.ToString() ?? "Client";

                MessageBoxResult result = MessageBox.Show(
                    $"Confirmez-vous le paiement du solde restant de {resteAPayer:N0} FCFA pour la commande {numeroCmd} ?",
                    "Encaisser le Reste à Payer", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 1. OUVRIR LA BOÎTE DE DIALOGUE AVANT DE MODIFIER LA BASE DE DONNÉES
                    // Cela évite de valider un paiement si l'utilisateur annule l'enregistrement du PDF
                    Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                    saveFileDialog.Filter = "Document PDF (*.pdf)|*.pdf";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    saveFileDialog.FileName = $"FACTURE_{idCommande}_{nomClient.Replace(" ", "_")}";

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string cheminChoisi = saveFileDialog.FileName;

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            MySqlTransaction tx = conn.BeginTransaction();

                            try
                            {
                                // 2. Mettre à jour la table commandes
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

                                // 3. Insérer une nouvelle ligne d'enregistrement dans la table `paiements`
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

                                // 4. GENERATION AVEC LE TROISIÈME PARAMÈTRE REQUIS
                                DocumentService.GenererDocumentPDF(idCommande, "FACTURE", cheminChoisi);

                                MessageBox.Show($"Le règlement a été enregistré. La commande {numeroCmd} est maintenant SOLDÉE. La facture définitive a été émise.", "Règlement Validé", MessageBoxButton.OK, MessageBoxImage.Information);

                                // 5. APPERÇU AUTOMATIQUE DE LA FACTURE CHISIE
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(cheminChoisi)
                                {
                                    UseShellExecute = true
                                });

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
        }
        private void BtnImprimer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // sans avoir besoin de nommer GridFactures, DgRecus ou DgFactures.
                if (btn.DataContext is DataRowView ligneSelectionnee)
                {
                    int idCommande = Convert.ToInt32(ligneSelectionnee["id_commande"]);

                    // Récupération sécurisée du type de document et des colonnes de votre base de données
                    string nomClient = (ligneSelectionnee.Row.Table.Columns.Contains("nom")
                                        ? ligneSelectionnee["nom"]?.ToString()
                                        : ligneSelectionnee["nom"]?.ToString()) ?? "Client";

                    string statut = ligneSelectionnee["statut_commande"]?.ToString() ?? "";
                    string typeDoc = (statut == "Terminée" || statut == "Payée") ? "FACTURE" : "REÇU DE DÉPÔT";

                    // 2. OUVRIR LA BOÎTE DE DIALOGUE "ENREGISTRER SOUS"
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "Document PDF (*.pdf)|*.pdf";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    saveFileDialog.FileName = $"{typeDoc.Replace(" ", "_")}_{idCommande}_{nomClient.Replace(" ", "_")}";

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string cheminChoisi = saveFileDialog.FileName;

                        try
                        {
                            // 3. GENERATION ET PREVISUALISATION
                            DocumentService.GenererDocumentPDF(idCommande, typeDoc, cheminChoisi);

                            // 4. OUVERTURE AUTOMATIQUE POUR VISUALISATION
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(cheminChoisi) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Impossible de prévisualiser le PDF : {ex.Message}", "Erreur d'ouverture", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une ligne pour imprimer le document.", "Sélection manquante", MessageBoxButton.OK, MessageBoxImage.Warning);
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
