using System;
using System.Data;
using System.IO;
using System.Windows;
using MySql.Data.MySqlClient;

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
            // Initialisation du document A4 avec des marges de 36pt
            iTextDocument doc = new iTextDocument(iTextPageSize.A4, 36, 36, 36, 36);

            try
            {
                iTextSharp.text.pdf.PdfWriter.GetInstance(doc, new FileStream(cheminFichier, FileMode.Create));
                doc.Open();

                //CHARTE GRAPHIQUE & POLICES
                iTextSharp.text.pdf.BaseFont bf = iTextSharp.text.pdf.BaseFont.CreateFont(iTextSharp.text.pdf.BaseFont.HELVETICA, iTextSharp.text.pdf.BaseFont.CP1252, iTextSharp.text.pdf.BaseFont.NOT_EMBEDDED);

                iTextBaseColor bleuNuit = new iTextBaseColor(26, 54, 93);     // #1A365D
                iTextBaseColor vertSlogan = new iTextBaseColor(40, 167, 69);  // #28A745
                iTextBaseColor grisTexte = new iTextBaseColor(113, 128, 150); // #718096
                iTextBaseColor grisSombre = new iTextBaseColor(31, 41, 55);    // #1F2937

                iTextFont fontEntreprise = new iTextFont(bf, 18, iTextFont.BOLD, bleuNuit);
                iTextFont fontSlogan = new iTextFont(bf, 9.5f, iTextFont.ITALIC, vertSlogan);
                iTextFont fontInfosEntreprise = new iTextFont(bf, 9, iTextFont.NORMAL, grisTexte);

                iTextFont fontTitreDoc = new iTextFont(bf, 12, iTextFont.BOLD, bleuNuit);
                iTextFont fontNormal = new iTextFont(bf, 10, iTextFont.NORMAL, grisSombre);
                iTextFont fontBold = new iTextFont(bf, 10, iTextFont.BOLD, bleuNuit);
                iTextFont fontClientNom = new iTextFont(bf, 11, iTextFont.BOLD, grisSombre);
                iTextFont fontSectionTitre = new iTextFont(bf, 10, iTextFont.BOLD, grisTexte);

                iTextBaseColor couleurBordureLight = new iTextBaseColor(229, 231, 235); // #E5E7EB
                iTextBaseColor couleurFondGris = new iTextBaseColor(245, 245, 245);    // #F5F5F5

                // Récupération des données
                DataRow? commandeRow = RecupererInfosCommande(idCommande);

                
                //EN-TÊTE PRINCIPALE
                iTextTable tableHeader = new iTextTable(2);
                tableHeader.WidthPercentage = 100;
                tableHeader.SetWidths(new float[] { 1.3f, 1f }); // Équilibre gauche/droite

                //PARTIE GAUCHE : LOGO + INFOS ENTREPRISE
                iTextCell cellGauche = new iTextCell();
                cellGauche.Border = iTextCell.NO_BORDER;
                cellGauche.VerticalAlignment = iTextElement.ALIGN_TOP;

                // Gestion et intégration propre du Logo
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    // On cherche d'abord dans le dossier d'exécution direct (grâce à "Copier si plus récent")
                    string cheminLogo = Path.Combine(baseDir, "Resources", "logo.jpeg");
                    if (!File.Exists(cheminLogo)) cheminLogo = Path.Combine(baseDir, "Resources", "logo.png");

                    // Sécurité de secours si exécuté depuis l'environnement de dev sans copie
                    if (!File.Exists(cheminLogo)) cheminLogo = Path.Combine(baseDir, "..", "..", "Resources", "logo.jpeg");
                    if (!File.Exists(cheminLogo)) cheminLogo = Path.Combine(baseDir, "..", "..", "Resources", "logo.png");

                    if (File.Exists(cheminLogo))
                    {
                        iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(cheminLogo);
                        logo.ScaleToFit(60f, 60f); 
                        logo.Alignment = iTextElement.ALIGN_LEFT;
                        cellGauche.AddElement(logo);
                    }
                }
                catch (Exception) { /* Évite de faire planter l'application */ }

                cellGauche.AddElement(new iTextParagraph("El-Kharis Pressing", fontEntreprise));

                iTextParagraph pSlog = new iTextParagraph("\"L'excellence au service de votre éclat\"", fontSlogan);
                pSlog.SpacingAfter = 3f;
                cellGauche.AddElement(pSlog);

                cellGauche.AddElement(new iTextParagraph("Situé à Yaoundé, Quartier Obobogo", fontInfosEntreprise));
                cellGauche.AddElement(new iTextParagraph("Ouvert du Lundi au Vendredi : 08h00 - 18h00", fontInfosEntreprise));
                cellGauche.AddElement(new iTextParagraph("Contact : +237 677 28 67 12 / +237 656 53 04 84", fontInfosEntreprise));
                tableHeader.AddCell(cellGauche);

                // --- PARTIE DROITE : NUMÉRO, DATE, ÉMIS PAR ---
                iTextCell cellDroite = new iTextCell();
                cellDroite.Border = iTextCell.NO_BORDER;
                cellDroite.VerticalAlignment = iTextElement.ALIGN_TOP;
                cellDroite.HorizontalAlignment = iTextElement.ALIGN_RIGHT;

                // Facture N° / Reçu N°
                string libelleDoc = typeDocument.ToUpper() == "FACTURE" ? "Facture N°" : "Reçu N°";
                string numeroDoc = commandeRow != null ? commandeRow["numero_commande"]?.ToString() ?? idCommande.ToString() : idCommande.ToString();
                iTextParagraph pTitre = new iTextParagraph($"{libelleDoc} : {numeroDoc}", fontTitreDoc);
                pTitre.Alignment = iTextElement.ALIGN_RIGHT;
                pTitre.SpacingAfter = 4f;
                cellDroite.AddElement(pTitre);

                // Date de dépôt
                string dateAffichage = commandeRow != null && commandeRow["date_commande"] != DBNull.Value
                    ? Convert.ToDateTime(commandeRow["date_commande"]).ToString("dd/MM/yyyy à HH:mm")
                    : DateTime.Now.ToString("dd/MM/yyyy à HH:mm");
                iTextParagraph pDate = new iTextParagraph($"Date dépôt : {dateAffichage}", fontNormal);
                pDate.Alignment = iTextElement.ALIGN_RIGHT;
                pDate.SpacingAfter = 4f;
                cellDroite.AddElement(pDate);

                tableHeader.AddCell(cellDroite);
                doc.Add(tableHeader);

                // Ligne de séparation sous l'en-tête complet
                doc.Add(new iTextParagraph("\n"));

                
                //BLOC CLIENT (Juste en dessous : Gauche = Infos Client / Droite = ID & Tél)
                if (commandeRow != null)
                {
                    iTextTable tableClientBloc = new iTextTable(2);
                    tableClientBloc.WidthPercentage = 100;
                    tableClientBloc.SetWidths(new float[] { 1.2f, 1.2f });

                    //CLIENT GAUCHE
                    iTextCell cellClientG = new iTextCell();
                    cellClientG.Border = iTextCell.NO_BORDER;
                    cellClientG.VerticalAlignment = iTextElement.ALIGN_TOP;

                    cellClientG.AddElement(new iTextParagraph("FACTURE POUR :", fontSectionTitre));
                    cellClientG.AddElement(new iTextParagraph($"{commandeRow["nom"]}", fontClientNom));

                    string emailClient = commandeRow["email_client"] != DBNull.Value && !string.IsNullOrEmpty(commandeRow["email_client"].ToString())
                        ? commandeRow["email_client"].ToString()! : "N/A";
                    cellClientG.AddElement(new iTextParagraph($"Email : {emailClient}", fontNormal));
                    tableClientBloc.AddCell(cellClientG);

                    //CLIENT DROITE (Coordonnées Téléphone & ID) 
                    iTextCell cellClientD = new iTextCell();
                    cellClientD.Border = iTextCell.NO_BORDER;
                    cellClientD.VerticalAlignment = iTextElement.ALIGN_TOP;
                    cellClientD.HorizontalAlignment = iTextElement.ALIGN_RIGHT;

                    iTextParagraph pIdClient = new iTextParagraph($"ID Client : #{commandeRow["id_client"]}", fontBold);
                    pIdClient.Alignment = iTextElement.ALIGN_RIGHT;
                    pIdClient.SpacingAfter = 2f;
                    cellClientD.AddElement(pIdClient);

                    iTextParagraph pTelClient = new iTextParagraph($"Téléphone : {commandeRow["telephone"]}", fontNormal);
                    pTelClient.Alignment = iTextElement.ALIGN_RIGHT;
                    cellClientD.AddElement(pTelClient);
                    tableClientBloc.AddCell(cellClientD);

                    doc.Add(tableClientBloc);
                    doc.Add(new iTextParagraph("\n"));

                   
                    // TABLEAU DES ARTICLES
                    iTextTable tableArticles = new iTextTable(4);
                    tableArticles.WidthPercentage = 100;
                    tableArticles.SetWidths(new float[] { 2.8f, 1.2f, 1f, 1.3f });

                    string[] headers = { "Désignation & Service", "Prix Unitaire", "Qté", "Montant Total" };
                    foreach (string header in headers)
                    {
                        iTextCell hCell = new iTextCell(new iTextPhrase(header, fontBold));
                        hCell.BorderColor = couleurBordureLight;
                        hCell.Border = iTextCell.BOTTOM_BORDER;
                        hCell.PaddingTop = 6;
                        hCell.PaddingBottom = 8;
                        hCell.BackgroundColor = couleurFondGris;
                        hCell.HorizontalAlignment = (header.Contains("Désignation")) ? iTextElement.ALIGN_LEFT : iTextElement.ALIGN_RIGHT;
                        tableArticles.AddCell(hCell);
                    }

                    DataTable dtDetails = RecupererDetailsCommande(idCommande);
                    decimal totalArticlesHT = 0;

                    foreach (DataRow detail in dtDetails.Rows)
                    {
                        decimal pu = Convert.ToDecimal(detail["prix_unitaire"]);
                        int qte = Convert.ToInt32(detail["quantite"]);
                        decimal totalLigne = pu * qte;
                        totalArticlesHT += totalLigne;

                        string designationComplete = $"{detail["nom_article"]} ({detail["nom_service"]})";
                        iTextCell cName = new iTextCell(new iTextPhrase(designationComplete, fontNormal));
                        cName.Border = iTextCell.BOTTOM_BORDER;
                        cName.BorderColor = couleurBordureLight;
                        cName.Padding = 8;
                        tableArticles.AddCell(cName);

                        iTextCell cPrice = new iTextCell(new iTextPhrase($"{pu.ToString("N0")} F", fontNormal));
                        cPrice.Border = iTextCell.BOTTOM_BORDER;
                        cPrice.BorderColor = couleurBordureLight;
                        cPrice.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                        cPrice.Padding = 8;
                        tableArticles.AddCell(cPrice);

                        iTextCell cQty = new iTextCell(new iTextPhrase(qte.ToString(), fontNormal));
                        cQty.Border = iTextCell.BOTTOM_BORDER;
                        cQty.BorderColor = couleurBordureLight;
                        cQty.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                        cQty.Padding = 8;
                        tableArticles.AddCell(cQty);

                        iTextCell cAmount = new iTextCell(new iTextPhrase($"{totalLigne.ToString("N0")} F", fontNormal));
                        cAmount.Border = iTextCell.BOTTOM_BORDER;
                        cAmount.BorderColor = couleurBordureLight;
                        cAmount.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                        cAmount.Padding = 8;
                        tableArticles.AddCell(cAmount);
                    }
                    doc.Add(tableArticles);

                    
                    // SYNTHÈSE FINANCIÈRE
                    iTextTable tableSummary = new iTextTable(2);
                    tableSummary.WidthPercentage = 100;
                    tableSummary.SetWidths(new float[] { 3.5f, 1.5f });

                    AjouterCelluleSynthese(tableSummary, "Total Articles :", fontNormal, iTextElement.ALIGN_RIGHT, couleurBordureLight);
                    AjouterCelluleSynthese(tableSummary, $"{totalArticlesHT.ToString("N0")} F", fontNormal, iTextElement.ALIGN_RIGHT, couleurBordureLight);

                    decimal reduction = commandeRow["reduction"] != DBNull.Value ? Convert.ToDecimal(commandeRow["reduction"]) : 0;
                    AjouterCelluleSynthese(tableSummary, "Réduction / Remise :", fontNormal, iTextElement.ALIGN_RIGHT, couleurBordureLight);
                    AjouterCelluleSynthese(tableSummary, $"- {reduction.ToString("N0")} F", fontNormal, iTextElement.ALIGN_RIGHT, couleurBordureLight);

                    decimal totalTTC = Convert.ToDecimal(commandeRow["montant_total"]);
                    AjouterCelluleSynthese(tableSummary, "Net à Payer :", fontBold, iTextElement.ALIGN_RIGHT, couleurBordureLight);
                    AjouterCelluleSynthese(tableSummary, $"{totalTTC.ToString("N0")} F", fontBold, iTextElement.ALIGN_RIGHT, couleurBordureLight);

                    decimal avance = commandeRow["avance"] != DBNull.Value ? Convert.ToDecimal(commandeRow["avance"]) : 0;
                    AjouterCelluleSynthese(tableSummary, "Montant Versé (Avance) :", fontNormal, iTextElement.ALIGN_RIGHT, couleurBordureLight);
                    AjouterCelluleSynthese(tableSummary, $"{avance.ToString("N0")} F", fontNormal, iTextElement.ALIGN_RIGHT, couleurBordureLight);

                    decimal reste = totalTTC - reduction - avance;
                    if (reste < 0) reste = 0;

                    iTextCell cellTotalLbl = new iTextCell(new iTextPhrase("RESTE À PAYER :", fontBold));
                    cellTotalLbl.BackgroundColor = couleurFondGris;
                    cellTotalLbl.Border = iTextCell.NO_BORDER;
                    cellTotalLbl.Padding = 10;
                    cellTotalLbl.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                    tableSummary.AddCell(cellTotalLbl);

                    iTextCell cellTotalVal = new iTextCell(new iTextPhrase($"{reste.ToString("N0")} F", fontBold));
                    cellTotalVal.BackgroundColor = couleurFondGris;
                    cellTotalVal.Border = iTextCell.NO_BORDER;
                    cellTotalVal.Padding = 10;
                    cellTotalVal.HorizontalAlignment = iTextElement.ALIGN_RIGHT;
                    tableSummary.AddCell(cellTotalVal);

                    doc.Add(tableSummary);
                    doc.Add(new iTextParagraph("\n"));

                    
                    //ZONE BAS DE PAGE & CONDITIONS
                    iTextTable tableFooterBlock = new iTextTable(2);
                    tableFooterBlock.WidthPercentage = 100;
                    tableFooterBlock.SetWidths(new float[] { 1.5f, 1f });

                    iTextCell cellConditions = new iTextCell();
                    cellConditions.Border = iTextCell.NO_BORDER;
                    iTextFont fontBordureGris = new iTextFont(bf, 10f, iTextFont.NORMAL, grisTexte);

                    string dateRetraitPrevue = commandeRow["date_livraison_prevue"] != DBNull.Value
                        ? Convert.ToDateTime(commandeRow["date_livraison_prevue"]).ToString("dd/MM/yyyy")
                        : "À définir";

                    cellConditions.AddElement(new iTextParagraph($"Date de retrait prévue : {dateRetraitPrevue}", fontBold));
                    cellConditions.AddElement(new iTextParagraph("Mode de règlement : Espèces / Mobile Money", fontBordureGris));
                    cellConditions.AddElement(new iTextParagraph("Tout vêtement non retiré après 3 mois sera donné à des œuvres caritatives.", fontBordureGris));
                    cellConditions.AddElement(new iTextParagraph("Merci pour votre confiance !", fontBold));
                    tableFooterBlock.AddCell(cellConditions);

                    iTextCell cellCachet = new iTextCell();
                    cellCachet.Border = iTextCell.NO_BORDER;
                    cellCachet.HorizontalAlignment = iTextElement.ALIGN_RIGHT;

                    iTextParagraph pDir = new iTextParagraph("LE RÉCEPTIONNISTE", fontBold);
                    pDir.Alignment = iTextElement.ALIGN_RIGHT;
                    cellCachet.AddElement(pDir);

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
                try
                {
                    conn.Open();
                    string query = @"SELECT c.*, 
                                            CONCAT(cl.nom, ' ', IFNULL(cl.prenom, '')) AS nom, 
                                            cl.telephone, cl.email AS email_client
                                     FROM commandes c 
                                     INNER JOIN clients cl ON c.id_client = cl.id_client 
                                     WHERE c.id_commande = @id";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idCommande);
                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur récupération commande : {ex.Message}");
                    return null;
                }
            }
        }

        private static DataTable RecupererDetailsCommande(int idCommande)
        {
            DataTable dt = new DataTable();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = @"SELECT dc.*, a.nom_article, s.nom_service
                                     FROM detail_commandes dc
                                     INNER JOIN articles a ON dc.id_article = a.id_article
                                     INNER JOIN services s ON dc.id = s.id
                                     WHERE dc.id_commande = @id";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idCommande);
                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur récupération lignes détails : {ex.Message}");
                }
            }
            return dt;
        }
    }
}