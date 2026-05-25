using Microsoft.Data.SqlClient;
using Server_vodenko.Application.Interfaces;
using Server_vodenko.Domain;
using Server_vodenko.Infrastructure.BackgroundServices;

namespace Server_vodenko.Infrastructure.Repository
{
    public class VodenkoRepository : IVodenkoRepository
    {

        private readonly string _connectionString;
        private readonly PlcConnection _plcConnection;

        public VodenkoRepository(IConfiguration configuration, PlcConnection plcConnection)
        {
            _connectionString = configuration.GetConnectionString("DbConnectionString");
            _plcConnection = plcConnection;
        }

        public Plc GetPlc()
        {
            try
            {
                Plc result = new Plc() { };

                string query = "SELECT _ip, rack, slot, cpu_type from PLC_CONFIGURATION";

                using SqlConnection connection = new SqlConnection(_connectionString);
                using SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                using SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string ip = reader.GetString(reader.GetOrdinal("_ip"));
                    int rack = reader.GetInt32(reader.GetOrdinal("rack"));
                    int slot = reader.GetInt32(reader.GetOrdinal("slot"));
                    string cpu_type = reader.GetString(reader.GetOrdinal("cpu_type"));

                    result = new Plc(cpu_type, ip, rack, slot);

                }

                connection.Close();
                return result;

            }
            catch
            {
                throw new Exception("No PLC configuration found in database.");
            }


        }

    }
}
