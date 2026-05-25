using Microsoft.Data.SqlClient;
using Server_vodenko.Application.DTOs;
using Server_vodenko.Application.Interfaces;
using Server_vodenko.Domain;

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
        
        public async Task<List<Alarms>> GetAlarmsAsync(int minutes)
        {
            var result = new List<Alarms>();

            string query = @"SELECT Setpoint_Invalid, Manual_Valve_Invalid, Tank_Overflow, Time_Saved
                             FROM ALARMS
                             WHERE Time_Saved >= DATEADD(MINUTE, @minutes, GETDATE())
                             ORDER BY Time_Saved DESC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@minutes", -Math.Abs(minutes)); 

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync(); 

            while (await reader.ReadAsync())
            {
                result.Add(new Alarms(
                    reader.GetBoolean(reader.GetOrdinal("Setpoint_Invalid")),
                    reader.GetBoolean(reader.GetOrdinal("Manual_Valve_Invalid")),
                    reader.GetBoolean(reader.GetOrdinal("Tank_Overflow")),
                    reader.GetDateTime(reader.GetOrdinal("Time_Saved"))
                ));
            }

            return result;

        }
        
        public async Task<L2ToPlc> GetControlRowAsync()
        {
            string query = @"SELECT TOP 1 Level_Setpoint, Manual_Valve_Value,
                                    Automatic_Manual, Start_Pump, Reset
                             FROM L2_TO_PLC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new L2ToPlc(
                    reader.IsDBNull(reader.GetOrdinal("Level_Setpoint")) ? null : reader.GetFloat(reader.GetOrdinal("Level_Setpoint")),
                    reader.IsDBNull(reader.GetOrdinal("Manual_Valve_Value")) ? null : reader.GetFloat(reader.GetOrdinal("Manual_Valve_Value")),
                    reader.IsDBNull(reader.GetOrdinal("Automatic_Manual")) ? null : reader.GetBoolean(reader.GetOrdinal("Automatic_Manual")),
                    reader.IsDBNull(reader.GetOrdinal("Start_Pump")) ? null : reader.GetBoolean(reader.GetOrdinal("Start_Pump")),
                    reader.IsDBNull(reader.GetOrdinal("Reset")) ? null : reader.GetBoolean(reader.GetOrdinal("Reset"))
                );
            }

            return new L2ToPlc(null, null, null, null, null);
        }
        
        public async Task<List<Vodenko>> GetTrendsAsync(int minutes)
        {
            var result = new List<Vodenko>();

            string query = @"SELECT Actual_Level, Valve_Position, Time_Saved
                             FROM PLC_TO_L2
                             WHERE Time_Saved >= DATEADD(MINUTE, @minutes, GETDATE())
                             ORDER BY Time_Saved ASC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@minutes", -Math.Abs(minutes));

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new Vodenko(
                    reader.GetFloat(reader.GetOrdinal("Actual_Level")),
                    reader.GetFloat(reader.GetOrdinal("Valve_Position")),
                    reader.GetDateTime(reader.GetOrdinal("Time_Saved"))
                ));
            }

            return result;
        }
        
        public async Task SetResetPulseAsync()
        {
            string query = @"UPDATE L2_TO_PLC
                             SET Reset = 1";
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
            _plcConnection.WriteBool("DB102,X8.2", true);
             await Task.Delay(1000); 
            _plcConnection.WriteBool("DB102,X8.2", false);
        }

        public async Task UpdateControlAsync(L2ToPlcDto dto)
        {
            string query = @"UPDATE L2_TO_PLC
                             SET Level_Setpoint     = @Level_Setpoint,
                                 Manual_Valve_Value = @Manual_Valve_Value,
                                 Automatic_Manual   = @Automatic_Manual,
                                 Start_Pump         = @Start_Pump";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Level_Setpoint", (object?)dto.Level_Setpoint ?? DBNull.Value);
            command.Parameters.AddWithValue("@Manual_Valve_Value", (object?)dto.Manual_Valve_Value ?? DBNull.Value);
            command.Parameters.AddWithValue("@Automatic_Manual", (object?)dto.Automatic_Manual ?? DBNull.Value);
            command.Parameters.AddWithValue("@Start_Pump", (object?)dto.Start_Pump ?? DBNull.Value);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            if (dto.Level_Setpoint.HasValue) _plcConnection.WriteReal("DB102,R0.0", dto.Level_Setpoint.Value);
            if (dto.Manual_Valve_Value.HasValue) _plcConnection.WriteReal("DB102,R4.0", dto.Manual_Valve_Value.Value);
            if (dto.Automatic_Manual.HasValue) _plcConnection.WriteBool("DB102,X8.0", dto.Automatic_Manual.Value);
            if (dto.Start_Pump.HasValue) _plcConnection.WriteBool("DB102,X8.1", dto.Start_Pump.Value);
        }
    }
}
