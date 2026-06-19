using MySql.Data.MySqlClient;
using ElKharis.Database;
using ElKharis.Models;
using System.Collections.Generic;

namespace ElKharis.Services
{
    internal class ServiceRepository
    {
        public List<ServiceModel> GetAll()
        {
            List<ServiceModel> services = new List<ServiceModel>();

            using (MySqlConnection? conn = DbConnection.GetConnection())
            {
                if (conn == null)
                    return services;

                string query = "SELECT id, nom_service, coefficient_prix FROM services";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        services.Add(new ServiceModel
                        {
                            Id = reader.GetInt32("id"),
                            NomService = reader.GetString("nom_service"),
                            CoefficientPrix = reader.GetDecimal("coefficient_prix")
                        });
                    }
                }
            }

            return services;
        }

        public void Add(ServiceModel service)
        {
            using (MySqlConnection? conn = DbConnection.GetConnection())
            {
                if (conn == null)
                    return;

                string query = @"INSERT INTO services(nom_service, coefficient_prix)
                                 VALUES(@nom, @coefficient)";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@nom", service.NomService);
                    cmd.Parameters.AddWithValue("@coefficient", service.CoefficientPrix);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(ServiceModel service)
        {
            using (MySqlConnection? conn = DbConnection.GetConnection())
            {
                if (conn == null)
                    return;

                string query = @"UPDATE services 
                                 SET nom_service = @nom, coefficient_prix = @coefficient
                                 WHERE id = @id";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", service.Id);
                    cmd.Parameters.AddWithValue("@nom", service.NomService);
                    cmd.Parameters.AddWithValue("@coefficient", service.CoefficientPrix);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (MySqlConnection? conn = DbConnection.GetConnection())
            {
                if (conn == null)
                    return;

                string query = "DELETE FROM services WHERE id = @id";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}