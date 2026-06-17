using System;
using System.Data;
using System.IO;
using System.Windows;
using MySql.Data.MySqlClient;
// Alias pour éliminer les conflits WPF/iTextSharp
using iTextDocument = iTextSharp.text.Document;
using iTextPageSize = iTextSharp.text.PageSize;
using iTextElement = iTextSharp.text.Element;
using iTextFont = iTextSharp.text.Font;
using iTextTable = iTextSharp.text.pdf.PdfPTable;
using iTextCell = iTextSharp.text.pdf.PdfPCell;
using iTextPhrase = iTextSharp.text.Phrase;
using iTextParagraph = iTextSharp.text.Paragraph;
using iTextBaseColor = iTextSharp.text.BaseColor;

namespace ElKharis.Services
{
    public class DocumentService
    {
        private static readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";

        public static void GenererDocumentPDF(int idCommande, string typeDocument, string cheminFichier)
        {
            // Initialisation du document A4 avec des marges équilibrées (comme vos 2rem en HTML)
            iTextDocument doc = new iTextDocument(iTextPageSize.A4, 36, 36, 36, 36);

            try
            {
                iTextSharp.text.pdf.PdfWriter.GetInstance(doc, new FileStream(cheminFichier, FileMode.Create));
                doc.Open();

                // 1. CHARTE GRAPHIQUE & POLICES (Inspirées du CSS moderne)
                iTextSharp.text.pdf.BaseFont bf = iTextSharp.text.pdf.BaseFont.CreateFont(iTextSharp.text.pdf.BaseFont.HELVETICA, iTextSharp.text.pdf.BaseFont.CP1252, iTextSharp.text.pdf.BaseFont.NOT_EMBEDDED);

                iTextFont fontEntreprise = new iTextFont(bf, 18, iTextFont.BOLD, new iTextBaseColor(17, 24, 39)); // Très sombre
                iTextFont fontSlogan = new iTextFont(bf, 10, iTextFont.ITALIC, new iTextBaseColor(75, 85, 99)); // Gris #4b5563
                iTextFont fontTitreDoc = new iTextFont(bf, 18, iTextFont.BOLD, new iTextBaseColor(17, 24, 39));

                iTextFont fontNormal = new iTextFont(bf, 10, iTextFont.NORMAL, new iTextBaseColor(31, 41, 55));
                iTextFont fontBold = new iTextFont(bf, 10, iTextFont.BOLD, new iTextBaseColor(17, 24, 39));
                iTextFont fontBordureGris = new iTextFont(bf, 10, iTextFont.NORMAL, new iTextBaseColor(107, 114, 128));

                iTextBaseColor couleurBordureLight = new iTextBaseColor(229, 231, 235); // Gris clair #e5e7eb du HTML
                iTextBaseColor couleurFondGris = new iTextBaseColor(245, 245, 245); // Fond #f5f5f5 du HTML

                // 2. BLOC D'ENTÊTE DIRECTEUR (Flex-like en utilisant un tableau à 2 colonnes)
                iTextTable tableHeader = new iTextTable(2);
                tableHeader.WidthPercentage = 100;
                tableHeader.SetWidths(new float[] { 1.2f, 1f });

                // Partie Gauche : Entreprise Grace & God / ElKharis
                iTextCell cellGauche = new iTextCell();
                cellGauche.Border = iTextCell.NO_BORDER;
                cellGauche.AddElement(new iTextParagraph("Grace & God Solution", fontEntreprise));
                cellGauche.AddElement(new iTextParagraph("\"Le Futur est à Nous\"", fontSlogan));
                tableHeader.AddCell(cellGauche);

                // Partie Droite : Type de document & Numéro unique
                iTextCell cellDroite = new iTextCell();
                cellDroite.Border = iTextCell.NO_BORDER;
                cellDroite.HorizontalAlignment = iTextElement.ALIGN_RIGHT;

                iTextParagraph pTitre = new iTextParagraph(typeDocument.ToUpper(), fontTitreDoc);
                pTitre.Alignment = iTextElement.ALIGN_RIGHT;
                cellDroite.AddElement(pTitre);

                iTextParagraph pNum = new iTextParagraph($"N° 8931542/{idCommande}", fontBold);
                pNum.Alignment = iTextElement.ALIGN_RIGHT;
                cellDroite.AddElement(pNum);

                iTextParagraph pDate = new iTextParagraph($"Date: {DateTime.Now:dd/MM/yyyy}", fontNormal);
                pDate.Alignment = iTextElement.ALIGN_RIGHT;
                cellDroite.AddElement(pDate);

                tableHeader.AddCell(cellDroite);
                doc.Add(tableHeader);

                doc.Add(new iTextParagraph("\n")); // Espacement discret

                // 3. BLOC CLIENTS ET COORDONNÉES (2 colonnes juxtaposées)
                DataRow? commandeRow = RecupererInfosCommande(idCommande);
                if (commandeRow != null)
                {
                    iTextTable tableClient = new iTextTable(2);
                    tableClient.WidthPercentage = 100;
                    tableClient.SetWidths(new float[] { 1f, 1f });

                    // Colonne Client Gauche
                    iTextCell cellClG = new iTextCell();
                    cellClG.Border = iTextCell.NO_BORDER;
                    cellClG.AddElement(new iTextParagraph($"Client : {commandeRow["nom_client"]}", fontBold));
                    cellClG.AddElement(new iTextParagraph($"Email : {commandeRow["email_client"] ?? "N/A"}", fontNormal));
                    tableClient.AddCell(cellClG);

                    // Colonne Client Droite
                    iTextCell cellClD = new iTextCell();
                    cellClD.Border = iTextCell.NO_BORDER;
                    cellClD.AddElement(new iTextParagraph($"Téléphone : {commandeRow["telephone"]}", fontBold));
                    cellClD.AddElement(new iTextParagraph($"NUI : {commandeRow["niu_client"] ?? "N/A"}", fontNormal));
                    tableClient.AddCell(cellClD);

                    doc.Add(tableClient);
                    doc.Add(new iTextParagraph("\n"));

                    // 4. TABLEAU PRINCIPAL DES ARTICLES (.print-table)
                    iTextTable tableArticles = new iTextTable(4);
                    tableArticles.WidthPercentage = 100;
                    tableArticles.SetWidths(new float[] { 2.5f, 1.2f, 1f, 1.3f });

                    // Entêtes du tableau
                    string[] headers = { "Article", "Prix unitaire", "Quantité", "Total" };
                    foreach (string header in headers)
                    {
                        iTextCell hCell = new iTextCell(new iTextPhrase(header, fontBold));
                        hCell.BorderColor = couleurBordureLight;
                        hCell.Border = iTextCell.BOTTOM_BORDER; // Seulement une bordure basse élégante
                        hCell.PaddingBottom = 8;
                        hCell.BackgroundColor = couleurFondGris;
                        hCell.HorizontalAlignment = (header == "Article") ? iTextElement.ALIGN_LEFT : iTextElement.ALIGN_RIGHT;
                        tableArticles.AddCell(hCell);
                    }

                    // Remplissage avec la boucle dynamique (.map().join(''))
                    DataTable dtDetails = RecupererDetailsCommande(idCommande);
                    decimal totalArticlesHT = 0;

                    foreach (DataRow detail in dtDetails.Rows)
                    {
                        decimal pu = Convert.ToDecimal(detail["prix_unitaire"]);
                        int qte = Convert.ToInt32(detail["quantite"]);
                        decimal totalLigne = pu * qte;
                        totalArticlesHT += totalLigne;

                        // Article
                        iTextCell cName = new iTextCell(new iTextPhrase(detail["nom_article"].ToString(), fontNormal));
                        cName.Border = iTextCell.BOTTOM_BORDER;
                        cName.BorderColor = couleurBordureLight;
                        cName.Padding = 8;
                        tableArticles.AddCell(cName);

                        // Prix Unitaire
                        iTextCell cPrice = new iTextCell(new iTextPhrase($"{pu.ToString("N0")} FCFA", fontNormal));
                        cPrice.Border = iTextCell.BOTTOM_BORDER;
                        cPrice.BorderColor = couleurBordureLight;
                        cPrice.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                        cPrice.Padding = 8;
                        tableArticles.AddCell(cPrice);

                        // Quantité
                        iTextCell cQty = new iTextCell(new iTextPhrase(qte.ToString(), fontNormal));
                        cQty.Border = iTextCell.BOTTOM_BORDER;
                        cQty.BorderColor = couleurBordureLight;
                        cQty.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                        cQty.Padding = 8;
                        tableArticles.AddCell(cQty);

                        // Total Ligne
                        iTextCell cAmount = new iTextCell(new iTextPhrase($"{totalLigne.ToString("N0")} FCFA", fontNormal));
                        cAmount.Border = iTextCell.BOTTOM_BORDER;
                        cAmount.BorderColor = couleurBordureLight;
                        cAmount.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                        cAmount.Padding = 8;
                        tableArticles.AddCell(cAmount);
                    }
                    doc.Add(tableArticles);

                    // 5. TABLEAU DE SYNTHÈSE FINANCIÈRE (summaryTableHtml)
                    iTextTable tableSummary = new iTextTable(2);
                    tableSummary.WidthPercentage = 100;
                    tableSummary.SetWidths(new float[] { 3f, 1f });

                    // Ligne Total Articles
                    AjouterCelluleSynthese(tableSummary, "Total Articles", fontBold, iTextElement.ALIGN_RIGHT, couleurBordureLight);
                    AjouterCelluleSynthese(tableSummary, $"{totalArticlesHT.ToString("N0")} FCFA", fontBold, iTextElement.ALIGN_RIGHT, couleurBordureLight);

                    // Ligne Support de réalisation (Pris dynamiquement ou à défaut 0)
                    decimal supportReal = commandeRow["reduction"] != DBNull.Value ? Convert.ToDecimal(commandeRow["reduction"]) : 0;
                    AjouterCelluleSynthese(tableSummary, "Support de réalisation", fontBold, iTextElement.ALIGN_RIGHT, couleurBordureLight);
                    AjouterCelluleSynthese(tableSummary, $"{supportReal.ToString("N0")} FCFA", fontBold, iTextElement.ALIGN_RIGHT, couleurBordureLight);

                    // Ligne GRAND TOTAL (Mise en avant avec arrière-plan #f5f5f5)
                    decimal totalTTC = Convert.ToDecimal(commandeRow["montant_total"]);

                    iTextCell cellTotalLbl = new iTextCell(new iTextPhrase("TOTAL", fontBold));
                    cellTotalLbl.BackgroundColor = couleurFondGris;
                    cellTotalLbl.Border = iTextCell.NO_BORDER;
                    cellTotalLbl.Padding = 10;
                    cellTotalLbl.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                    tableSummary.AddCell(cellTotalLbl);

                    iTextCell cellTotalVal = new iTextCell(new iTextPhrase($"{totalTTC.ToString("N0")} FCFA", fontBold));
                    cellTotalVal.BackgroundColor = couleurFondGris;
                    cellTotalVal.Border = iTextCell.NO_BORDER;
                    cellTotalVal.Padding = 10;
                    cellTotalVal.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                    tableSummary.AddCell(cellTotalVal);

                    doc.Add(tableSummary);
                    doc.Add(new iTextParagraph("\n"));

                    // 6. ZONE CONDITIONS DE PAIEMENT & CACHET (conditionsAndStampHtml)
                    iTextTable tableFooterBlock = new iTextTable(2);
                    tableFooterBlock.WidthPercentage = 100;
                    tableFooterBlock.SetWidths(new float[] { 1.5f, 1f });

                    // Bloc des mentions légales / Conditions
                    iTextCell cellConditions = new iTextCell();
                    cellConditions.Border = iTextCell.NO_BORDER;
                    cellConditions.AddElement(new iTextParagraph("Mode de payement: Espèces / Mobile Money", fontBordureGris));
                    cellConditions.AddElement(new iTextParagraph("Délai de livraison: Selon planning machine", fontBordureGris));
                    cellConditions.AddElement(new iTextParagraph("\"80% du payement à la commande\"", fontBordureGris));
                    cellConditions.AddElement(new iTextParagraph("Validité de l'offre: 3 jours", fontBold));
                    tableFooterBlock.AddCell(cellConditions);

                    // Bloc Cachet Direction à droite
                    iTextCell cellCachet = new iTextCell();
                    cellCachet.Border = iTextCell.NO_BORDER;
                    cellCachet.HorizontalAlignment = iTextElement.ALIGN_RIGHT;

                    iTextParagraph pDir = new iTextParagraph("LA DIRECTION", fontBold);
                    pDir.Alignment = iTextElement.ALIGN_RIGHT;
                    cellCachet.AddElement(pDir);

                    // Optionnel : Si vous avez une image de cachet, vous décommentez ceci :
                    // iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance("cachetElKharis.png");
                    // img.ScaleToFit(120f, 90f);
                    // img.Alignment = iTextElement.ALIGN_RIGHT;
                    // cellCachet.AddElement(img);

                    tableFooterBlock.AddCell(cellCachet);
                    doc.Add(tableFooterBlock);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la construction du design PDF : {ex.Message}", "Erreur Structure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                doc.Close();
            }
        }

        private static void AjouterCelluleSynthese(iTextTable table, string texte, iTextFont font, int alignement, iTextBaseColor couleurBordure)
        {
            iTextCell cell = new iTextCell(new iTextPhrase(texte, font));
            cell.Border = iTextCell.BOTTOM_BORDER;
            cell.BorderColor = couleurBordure;
            cell.Padding = 8;
            cell.HorizontalAlignment = alignement;
            table.AddCell(cell);
        }

        private static DataRow? RecupererInfosCommande(int idCommande)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT c.*, 
                                        CONCAT(cl.nom, ' ', IFNULL(cl.prenom, '')) AS nom, 
                                        cl.telephone, cl.email AS email_client, cl.id_client AS niu_client
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
                // Correction appliquée suite au correctif d'identification de clé SQL (id_article / id_service)
                string query = @"SELECT dc.*, a.nom_article, s.nom_service
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