using System;
using System.Data;
using System.IO;
using System.Windows;
using iTextSharp.text.pdf;
using MySql.Data.MySqlClient;
// Utilisation d'alias pour éviter les conflits entre WPF et iTextSharp
using iTextDocument = iTextSharp.text.Document;
using iTextElement = iTextSharp.text.Element;
using iTextFont = iTextSharp.text.Font;
using iTextPageSize = iTextSharp.text.PageSize;

namespace ElKharis.Models
{
    public class DocumentService
    {
        private static readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";

        public static void GenererDocumentPDF(int idCommande, string typeDocument)
        {
            // typeDocument peut être "REÇU" ou "FACTURE"
            string dossierDest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ElKharis_Documents");
            if (!Directory.Exists(dossierDest)) Directory.CreateDirectory(dossierDest);

            string cheminFichier = Path.Combine(dossierDest, $"{typeDocument}_{idCommande}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            iTextDocument doc = new iTextDocument(iTextPageSize.A4, 36, 36, 36, 36);

            try
            {
                PdfWriter.GetInstance(doc, new FileStream(cheminFichier, FileMode.Create));
                doc.Open();

                // 1. POLICES & COULEURS
                BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                iTextFont fontTitre = new iTextFont(bf, 18, iTextFont.BOLD, new iTextSharp.text.BaseColor(79, 70, 229)); // Indigo
                iTextFont fontSousTitre = new iTextFont(bf, 14, iTextFont.BOLD, iTextSharp.text.BaseColor.DARK_GRAY);
                iTextFont fontNormal = new iTextFont(bf, 10, iTextFont.NORMAL, iTextSharp.text.BaseColor.BLACK);
                iTextFont fontBold = new iTextFont(bf, 10, iTextFont.BOLD, iTextSharp.text.BaseColor.BLACK);
                iTextFont fontTotal = new iTextFont(bf, 11, iTextFont.BOLD, new iTextSharp.text.BaseColor(79, 70, 229));
                iTextFont fontSession = new iTextFont(bf, 9, iTextFont.ITALIC, iTextSharp.text.BaseColor.GRAY); // Police pour l'agent

                // 2. ENTÊTE : LOGO & INFOS ENTREPRISE
                PdfPTable tableEntete = new PdfPTable(2);
                tableEntete.WidthPercentage = 100;
                tableEntete.SetWidths(new float[] { 1f, 1f });

                // Bloc Gauche : Infos Entreprise
                PdfPCell cellInfos = new PdfPCell();
                cellInfos.Border = iTextSharp.text.Rectangle.NO_BORDER;
                cellInfos.AddElement(new iTextSharp.text.Paragraph("PRESSING EL-KHARIS", fontTitre));
                cellInfos.AddElement(new iTextSharp.text.Paragraph("Service de Nettoyage Professionnel\nDouala, Cameroun\nTél: +237 XXX XXX XXX\nEmail: contact@elkharis.com", fontNormal));
                tableEntete.AddCell(cellInfos);

                // Bloc Droite : Type de Document, Date & Prise en compte de la SESSION
                string nomAgent = Session.NomUtilisateur ?? "Système"; // Récupération de la session anonyme par sécurité

                PdfPCell cellDocInfo = new PdfPCell();
                cellDocInfo.Border = iTextSharp.text.Rectangle.NO_BORDER;
                cellDocInfo.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                cellDocInfo.AddElement(new iTextSharp.text.Paragraph($"{typeDocument} N°: {idCommande}", fontSousTitre));
                cellDocInfo.AddElement(new iTextSharp.text.Paragraph($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}", fontNormal));
                cellDocInfo.AddElement(new iTextSharp.text.Paragraph($"Émis par : {nomAgent}", fontSession)); // Intégration de la session sur le document PDF
                tableEntete.AddCell(cellDocInfo);

                doc.Add(tableEntete);
                doc.Add(new iTextSharp.text.Paragraph("\n")); // Espace

                // 3. RÉCUPÉRATION DES DONNÉES EN BDD (Infos Commande & Client)
                DataRow? commandeRow = RecupererInfosCommande(idCommande);
                if (commandeRow != null)
                {
                    // Informations du Client
                    iTextSharp.text.Paragraph pClient = new iTextSharp.text.Paragraph($"Client : {commandeRow["nom_client"]}\nTél : {commandeRow["telephone"]}", fontBold);
                    pClient.Alignment = iTextElement.ALIGN_LEFT;
                    doc.Add(pClient);
                    doc.Add(new iTextSharp.text.Paragraph("\n"));

                    // 4. TABLEAU DES ARTICLES & SERVICES
                    PdfPTable tableArticles = new PdfPTable(5);
                    tableArticles.WidthPercentage = 100;
                    tableArticles.SetWidths(new float[] { 2f, 1.5f, 1f, 1f, 1.5f });

                    // Headers du tableau
                    string[] headers = { "Article", "Service", "Qté", "P.U", "Total" };
                    foreach (string header in headers)
                    {
                        PdfPCell hCell = new PdfPCell(new iTextSharp.text.Phrase(header, fontBold));
                        hCell.BackgroundColor = new iTextSharp.text.BaseColor(243, 244, 246);
                        hCell.Padding = 8;
                        hCell.HorizontalAlignment = iTextElement.ALIGN_CENTER;
                        tableArticles.AddCell(hCell);
                    }

                    // Remplissage avec les détails de la commande
                    DataTable dtDetails = RecupererDetailsCommande(idCommande);
                    foreach (DataRow detail in dtDetails.Rows)
                    {
                        tableArticles.AddCell(new PdfPCell(new iTextSharp.text.Phrase(detail["nom_article"].ToString(), fontNormal)) { Padding = 6 });
                        tableArticles.AddCell(new PdfPCell(new iTextSharp.text.Phrase(detail["nom_service"].ToString(), fontNormal)) { Padding = 6 });
                        tableArticles.AddCell(new PdfPCell(new iTextSharp.text.Phrase(detail["quantite"].ToString(), fontNormal)) { HorizontalAlignment = iTextElement.ALIGN_CENTER, Padding = 6 });
                        tableArticles.AddCell(new PdfPCell(new iTextSharp.text.Phrase($"{string.Format("{0:N0}", detail["prix_unitaire"])} F", fontNormal)) { HorizontalAlignment = iTextElement.ALIGN_RIGHT, Padding = 6 });
                        tableArticles.AddCell(new PdfPCell(new iTextSharp.text.Phrase($"{string.Format("{0:N0}", detail["total_ligne"])} F", fontNormal)) { HorizontalAlignment = iTextElement.ALIGN_RIGHT, Padding = 6 });
                    }

                    doc.Add(tableArticles);
                    doc.Add(new iTextSharp.text.Paragraph("\n"));

                    // 5. ZONE DES CALCULS FINANCIERS
                    PdfPTable tableTotaux = new PdfPTable(2);
                    tableTotaux.WidthPercentage = 40;
                    tableTotaux.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                    tableTotaux.SetWidths(new float[] { 1.2f, 1f });

                    AjouterLigneTotal(tableTotaux, "Montant Total :", $"{string.Format("{0:N0}", commandeRow["montant_total"])} FCFA", fontNormal);
                    AjouterLigneTotal(tableTotaux, "Réduction :", $"{string.Format("{0:N0}", commandeRow["reduction"])} FCFA", fontNormal);
                    AjouterLigneTotal(tableTotaux, "Avance versée :", $"{string.Format("{0:N0}", commandeRow["avance"])} FCFA", fontNormal);

                    decimal reste = Convert.ToDecimal(commandeRow["reste_a_payer"]);
                    if (typeDocument == "FACTURE" || reste <= 0)
                    {
                        AjouterLigneTotal(tableTotaux, "Net à Payer :", "0 FCFA", fontTotal);
                        AjouterLigneTotal(tableTotaux, "Statut :", "SOLDE / PAYÉ", fontTotal);
                    }
                    else
                    {
                        AjouterLigneTotal(tableTotaux, "Reste à Payer :", $"{string.Format("{0:N0}", reste)} FCFA", fontTotal);
                    }

                    doc.Add(tableTotaux);
                }

                // Pied de page sécurisé
                doc.Add(new iTextSharp.text.Paragraph("\n\nMerci pour votre confiance. Merci de conserver ce document.", fontNormal) { Alignment = iTextElement.ALIGN_CENTER });

                MessageBox.Show($"{typeDocument} généré avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la génération du PDF : {ex.Message}", "Erreur PDF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                doc.Close();
            }
        }

        private static void AjouterLigneTotal(PdfPTable table, string label, string valeur, iTextFont font)
        {
            table.AddCell(new PdfPCell(new iTextSharp.text.Phrase(label, font)) { Border = iTextSharp.text.Rectangle.NO_BORDER, Padding = 4 });
            table.AddCell(new PdfPCell(new iTextSharp.text.Phrase(valeur, font)) { Border = iTextSharp.text.Rectangle.NO_BORDER, HorizontalAlignment = iTextElement.ALIGN_RIGHT, Padding = 4 });
        }

        private static DataRow? RecupererInfosCommande(int idCommande)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT c.*, 
                                        (c.montant_total - c.reduction - c.avance) AS reste_a_payer,
                                        CONCAT(cl.nom, ' ', IFNULL(cl.prenom, '')) AS nom_client, 
                                        cl.telephone 
                                 FROM commandes c 
                                 INNER JOIN clients cl ON c.id_client = cl.id_client 
                                 WHERE c.id_commande = @id";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", idCommande);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                }
            }
        }

        private static DataTable RecupererDetailsCommande(int idCommande)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // CORRECTION : Changement de 'a.id' par 'a.id_article' (et vérification de 's.id_service')
                string query = @"SELECT dc.*, a.nom_article, s.nom_service, (dc.quantite * dc.prix_unitaire) AS total_ligne
                         FROM detail_commandes dc
                         INNER JOIN articles a ON dc.id_article = a.id_article
                         INNER JOIN services s ON dc.id = s.id
                         WHERE dc.id_commande = @id";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", idCommande);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }
    }
}