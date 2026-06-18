using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using ElKharis.Database;
using ElKharis.Services;

namespace ElKharis.Views
{
    public partial class NouvelleCommandeWindow : Window
    {
        private readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";
        private long? _idClientForce = null;

        public class LignePanier
        {
            public int IdArticle { get; set; }
            public int IdService { get; set; }
            public string Designation { get; set; } = string.Empty;
            public string Service { get; set; } = string.Empty;
            public decimal PrixUnitaire { get; set; }
            public int Quantite { get; set; }
            public decimal TotalLigne => PrixUnitaire * Quantite;
        }

        private ObservableCollection<LignePanier> panier = new ObservableCollection<LignePanier>();
        private bool estModification = false;
        private int idCommandeEnCours = 0;
        private string statutActuel = "Créée";

        public NouvelleCommandeWindow()
        {
            InitializeComponent();
            InitialiserFormulaire();
        }

        public NouvelleCommandeWindow(long idClient)
        {
            InitializeComponent();
            this._idClientForce = idClient;
            InitialiserFormulaire();
        }

        public NouvelleCommandeWindow(DataRow ligneCommande)
        {
            InitializeComponent();
            InitialiserFormulaire();

            estModification = true;
            idCommandeEnCours = Convert.ToInt32(ligneCommande["id_commande"]);
            statutActuel = ligneCommande["statut_commande"]?.ToString() ?? "Creer";

            TxtNumero.Text = ligneCommande["numero_commande"]?.ToString() ?? string.Empty;
            CbClients.SelectedValue = Convert.ToInt64(ligneCommande["id_client"]);
            DpDepot.SelectedDate = Convert.ToDateTime(ligneCommande["date_commande"]);
            DpLivraison.SelectedDate = Convert.ToDateTime(ligneCommande["date_livraison_prevue"]);
            TxtTotal.Text = ligneCommande["montant_total"]?.ToString() ?? "0";
            TxtReduction.Text = ligneCommande["reduction"]?.ToString() ?? "0";
            TxtAvance.Text = ligneCommande["avance"]?.ToString() ?? "0";

            if (statutActuel.Equals("Livrée", StringComparison.OrdinalIgnoreCase) || statutActuel.Equals("Livre", StringComparison.OrdinalIgnoreCase))
            {
                VerrouillerFormulaire();
            }

            ChargerPanierCommandeExistante(idCommandeEnCours);
        }

        private void VerrouillerFormulaire()
        {
            CbClients.IsEnabled = false;
            CbArticles.IsEnabled = false;
            CbServices.IsEnabled = false;
            TxtQuantite.IsEnabled = false;
            TxtReduction.IsEnabled = false;
            TxtAvance.IsEnabled = false;
            DpLivraison.IsEnabled = false;
            DgPanier.IsEnabled = false;
            MessageBox.Show("Cette commande est déjà LIVRÉE. Elle est en lecture seule (Règle CA-GCommande-009).", "Verrouillée", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InitialiserFormulaire()
        {
            DgPanier.ItemsSource = panier;

            // Liaison des événements de changement de date pour le calcul automatique en temps réel
            DpDepot.SelectedDateChanged += DatePicker_SelectedDateChanged;
            DpLivraison.SelectedDateChanged += DatePicker_SelectedDateChanged;

            DpDepot.SelectedDate = DateTime.Today;
            DpLivraison.SelectedDate = DateTime.Today.AddDays(3);

            ChargerClients();
            ChangeApresChargementClient();
            ChargerArticles();
            ChargerServices();

            if (!estModification)
            {
                GenererNumeroCommande();
            }
        }

        private void ChangeApresChargementClient()
        {
            if (_idClientForce.HasValue)
            {
                CbClients.SelectedValue = _idClientForce.Value;
            }
        }

        // ÉVÉNEMENT : Dès que la date change, on recalcule TOUT le panier automatiquement à la volée !
        private void DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DpDepot.SelectedDate == null || DpLivraison.SelectedDate == null || panier.Count == 0) return;

            DateTime dateDepot = DpDepot.SelectedDate.Value.Date;
            DateTime dateLivraison = DpLivraison.SelectedDate.Value.Date;
            int joursEcart = (dateLivraison - dateDepot).Days;

            if (joursEcart <= 0) return;

            // On met à jour chaque ligne selon le nouvel écart de dates
            for (int i = 0; i < panier.Count; i++)
            {
                decimal prixBase = ObtenirPrixDeBaseArticle(panier[i].IdArticle);
                decimal coeff = ObtenirCoefficientService(panier[i].IdService);
                decimal nouveauPrix = prixBase * coeff;

                if (joursEcart == 1)
                {
                    nouveauPrix *= 2.0m;
                }
                else if (joursEcart == 2)
                {
                    nouveauPrix *= 1.5m;
                }

                panier[i].PrixUnitaire = nouveauPrix;
            }

            // Rafraîchir l'affichage du tableau WPF et recalculer les totaux globaux
            DgPanier.Items.Refresh();
            MettreAJourTotaux();
        }

