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

        // Collection dynamique liée à la DataGrid (DgPanier)
        private ObservableCollection<LignePanier> panier = new ObservableCollection<LignePanier>();

        private bool estModification = false;
        private int idCommandeEnCours = 0;
        private string statutActuel = "Créée";

        // CONSTRUCTEUR 1 : Appel classique depuis le tableau de bord / menu
        public NouvelleCommandeWindow()
        {
            InitializeComponent();
            InitialiserFormulaire();
        }

        // CONSTRUCTEUR 2 : Appel automatique après création d'un nouveau client
        public NouvelleCommandeWindow(long idClient)
        {
            InitializeComponent();
            this._idClientForce = idClient;
            InitialiserFormulaire();
        }

        // CONSTRUCTEUR 3 : Appel pour charger une commande existante (Mode Consultation/Modification)
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

            // 3. Verrouillage si la commande est livrée (Règle CA-GCommande-009)
            if (statutActuel.Equals("Livrée", StringComparison.OrdinalIgnoreCase) || statutActuel.Equals("Livre", StringComparison.OrdinalIgnoreCase))
            {
                VerrouillerFormulaire();
            }

            // 4. Charger les articles du panier depuis la table detail_commandes
            ChargerPanierCommandeExistante(idCommandeEnCours);
        }

        // Bloque les modifications si la commande est déjà archivée/livrée
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
            MessageBox.Show("Cette commande est déjà LIVRÉE. Elle est affichée en lecture seule et ne peut plus être modifiée (Règle CA-GCommande-009).", "Commande Verrouillée", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InitialiserFormulaire()
        {
            // Liaison de la liste en mémoire (panier) à la DataGrid visuelle
            DgPanier.ItemsSource = panier;

            // RÈGLE MÉTIER : Dépôt = Aujourd'hui, Livraison Prévue = 3 jours de délai standard
            DpDepot.SelectedDate = DateTime.Today;
            DpLivraison.SelectedDate = DateTime.Today.AddDays(3);

            // Charger les ComboBox de façon synchronisée avec la base
            ChargerClients();
            ChangeApresChargementClient();
            ChargerArticles();
            ChargerServices();

            // On ne génère un numéro automatique que si on est en mode création
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

        #region TRANSACTION ET ENREGISTREMENT SQL FINAL (CRÉATION ET MODIFICATION FUSIONNÉES)

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            // [Vos vérifications de panier et de client existantes...]

            string numero = TxtNumero.Text.Trim();
            decimal total = Convert.ToDecimal(TxtTotal.Text);
            decimal reduction = decimal.TryParse(TxtReduction.Text, out decimal r) ? r : 0m;
            decimal avance = decimal.TryParse(TxtAvance.Text, out decimal a) ? a : 0m;


            // CA-GFacture-008 : Application de la réduction sur le net à payer
            decimal netAPayer = total - reduction;
            if (netAPayer < 0) netAPayer = 0;

            // CA-GFacture-009 : Empêcher un paiement supérieur au montant restant à payer
            if (avance > netAPayer)
            {
                MessageBox.Show($"Le montant versé ({avance} F) ne peut pas être supérieur au net à payer ({netAPayer} F) après réduction (Règle CA-GFacture-009).",
                                "Erreur de Saisie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (avance == 0)
            {
                statutActuel = "En attente de paiement";
            }
            else if (avance < netAPayer)
            {
                statutActuel = "Payé partiellement"; 
            }
            else
            {
                statutActuel = "Payé";
            }

            decimal resteAPayer = netAPayer - avance;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    int idCommandeResultat = idCommandeEnCours;

                    // Récupération du mode de paiement depuis votre ComboBox (Ex: CbModePaiement)
                    // CA-GFacture-004 : Espèces ou Mobile Money
                    string modePaiement = "Espèces";
                    if (CbModePaiement != null && CbModePaiement.SelectedItem != null)
                    {
                        modePaiement = (CbModePaiement.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Espèces";
                    }

                    if (estModification)
                    {
                        // [Votre code UPDATE commandes existant...]
                        // Pensez à passer @statut avec la variable statutActuel mise à jour
                    }
                    else
                    {
                        // MODE CRÉATION
                        string queryInsert = @"INSERT INTO commandes 
                    (numero_commande, id_client, date_commande, date_livraison_prevue, montant_total, reduction, avance, statut_commande) 
                    VALUES (@num, @id_client, @date_c, @date_l, @total, @reduc, @avance, @statut);
                    SELECT LAST_INSERT_ID();";

                        using (MySqlCommand cmdCmd = new MySqlCommand(queryInsert, conn, transaction))
                        {
                            cmdCmd.Parameters.AddWithValue("@num", numero);
                            cmdCmd.Parameters.AddWithValue("@id_client", CbClients.SelectedValue);
                            cmdCmd.Parameters.AddWithValue("@date_c", DateTime.Now);
                            cmdCmd.Parameters.AddWithValue("@total", total);
                            cmdCmd.Parameters.AddWithValue("@reduc", reduction);
                            cmdCmd.Parameters.AddWithValue("@avance", avance);
                            cmdCmd.Parameters.AddWithValue("@statut", statutActuel);

                            idCommandeResultat = Convert.ToInt32(cmdCmd.ExecuteScalar());

                            // 1. On vérifie si une date a bien été choisie
                            if (DpLivraison.SelectedDate == null)
                            {
                                MessageBox.Show("Veuillez sélectionner une date de livraison prévue.",
                                                "Saisie incomplète", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return; // On arrête l'exécution ici
                            }

                            // 2. Si c'est bon, on peut utiliser .Value en toute sécurité plus bas dans les paramètres :
                            cmdCmd.Parameters.AddWithValue("@date_l", DpLivraison.SelectedDate.Value);
                        }
                    }

                    // [Votre boucle foreach (var item in panier) pour insérer les détails...]

                    // CA-GFacture-001 & CA-GFacture-002 : Enregistrement du paiement si > 0
                    if (avance > 0)
                    {
                        string queryPaiement = @"INSERT INTO paiements 
                    (id_commande, montant_verse, mode_paiement, statut_reglement) 
                    VALUES (@id_cmd, @montant, @mode, @statut_regle)";

                        using (MySqlCommand cmdPaiement = new MySqlCommand(queryPaiement, conn, transaction))
                        {
                            cmdPaiement.Parameters.AddWithValue("@id_cmd", idCommandeResultat);
                            cmdPaiement.Parameters.AddWithValue("@montant", avance); // CA-GFacture-002 (supérieur à 0)
                            cmdPaiement.Parameters.AddWithValue("@mode", modePaiement); // CA-GFacture-004
                            cmdPaiement.Parameters.AddWithValue("@statut_regle", resteAPayer == 0 ? "Soldé" : "Avance");

                            cmdPaiement.ExecuteNonQuery(); // Génère automatiquement l'ID unique via l'AUTO_INCREMENT (CA-GFacture-001)
                        }
                    }

                    transaction.Commit();

                    // 1. Déclarer la variable pour stocker le chemin du fichier
                    string cheminChoisi = "";

                    // 2. Récupérer le nom du client depuis votre formulaire pour nommer proprement le fichier
                    // (Exemple : TxtNomClient.Text ou la variable que vous utilisez dans votre fenêtre)
                    string nomClientPourFichier = "Client";
                    if (this.DataContext is DataRowView drv)
                    {
                        nomClientPourFichier = drv["nom"]?.ToString() ?? "Client";
                    }

                    // 3. Déterminer le type de document par défaut pour la boîte de dialogue
                    string typeDocumentInitial = (resteAPayer > 0) ? "REÇU_DE_DÉPÔT" : "FACTURE";

                    // 4. Ouvrir la boîte de dialogue "Enregistrer sous"
                    Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                    saveFileDialog.Filter = "Document PDF (*.pdf)|*.pdf";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    saveFileDialog.FileName = $"{typeDocumentInitial}_{idCommandeResultat}_{nomClientPourFichier.Replace(" ", "_")}";

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        // On affecte la valeur choisie par l'utilisateur à la variable
                        cheminChoisi = saveFileDialog.FileName;

                        try
                        {
                            // 5. Votre bloc If/Else mis à jour avec les 3 paramètres requis
                            if (resteAPayer > 0)
                            {
                                ElKharis.Services.DocumentService.GenererDocumentPDF(idCommandeResultat, "REÇU DE DÉPÔT", cheminChoisi);
                            }
                            else
                            {
                                ElKharis.Services.DocumentService.GenererDocumentPDF(idCommandeResultat, "FACTURE", cheminChoisi);
                            }

                            // 6. Ouvrir instantanément le PDF pour prévisualisation et impression
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(cheminChoisi)
                            {
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erreur lors de la création ou de l'ouverture du PDF : {ex.Message}", "Erreur PDF", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                }catch (Exception ex)
                {
                    MessageBox.Show($"Une erreur est survenue : {ex.Message}");
                }
            }
        }
        #endregion

        #region CHARGEMENT DES DONNÉES INITIALES (BDD)

        private void ChargerClients()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id_client, nom FROM clients ORDER BY nom ASC";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    CbClients.ItemsSource = dt.DefaultView;
                    CbClients.SelectedValuePath = "id_client";
                    CbClients.DisplayMemberPath = "nom";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des clients : {ex.Message}", "Erreur SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerArticles()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id_article, nom_article FROM articles ORDER BY nom_article ASC";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    CbArticles.ItemsSource = dt.DefaultView;
                    CbArticles.SelectedValuePath = "id_article";
                    CbArticles.DisplayMemberPath = "nom_article";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des articles : {ex.Message}", "Erreur SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerServices()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, nom_service, coefficient_prix FROM services ORDER BY nom_service ASC";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    CbServices.ItemsSource = dt.DefaultView;
                    CbServices.SelectedValuePath = "id";
                    CbServices.DisplayMemberPath = "nom_service";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des services : {ex.Message}", "Erreur SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenererNumeroCommande()
        {
            TxtNumero.Text = "CMD-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        }

        #endregion

        #region GESTION DU PANIER (AJOUT / SUPPRESSION ET CALCULS DYNAMIQUES)

        private void BtnAjouterAuPanier_Click(object sender, RoutedEventArgs e)
        {
            if (CbArticles.SelectedValue == null || CbArticles.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un type de vêtement.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TxtQuantite.Text, out int qte) || qte <= 0)
            {
                MessageBox.Show("Veuillez saisir une quantité valide supérieure à 0.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (DpDepot.SelectedDate == null || DpLivraison.SelectedDate == null)
            {
                MessageBox.Show("Les dates de dépôt et de livraison doivent être renseignées pour calculer le tarif.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                if (idService == -1)
                {
                    MessageBox.Show("Le service par défaut 'Complet' est introuvable dans votre table SQL.", "Erreur Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                DataRowView ligneService = (DataRowView)CbServices.SelectedItem;
                idService = Convert.ToInt32(ligneService["id"]);
                nomService = ligneService["nom_service"]?.ToString() ?? "Service inconnu";
                coefficientService = Convert.ToDecimal(ligneService["coefficient_prix"]);
            }

            decimal prixBaseArticle = ObtenirPrixDeBaseArticle(idArticle);

            if (prixBaseArticle == 0)
            {
                MessageBox.Show("Attention, le prix de cet article est configuré à 0 FCFA ou n'a pas été trouvé dans la base de données.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            decimal prixCalcule = prixBaseArticle * coefficientService;

            DateTime dateDepot = DpDepot.SelectedDate.Value.Date;
            DateTime dateLivraison = DpLivraison.SelectedDate.Value.Date;
            int joursEcart = (dateLivraison - dateDepot).Days;

            if (joursEcart <= 0)
            {
                MessageBox.Show("La date de livraison doit être postérieure à la date de dépôt !", "Erreur de Date", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (joursEcart == 1)
            {
                prixCalcule = prixCalcule * 2.0m;
            }
            else if (joursEcart == 2)
            {
                prixCalcule = prixCalcule * 1.5m;
            }

            LignePanier nouvelleLigne = new LignePanier
            {
                IdArticle = idArticle,
                IdService = idService,
                Designation = nomArticle,
                Service = nomService,
                PrixUnitaire = prixCalcule,
                Quantite = qte
            };

            panier.Add(nouvelleLigne);

            TxtQuantite.Text = "1";
            CbArticles.SelectedIndex = -1;
            CbServices.SelectedIndex = -1;

            MettreAJourTotaux();
        }

        private int ObtenirIdServiceCompletParDefaut()
        {
            int idResultat = -1;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id FROM services WHERE nom_service LIKE '%Complet%' LIMIT 1";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        object? res = cmd.ExecuteScalar();
                        if (res != null && res != DBNull.Value)
                        {
                            idResultat = Convert.ToInt32(res);
                        }
                    }
                }
            }
            catch
            {
                idResultat = -1;
            }
            return idResultat;
        }

        private decimal ObtenirPrixDeBaseArticle(int idArticle)
        {
            decimal prix = 0;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT montant FROM articles WHERE id_article = @id_article";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id_article", idArticle);
                        object? result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            prix = Convert.ToDecimal(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la récupération du prix de l'article : {ex.Message}", "Erreur BDD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return prix;
        }

        private void BtnSupprimerLignePanier_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            if (btn.DataContext is LignePanier ligneARetirer)
            {
                panier.Remove(ligneARetirer);
                MettreAJourTotaux();
            }
        }

        private void MettreAJourTotaux()
        {
            decimal totalGénéral = 0;
            foreach (var item in panier)
            {
                totalGénéral += item.TotalLigne;
            }

            TxtTotal.Text = totalGénéral.ToString("0");
            CalculerReste(null, null);
        }

        private void CalculerReste(object? sender, TextChangedEventArgs? e)
        {
            if (TxtTotal == null || TxtReduction == null || TxtAvance == null || TxtReste == null)
            {
                return;
            }

            try
            {
                decimal total = string.IsNullOrEmpty(TxtTotal.Text) ? 0 : Convert.ToDecimal(TxtTotal.Text);
                decimal reduction = string.IsNullOrEmpty(TxtReduction.Text) ? 0 : Convert.ToDecimal(TxtReduction.Text);
                decimal avance = string.IsNullOrEmpty(TxtAvance.Text) ? 0 : Convert.ToDecimal(TxtAvance.Text);

                decimal reste = total - reduction - avance;
                if (reste < 0) reste = 0;

                TxtReste.Text = reste.ToString("0");
            }
            catch
            {
                
            }
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
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            panier.Clear();
                            while (reader.Read())
                            {
                                panier.Add(new LignePanier
                                {
                                    IdArticle = reader.GetInt32("id_article"),
                                    IdService = reader.GetInt32("id"),
                                    Designation = reader.GetString("nom_article"),
                                    Service = reader.GetString("nom_service"),
                                    PrixUnitaire = reader.GetDecimal("prix_unitaire"),
                                    Quantite = reader.GetInt32("quantite")
                                });
                            }
                        }
                    }
                }
                MettreAJourTotaux();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des détails de la commande : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region COULOIR DE CRÉATION RAPIDE DE CLIENT

        private void BtnAjouterClientRapide_Click(object sender, RoutedEventArgs e)
        {
            NouveauClientWindow clientForm = new NouveauClientWindow();
            clientForm.Owner = this;

            if (clientForm.ChkOuvrirCommande != null)
            {
                clientForm.ChkOuvrirCommande.Visibility = Visibility.Collapsed;
            }

            if (clientForm.ShowDialog() == true)
            {
                string? clientPrecedentSelectionne = CbClients.SelectedValue?.ToString();

                ChargerClients();

                if (!string.IsNullOrEmpty(clientPrecedentSelectionne))
                {
                    CbClients.SelectedValue = Convert.ToInt64(clientPrecedentSelectionne);
                }
            }
        }

        #endregion

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}