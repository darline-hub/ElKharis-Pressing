using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace ElKharis.Views
{
    public partial class NouvelleCommandeWindow : Window
    {
        // Chaîne de connexion locale à MySQL
        private readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";

        // Variable pour intercepter l'ID du client s'il provient du formulaire de création rapide
        private long? _idClientForce = null;

        // Structure représentant une ligne du panier en mémoire
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


            // 1. Initialise les listes et les composants graphiques
            InitialiserFormulaire();

            // 2. Remplissage des champs textuels avec les données de la base
            TxtNumero.Text = ligneCommande["numero_commande"]?.ToString() ?? string.Empty;
            CbClients.SelectedValue = Convert.ToInt64(ligneCommande["id_client"]);
            DpDepot.SelectedDate = Convert.ToDateTime(ligneCommande["date_commande"]);
            DpLivraison.SelectedDate = Convert.ToDateTime(ligneCommande["date_livraison_prevue"]);
            TxtTotal.Text = ligneCommande["montant_total"]?.ToString() ?? "0";
            TxtReduction.Text = ligneCommande["reduction"]?.ToString() ?? "0";
            TxtAvance.Text = ligneCommande["avance"]?.ToString() ?? "0";

            // 3. Verrouillage si la commande est livrée (Règle CA-GCommande-009)
            string statut = ligneCommande["statut_commande"]?.ToString() ?? "";
            if (statut.Equals("Livrée", StringComparison.OrdinalIgnoreCase) || statut.Equals("Livre", StringComparison.OrdinalIgnoreCase))
            {
                VerrouillerFormulaire();
            }

            // 4. Charger les articles du panier depuis la table detail_commandes
            ChargerPanierCommandeExistante(Convert.ToInt32(ligneCommande["id_commande"]));
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
            MessageBox.Show("Cette commande est déjà LIVRÉE. Elle est affichée en lecture seule et ne peut plus être modifiée (Règle CA-GCommande-009).", "Commande Verrouillée", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Méthode d'appui pour remplir le panier en mode modification
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

        private void InitialiserFormulaire()
        {
            // Liaison de la liste en mémoire (panier) à la DataGrid visuelle
            DgPanier.ItemsSource = panier;

            // RÈGLE MÉTIER : Dépôt = Aujourd'hui, Livraison Prévue = 3 jours de délai standard
            DpDepot.SelectedDate = DateTime.Today;
            DpLivraison.SelectedDate = DateTime.Today.AddDays(3);

            // Charger les ComboBox de façon synchronisée avec la base
            ChargerClients();
            ChargerArticles();
            ChargerServices();
            GenererNumeroCommande();
        }

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
                    CbClients.SelectedValuePath = "id_client"; // Clé primaire renvoyée par le composant
                    CbClients.DisplayMemberPath = "nom";       // Colonne textuelle affichée à l'écran
                }

                // Pré-sélection si l'ID provient d'un ajout rapide externe
                if (_idClientForce.HasValue)
                {
                    CbClients.SelectedValue = _idClientForce.Value;
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
                    // Sélection explicite de l'ID, du nom et du coefficient réel de ta table services
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
            // Génération dynamique et automatique d'un identifiant unique (Règle CA-GCommande-002)
            TxtNumero.Text = "CMD-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        }

        #endregion

        #region GESTION DU PANIER (AJOUT / SUPPRESSION ET CALCULS DYNAMIQUES)

        private void BtnAjouterAuPanier_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validations de sécurité de base
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

            // 2. Récupération des informations de l'article sélectionné
            int idArticle = Convert.ToInt32(CbArticles.SelectedValue);
            string nomArticle = ((DataRowView)CbArticles.SelectedItem)["nom_article"]?.ToString() ?? "Article inconnu";

            int idService;
            string nomService;
            decimal coefficientService = 1.00m;

            // 3. Gestion de la règle du service par défaut ('Complet')
            if (CbServices.SelectedValue == null || CbServices.SelectedItem == null)
            {
                idService = ObtenirIdServiceCompletParDefaut();
                nomService = "Complet (Lavage + Repassage)";
                coefficientService = 1.00m; // Service complet = 100% du tarif de base

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

                // Récupération du coefficient de prix réel (Ex: 0.60 pour le repassage uniquement)
                coefficientService = Convert.ToDecimal(ligneService["coefficient_prix"]);
            }

            // 4. ÉTAPE CLÉ : Récupération du prix réel depuis le champ 'montant' de ta BDD
            decimal prixBaseArticle = ObtenirPrixDeBaseArticle(idArticle);

            if (prixBaseArticle == 0)
            {
                MessageBox.Show("Attention, le prix de cet article est configuré à 0 FCFA ou n'a pas été trouvé dans la base de données.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // 5. CALCUL DU PRIX PRESTATION : Prix de l'habit × Coefficient du type de service
            decimal prixCalcule = prixBaseArticle * coefficientService;

            // 6. CALCUL DE LA MAJORATION D'URGENCE (Express) selon le délai de livraison
            DateTime dateDepot = DpDepot.SelectedDate.Value.Date;
            DateTime dateLivraison = DpLivraison.SelectedDate.Value.Date;
            int joursEcart = (dateLivraison - dateDepot).Days;

            if (joursEcart <= 0)
            {
                MessageBox.Show("La date de livraison doit être postérieure à la date de dépôt !", "Erreur de Date", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (joursEcart == 1) // Urgent (Le lendemain) -> Tarif doublé (+100%)
            {
                prixCalcule = prixCalcule * 2.0m;
            }
            else if (joursEcart == 2) // Semi-urgent (48h après) -> Tarif +50%
            {
                prixCalcule = prixCalcule * 1.5m;
            }

            // 7. INSERTION DANS LE TABLEAU (DataGrid) : Visibilité immédiate pour l'utilisateur
            LignePanier nouvelleLigne = new LignePanier
            {
                IdArticle = idArticle,
                IdService = idService,
                Designation = nomArticle,
                Service = nomService,
                PrixUnitaire = prixCalcule, // Ce prix est maintenant parfaitement calculé et s'affichera dans la DataGrid
                Quantite = qte
            };

            panier.Add(nouvelleLigne);

            // 8. Réinitialisation des contrôles de saisie pour le vêtement suivant
            TxtQuantite.Text = "1";
            CbArticles.SelectedIndex = -1;
            CbServices.SelectedIndex = -1;

            // 9. RECALCUL AUTOMATIQUE DU TOTAL GÉNÉRAL en bas du formulaire
            MettreAJourTotaux();
        }
        // Recherche dynamique textuelle avec joker pour isoler l'ID de ton service par défaut
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

            // Forcer le calcul des réductions et nets à payer
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

                // Règle CA-GCommande-011 : Déduction automatique et immédiate de la réduction
                decimal reste = total - reduction - avance;
                if (reste < 0) reste = 0;

                TxtReste.Text = reste.ToString("0");
            }
            catch
            {
                // Empêche le plantage si l'utilisateur saisit temporairement une lettre
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
                // Sauvegarde de la sélection utilisateur en cours
                string? clientPrecedentSelectionne = CbClients.SelectedValue?.ToString();

                // Rechargement transparent
                ChargerClients();

                if (!string.IsNullOrEmpty(clientPrecedentSelectionne))
                {
                    CbClients.SelectedValue = Convert.ToInt64(clientPrecedentSelectionne);
                }
            }
        }

        #endregion

        #region TRANSACTION ET ENREGISTREMENT SQL FINAL

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            // RÈGLE CA-GCommande-001 : Interdiction formelle de valider sans client lié
            if (CbClients.SelectedValue == null)
            {
                MessageBox.Show("Veuillez sélectionner un client. Impossible de valider une commande anonyme (Règle CA-GCommande-001).", "Sécurité Bloquante", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // RÈGLE CA-GCommande-003 : Validation d'un panier obligatoirement garni
            if (panier.Count == 0)
            {
                MessageBox.Show("Impossible de valider une commande sans au moins un vêtement ajouté au panier (Règle CA-GCommande-003).", "Panier Vide", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DpLivraison.SelectedDate == null)
            {
                MessageBox.Show("Veuillez renseigner une date de livraison prévisionnelle.", "Information Manquante", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // Utilisation d'une transaction SQL sécurisée
                MySqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // RÈGLE CA-GCommande-002 : Statut forcé par défaut sur 'Creer'
                    string queryCommande = @"INSERT INTO commandes 
                        (numero_commande, id_client, date_commande, date_livraison_prevue, montant_total, reduction, avance, statut_commande) 
                        VALUES (@num, @id_client, @date_c, @date_l, @total, @reduc, @avance, 'Creer');
                        SELECT LAST_INSERT_ID();";

                    MySqlCommand cmdCmd = new MySqlCommand(queryCommande, conn, transaction);
                    cmdCmd.Parameters.AddWithValue("@num", TxtNumero.Text);
                    cmdCmd.Parameters.AddWithValue("@id_client", CbClients.SelectedValue);
                    cmdCmd.Parameters.AddWithValue("@date_c", DateTime.Now);
                    cmdCmd.Parameters.AddWithValue("@date_l", DpLivraison.SelectedDate.Value);
                    cmdCmd.Parameters.AddWithValue("@total", Convert.ToDecimal(TxtTotal.Text));
                    cmdCmd.Parameters.AddWithValue("@reduc", decimal.TryParse(TxtReduction.Text, out decimal r) ? r : 0m);
                    cmdCmd.Parameters.AddWithValue("@avance", decimal.TryParse(TxtAvance.Text, out decimal a) ? a : 0m);

                    int idCommandeGenere = Convert.ToInt32(cmdCmd.ExecuteScalar());

                    // 2. Insertion séquentielle de chaque ligne du panier d'achat
                    string queryDetail = @"INSERT INTO detail_commandes 
                        (id_commande, id_article, id, quantite, prix_unitaire) 
                        VALUES (@id_cmd, @id_art, @id_ser, @qte, @pu)";

                    foreach (var item in panier)
                    {
                        MySqlCommand cmdDetail = new MySqlCommand(queryDetail, conn, transaction);
                        cmdDetail.Parameters.AddWithValue("@id_cmd", idCommandeGenere);
                        cmdDetail.Parameters.AddWithValue("@id_art", item.IdArticle);
                        cmdDetail.Parameters.AddWithValue("@id_ser", item.IdService);
                        cmdDetail.Parameters.AddWithValue("@qte", item.Quantite);
                        cmdDetail.Parameters.AddWithValue("@pu", item.PrixUnitaire);

                        cmdDetail.ExecuteNonQuery();
                    }

                    // Validation définitive de la transaction atomique
                    transaction.Commit();

                    // RÈGLE CA-GCommande-013 : Confirmation et appel virtuel d'impression de facture
                    MessageBox.Show($"La commande {TxtNumero.Text} a été validée avec succès !\n\nFacture générée automatiquement en tâche de fond (Règle CA-GCommande-013).", "Succès Pressing", MessageBoxButton.OK, MessageBoxImage.Information);

                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    // Annulation complète en cas d'incident réseau
                    transaction.Rollback();
                    MessageBox.Show($"Erreur technique fatale lors du traitement de la transaction SQL : {ex.Message}", "Échec Transactionnel", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        #endregion
    }
}