        #region ENREGISTREMENT SQL
        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (CbClients.SelectedValue == null || CbClients.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'enregistrer.", "Saisie incomplète", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (panier.Count == 0)
            {
                MessageBox.Show("Le panier est vide.", "Panier vide", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (DpLivraison.SelectedDate == null)
            {
                MessageBox.Show("Veuillez sélectionner une date de livraison.", "Saisie incomplète", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string numero = TxtNumero.Text.Trim();
            decimal total = Convert.ToDecimal(TxtTotal.Text);
            decimal reduction = decimal.TryParse(TxtReduction.Text, out decimal r) ? r : 0m;
            decimal avance = decimal.TryParse(TxtAvance.Text, out decimal a) ? a : 0m;

            decimal netAPayer = total - reduction;
            if (netAPayer < 0) netAPayer = 0;

            if (avance > netAPayer)
            {
                MessageBox.Show($"Le montant versé ne peut pas être supérieur au net à payer.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (avance == 0) statutActuel = "En attente de paiement";
            else if (avance < netAPayer) statutActuel = "Payé partiellement";
            else statutActuel = "Payé";

            decimal resteAPayer = netAPayer - avance;
            string nomClient = ((DataRowView)CbClients.SelectedItem)["nom"]?.ToString() ?? "Le client";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    MySqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        int idCommandeResultat = idCommandeEnCours;
                        string modePaiement = "Espèces";
                        if (CbModePaiement != null && CbModePaiement.SelectedItem != null)
                        {
                            modePaiement = (CbModePaiement.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Espèces";
                        }

                        if (estModification)
                        {
                            string queryUpdate = @"UPDATE commandes 
                                                   SET id_client = @id_client, date_livraison_prevue = @date_l, 
                                                       montant_total = @total, reduction = @reduc, avance = @avance, 
                                                       statut_commande = @statut 
                                                   WHERE id_commande = @id_cmd";

                            using (MySqlCommand cmdCmd = new MySqlCommand(queryUpdate, conn, transaction))
                            {
                                cmdCmd.Parameters.AddWithValue("@id_client", CbClients.SelectedValue);
                                cmdCmd.Parameters.AddWithValue("@date_l", DpLivraison.SelectedDate.Value);
                                cmdCmd.Parameters.AddWithValue("@total", total);
                                cmdCmd.Parameters.AddWithValue("@reduc", reduction);
                                cmdCmd.Parameters.AddWithValue("@avance", avance);
                                cmdCmd.Parameters.AddWithValue("@statut", statutActuel);
                                cmdCmd.Parameters.AddWithValue("@id_cmd", idCommandeEnCours);
                                cmdCmd.ExecuteNonQuery();
                            }

                            string queryDeleteDetails = "DELETE FROM detail_commandes WHERE id_commande = @id_cmd";
                            using (MySqlCommand cmdDel = new MySqlCommand(queryDeleteDetails, conn, transaction))
                            {
                                cmdDel.Parameters.AddWithValue("@id_cmd", idCommandeEnCours);
                                cmdDel.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string queryInsert = @"INSERT INTO commandes 
                            (numero_commande, id_client, date_commande, date_livraison_prevue, montant_total, reduction, avance, statut_commande) 
                            VALUES (@num, @id_client, @date_c, @date_l, @total, @reduc, @avance, @statut);
                            SELECT LAST_INSERT_ID();";

                            using (MySqlCommand cmdCmd = new MySqlCommand(queryInsert, conn, transaction))
                            {
                                cmdCmd.Parameters.AddWithValue("@num", numero);
                                cmdCmd.Parameters.AddWithValue("@id_client", CbClients.SelectedValue);
                                cmdCmd.Parameters.AddWithValue("@date_c", DateTime.Now);
                                cmdCmd.Parameters.AddWithValue("@date_l", DpLivraison.SelectedDate.Value);
                                cmdCmd.Parameters.AddWithValue("@total", total);
                                cmdCmd.Parameters.AddWithValue("@reduc", reduction);
                                cmdCmd.Parameters.AddWithValue("@avance", avance);
                                cmdCmd.Parameters.AddWithValue("@statut", statutActuel);

                                idCommandeResultat = Convert.ToInt32(cmdCmd.ExecuteScalar());
                            }
                        }

                        string queryDetails = @"INSERT INTO detail_commandes (id_commande, id_article, id, prix_unitaire, quantite) 
                                                VALUES (@id_cmd, @id_art, @id_srv, @prix, @qte)";

                        foreach (var item in panier)
                        {
                            using (MySqlCommand cmdDetails = new MySqlCommand(queryDetails, conn, transaction))
                            {
                                cmdDetails.Parameters.AddWithValue("@id_cmd", idCommandeResultat);
                                cmdDetails.Parameters.AddWithValue("@id_art", item.IdArticle);
                                cmdDetails.Parameters.AddWithValue("@id_srv", item.IdService);
                                cmdDetails.Parameters.AddWithValue("@prix", item.PrixUnitaire);
                                cmdDetails.Parameters.AddWithValue("@qte", item.Quantite);
                                cmdDetails.ExecuteNonQuery();
                            }
                        }

                        if (avance > 0)
                        {
                            string queryPaiement = @"INSERT INTO paiements 
                            (id_commande, montant_verse, mode_paiement, statut_reglement) 
                            VALUES (@id_cmd, @montant, @mode, @statut_regle)";

                            using (MySqlCommand cmdPaiement = new MySqlCommand(queryPaiement, conn, transaction))
                            {
                                cmdPaiement.Parameters.AddWithValue("@id_cmd", idCommandeResultat);
                                cmdPaiement.Parameters.AddWithValue("@montant", avance);
                                cmdPaiement.Parameters.AddWithValue("@mode", modePaiement);
                                cmdPaiement.Parameters.AddWithValue("@statut_regle", resteAPayer == 0 ? "Soldé" : "Avance");
                                cmdPaiement.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();

                        // NOUVEAU FLUX : Message de confirmation clair puis retour automatique à la liste
                        MessageBox.Show($"La commande de {nomClient} a été enregistrée avec succès !", "Enregistrement réussi", MessageBoxButton.OK, MessageBoxImage.Information);

                        this.DialogResult = true;
                        this.Close(); // Ferme et actualise la liste principale
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Erreur SQL : {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Connexion impossible : {ex.Message}");
                }
            }
        }
        #endregion

        #region COULOIR AJOUT PANIER ET AUXILIAIRES
        private void BtnAjouterAuPanier_Click(object sender, RoutedEventArgs e)
        {
            if (CbArticles.SelectedValue == null || CbArticles.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un vêtement.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TxtQuantite.Text, out int qte) || qte <= 0)
            {
                MessageBox.Show("Quantité invalide.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (DpDepot.SelectedDate == null || DpLivraison.SelectedDate == null)
            {
                MessageBox.Show("Dates manquantes.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int idArticle = Convert.ToInt32(CbArticles.SelectedValue);
            string nomArticle = ((DataRowView)CbArticles.SelectedItem)["nom_article"]?.ToString() ?? "Article inconnu";

            int idService;
            string nomService;
            decimal coefficientService = 1.00m;

            if (CbServices.SelectedValue == null || CbServices.SelectedItem == null)
            {
                idService = ObtenirIdServiceCompletParDefaut();
                nomService = "Complet (Lavage + Repassage)";
                coefficientService = 1.00m;
                if (idService == -1) return;
            }
            else
            {
                DataRowView ligneService = (DataRowView)CbServices.SelectedItem;
                idService = Convert.ToInt32(ligneService["id"]);
                nomService = ligneService["nom_service"]?.ToString() ?? "Service inconnu";
                coefficientService = Convert.ToDecimal(ligneService["coefficient_prix"]);
            }

            decimal prixBaseArticle = ObtenirPrixDeBaseArticle(idArticle);
            decimal prixCalcule = prixBaseArticle * coefficientService;

            DateTime dateDepot = DpDepot.SelectedDate.Value.Date;
            DateTime dateLivraison = DpLivraison.SelectedDate.Value.Date;
            int joursEcart = (dateLivraison - dateDepot).Days;

            if (joursEcart <= 0)
            {
                MessageBox.Show("La date de livraison doit être postérieure à la date de dépôt !", "Erreur Date", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (joursEcart == 1)
            {
                prixCalcule *= 2.0m;
            }
            else if (joursEcart == 2)
            {
                prixCalcule *= 1.5m;
            }

            panier.Add(new LignePanier
            {
                IdArticle = idArticle,
                IdService = idService,
                Designation = nomArticle,
                Service = nomService,
                PrixUnitaire = prixCalcule,
                Quantite = qte
            });

            TxtQuantite.Text = "1";
            CbArticles.SelectedIndex = -1;
            CbServices.SelectedIndex = -1;

            MettreAJourTotaux();
        }

        private decimal ObtenirCoefficientService(int idService)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT coefficient_prix FROM services WHERE id = @id";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idService);
                        return Convert.ToDecimal(cmd.ExecuteScalar() ?? 1.0m);
                    }
                }
            }
            catch { return 1.0m; }
        }

        private void ChargerClients()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    MySqlDataAdapter da = new MySqlDataAdapter("SELECT id_client, nom FROM clients ORDER BY nom ASC", conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    CbClients.ItemsSource = dt.DefaultView;
                    CbClients.SelectedValuePath = "id_client";
                    CbClients.DisplayMemberPath = "nom";
                }
            }
            catch (Exception ex) { MessageBox.Show($"Erreur clients: {ex.Message}"); }
        }

        private void ChargerArticles()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    MySqlDataAdapter da = new MySqlDataAdapter("SELECT id_article, nom_article FROM articles ORDER BY nom_article ASC", conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    CbArticles.ItemsSource = dt.DefaultView;
                    CbArticles.SelectedValuePath = "id_article";
                    CbArticles.DisplayMemberPath = "nom_article";
                }
            }
            catch (Exception ex) { MessageBox.Show($"Erreur articles: {ex.Message}"); }
        }

        private void ChargerServices()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    MySqlDataAdapter da = new MySqlDataAdapter("SELECT id, nom_service, coefficient_prix FROM services ORDER BY nom_service ASC", conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    CbServices.ItemsSource = dt.DefaultView;
                    CbServices.SelectedValuePath = "id";
                    CbServices.DisplayMemberPath = "nom_service";
                }
            }
            catch (Exception ex) { MessageBox.Show($"Erreur services: {ex.Message}"); }
        }

        private void GenererNumeroCommande() { TxtNumero.Text = "CMD-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"); }

        private int ObtenirIdServiceCompletParDefaut()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    object? res = new MySqlCommand("SELECT id FROM services WHERE nom_service LIKE '%Complet%' LIMIT 1", conn).ExecuteScalar();
                    return res != null ? Convert.ToInt32(res) : -1;
                }
            }
            catch { return -1; }
        }

