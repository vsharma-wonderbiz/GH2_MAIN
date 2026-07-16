using System.Text.Json;
using System.Text.Json.Nodes;

namespace Infrastructure.Services
{
    public class ModbusStateUpdater
    {
        private readonly string _stateFilePath;

        // Thread-safety: agar multiple stacks parallel mein khatam ho jaayein
        // aur ek saath file update karne ki koshish karein
        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        public ModbusStateUpdater(string stateFilePath)
        {
            if (string.IsNullOrWhiteSpace(stateFilePath))
                throw new ArgumentException("State file path cannot be empty.", nameof(stateFilePath));

            _stateFilePath = stateFilePath;
        }

        /// <summary>
        /// Backfill khatam hone ke baad, us stack ka final lifetime aur h2flow
        /// value state.json mein update kar deta hai (ya naya entry bana deta hai
        /// agar stack ka entry exist nahi karta) - taaki Modbus service jab start
        /// ho, isi state se aage continue kare.
        /// </summary>
        public async Task UpdateStackFinalStateAsync(
            string stackName,
            float finalLifetimeValue,
            float finalH2FlowValue)
        {
            await _fileLock.WaitAsync();

            try
            {
                JsonObject root = await LoadOrCreateRootAsync();

                if (root["states"] is not JsonArray states)
                {
                    states = new JsonArray();
                    root["states"] = states;
                }

                bool stackFound = false;

                // Step 1: Dhundo ki is stack ka entry already exist karta hai kya
                foreach (var state in states)
                {
                    if (state is not JsonObject stateObj)
                        continue;

                    var stack = stateObj["stack"]?.ToString();

                    if (!string.Equals(stack, stackName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Mil gaya - dono values isi object mein update kar do
                    stateObj["remainingLifetime"] = finalLifetimeValue;
                    stateObj["currentH2Flow"] = finalH2FlowValue;
                    stackFound = true;
                    break;
                }

                // Step 2: Agar stack ka entry exist hi nahi karta, naya bana ke add karo
                if (!stackFound)
                {
                    Console.WriteLine($"[ModbusStateUpdater] '{stackName}' not found in state.json - creating a new entry.");

                    var newStackEntry = new JsonObject
                    {
                        ["stack"] = stackName,
                        ["remainingLifetime"] = finalLifetimeValue,
                        ["currentH2Flow"] = finalH2FlowValue
                    };

                    states.Add(newStackEntry);
                }

                await SaveRootAsync(root);

                Console.WriteLine($"[ModbusStateUpdater] state.json updated for '{stackName}': " +
                                   $"lifetime={finalLifetimeValue:F2}, h2flow={finalH2FlowValue:F3}");
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// File se JSON padhta hai. Agar file exist nahi karti, ya khaali hai,
        /// ya corrupt/invalid JSON hai - crash karne ki jagah naya base
        /// structure ({ states: [], archived_states: [] }) return karta hai.
        /// </summary>
        private async Task<JsonObject> LoadOrCreateRootAsync()
        {
            if (!File.Exists(_stateFilePath))
            {
                Console.WriteLine($"[ModbusStateUpdater] state.json not found at '{_stateFilePath}' - creating a new base structure.");
                return CreateEmptyRoot();
            }

            string json = await File.ReadAllTextAsync(_stateFilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"[ModbusStateUpdater] state.json is empty at '{_stateFilePath}' - creating a new base structure.");
                return CreateEmptyRoot();
            }

            try
            {
                var parsed = JsonNode.Parse(json)?.AsObject();

                if (parsed == null)
                {
                    Console.WriteLine("[ModbusStateUpdater] state.json parsed to null - creating a new base structure.");
                    return CreateEmptyRoot();
                }

                return parsed;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[ModbusStateUpdater] state.json is invalid/corrupt ({ex.Message}) - creating a new base structure.");
                return CreateEmptyRoot();
            }
        }

        private static JsonObject CreateEmptyRoot()
        {
            return new JsonObject
            {
                ["states"] = new JsonArray(),
                ["archived_states"] = new JsonArray()
            };
        }

        private async Task SaveRootAsync(JsonObject root)
        {
            var directory = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string updatedJson = root.ToJsonString(options);

            await File.WriteAllTextAsync(_stateFilePath, updatedJson);
        }
    }
}