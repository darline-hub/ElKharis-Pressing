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
    /// Logique d'interaction pour NouvelleCommandeWindow.xaml
    /// </summary>
    public partial class NouvelleCommandeWindow : Window
    {
        public NouvelleCommandeWindow()
        {
            InitializeComponent();
                ChargerClients();
                ChargerServices();

                TxtNumero.Text = "CMD-" + DateTime.Now.ToString("yyyyMMdd-HHmm");

                DpLivraison.SelectedDate = DateTime.Now.AddDays(3);
            }

            private void ChargerClients()
            {
                try
                {
                    using (MySqlConnection conn = DbConnection.GetConnection())
                    {
                        if (conn == null) return;

                        string query = "SELECT id_client, CONCAT(nom, ' ', IFNULL(prenom, '')) AS nom FROM clients ORDER BY nom ASC";
                        MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        CbClients.ItemsSource = dt.DefaultView;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors du chargement des clients : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            private void ChargerServices()
            {
                try
                {
                    using (MySqlConnection conn = DbConnection.GetConnection())
                    {
                        if (conn == null) return;

                        string query = "SELECT id_service, nom_service FROM services ORDER BY id_service ASC";
                        MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        CbServices.ItemsSource = dt.DefaultView;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors du chargement des services : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            private void CalculerReste(object sender, TextChangedEventArgs e)
            {
               
                if (TxtTotal == null || TxtReduction == null || TxtReste == null) return;

                decimal total = 0;
                decimal reduction = 0;

                decimal.TryParse(TxtTotal.Text, out total);
                decimal.TryParse(TxtReduction.Text, out reduction);

                decimal reste = total - reduction;

               
                TxtReste.Text = reste.ToString("N0") + " FCFA";
            }

            
            private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
            {
               
                if (CbClients.SelectedValue == null)
                {
                    MessageBox.Show("Veuillez sélectionner un client.", "Champ manquant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CbServices.SelectedValue == null)
                {
                    MessageBox.Show("Veuillez sélectionner un service pour ce dépôt.", "Champ manquant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtTotal.Text) || !decimal.TryParse(TxtTotal.Text, out _))
                {
                    MessageBox.Show("Veuillez saisir un montant total valide.", "Champ invalide", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MySqlTransaction? transaction = null;

                try
                {
                    using (MySqlConnection conn = DbConnection.GetConnection())
                    {
                        if (conn == null) return;

                    
                        transaction = conn.BeginTransaction();

                        decimal total = decimal.Parse(TxtTotal.Text);
                        decimal reduction = 0;
                        decimal.TryParse(TxtReduction.Text, out reduction);
                        decimal reste = total - reduction;

                        string queryCommande = @"INSERT INTO commandes (numero_commande, date_commande, montant_total, reduction, reste_a_payer, statut_commande, date_livraison_prevue, id_client, id_utilisateur) 
                                             VALUES (@num, @date, @total, @reduc, @reste, @statut, @livraison, @client, @user);
                                             SELECT LAST_INSERT_ID();";

                        MySqlCommand cmdCmd = new MySqlCommand(queryCommande, conn, transaction);
                        cmdCmd.Parameters.AddWithValue("@num", TxtNumero.Text);
                        cmdCmd.Parameters.AddWithValue("@date", DateTime.Now);
                        cmdCmd.Parameters.AddWithValue("@total", total);
                        cmdCmd.Parameters.AddWithValue("@reduc", reduction);
                        cmdCmd.Parameters.AddWithValue("@reste", reste);
                        cmdCmd.Parameters.AddWithValue("@statut", "Reçue");
                        cmdCmd.Parameters.AddWithValue("@livraison", DpLivraison.SelectedDate ?? DateTime.Now.AddDays(3));
                        cmdCmd.Parameters.AddWithValue("@client", CbClients.SelectedValue);
                        cmdCmd.Parameters.AddWithValue("@user", 1); 

                    int newCommandeId = Convert.ToInt32(cmdCmd.ExecuteScalar()!);

                    string queryDetail = @"INSERT INTO detail_commandes (quantite, prix_unitaire, sous_total, etat_article, id_commande, id_article, id_service, description) 
                                           VALUES (@qte, @pu, @st, @etat, @idCmd, @idArt, @idServ, @desc)";

                        MySqlCommand cmdDetail = new MySqlCommand(queryDetail, conn, transaction);
                        cmdDetail.Parameters.AddWithValue("@qte", 1);
                        cmdDetail.Parameters.AddWithValue("@pu", total);
                        cmdDetail.Parameters.AddWithValue("@st", total);
                        cmdDetail.Parameters.AddWithValue("@etat", "Reçu");
                        cmdDetail.Parameters.AddWithValue("@idCmd", newCommandeId);
                        cmdDetail.Parameters.AddWithValue("@idArt", 1); 
                        cmdDetail.Parameters.AddWithValue("@idServ", CbServices.SelectedValue);
                        cmdDetail.Parameters.AddWithValue("@desc", TxtArticles.Text);

                        cmdDetail.ExecuteNonQuery();

                        transaction.Commit();

                        MessageBox.Show("Le dépôt a été enregistré avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                        this.DialogResult = true;
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    if (transaction != null)
                    {
                        transaction.Rollback();
                    }
                    MessageBox.Show("Erreur lors de l'enregistrement du dépôt : " + ex.Message, "Échec", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

           
            private void BtnFermer_Click(object sender, RoutedEventArgs e)
            {
                this.Close();
            }
     }
}
