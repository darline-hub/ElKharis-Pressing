using ElKharis.Database;
using ElKharis.Models;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace ElKharis.Services
{
    public class ArticleRepository
    {
        public List<ArticleModel> GetAll()
        {
            List<ArticleModel> articles = new List<ArticleModel>();

            using (MySqlConnection? conn = DbConnection.GetConnection())
            {
                if (conn == null)
                    return articles;

                string query = "SELECT id_article, nom_article, montant, description FROM articles";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        articles.Add(new ArticleModel
                        {
                            IdArticle = reader.GetInt32("id_article"),
                            NomArticle = reader.GetString("nom_article"),
                            Montant = reader.GetDecimal("montant"),
                            Description = reader["description"]?.ToString() ?? string.Empty
                        });
                    }
                }
            }

            return articles;
        }

        public void Add(ArticleModel article)
        {
            using (MySqlConnection? conn = DbConnection.GetConnection())
            {
                if (conn == null)
                    return;

                string query = @"INSERT INTO articles(nom_article, montant, description)
                                 VALUES(@nom, @montant, @description)";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@nom", article.NomArticle);
                    cmd.Parameters.AddWithValue("@montant", article.Montant);
                    cmd.Parameters.AddWithValue("@description", article.Description);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(ArticleModel article)
        {
            using (MySqlConnection? conn = DbConnection.GetConnection())
            {
                if (conn == null)
                    return;

                string query = @"UPDATE articles
                                 SET nom_article = @nom,
                                     montant = @montant,
                                     description = @description
                                 WHERE id_article = @id";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", article.IdArticle);
                    cmd.Parameters.AddWithValue("@nom", article.NomArticle);
                    cmd.Parameters.AddWithValue("@montant", article.Montant);
                    cmd.Parameters.AddWithValue("@description", article.Description);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int idArticle)
        {
            using (MySqlConnection? conn = DbConnection.GetConnection())
            {
                if (conn == null)
                    return;

                string query = "DELETE FROM articles WHERE id_article = @id";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", idArticle);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}