        private decimal ObtenirPrixDeBaseArticle(int idArticle)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT montant FROM articles WHERE id_article = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idArticle);
                        return Convert.ToDecimal(cmd.ExecuteScalar() ?? 0);
                    }
                }
            }
            catch { return 0; }
        }

        private void BtnSupprimerLignePanier_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is LignePanier line)
            {
                panier.Remove(line);
                MettreAJourTotaux();
            }
        }

        private void MettreAJourTotaux()
        {
            decimal total = 0;
            foreach (var item in panier) total += item.TotalLigne;
            TxtTotal.Text = total.ToString("0");
            CalculerReste(null, null);
        }

        private void CalculerReste(object? sender, TextChangedEventArgs? e)
        {
            if (TxtTotal == null || TxtReduction == null || TxtAvance == null || TxtReste == null) return;
            try
            {
                decimal total = string.IsNullOrEmpty(TxtTotal.Text) ? 0 : Convert.ToDecimal(TxtTotal.Text);
                decimal reduction = string.IsNullOrEmpty(TxtReduction.Text) ? 0 : Convert.ToDecimal(TxtReduction.Text);
                decimal avance = string.IsNullOrEmpty(TxtAvance.Text) ? 0 : Convert.ToDecimal(TxtAvance.Text);
                decimal reste = total - reduction - avance;
                TxtReste.Text = (reste < 0 ? 0 : reste).ToString("0");
            }
            catch { }
        }

        private void ChargerPanierCommandeExistante(int idCommande)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT dc.id_article, dc.id, a.nom_article, s.nom_service, dc.prix_unitaire, dc.quantite 
                                     FROM detail_commandes dc
                                     JOIN articles a ON dc.id_article = a.id_article
                                     JOIN services s ON dc.id = s.id
                                     WHERE dc.id_commande = @id_cmd";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id_cmd", idCommande);
                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            panier.Clear();
                            while (rdr.Read())
                            {
                                panier.Add(new LignePanier
                                {
                                    IdArticle = rdr.GetInt32("id_article"),
                                    IdService = rdr.GetInt32("id"),
                                    Designation = rdr.GetString("nom_article"),
                                    Service = rdr.GetString("nom_service"),
                                    PrixUnitaire = rdr.GetDecimal("prix_unitaire"),
                                    Quantite = rdr.GetInt32("quantite")
                                });
                            }
                        }
                    }
                }
                MettreAJourTotaux();
            }
            catch (Exception ex) { MessageBox.Show($"Erreur détails: {ex.Message}"); }
        }

        private void BtnAjouterClientRapide_Click(object sender, RoutedEventArgs e)
        {
            NouveauClientWindow cf = new NouveauClientWindow { Owner = this };
            if (cf.ChkOuvrirCommande != null) cf.ChkOuvrirCommande.Visibility = Visibility.Collapsed;
            if (cf.ShowDialog() == true) ChargerClients();
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        #endregion
    }
}