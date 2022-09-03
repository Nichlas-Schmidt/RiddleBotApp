

using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Riddle_Discord_Bot
{
    public record Riddle(string? Answer, string? Text);

    public record Riddlewinner(string Mention, int Points);

    public class RiddleGame : IRiddleGame
    {
        public Riddle? ActiveRiddle
        {
            get
            {
                return active_riddle;
            }
        }


        public List<Riddle>? riddles;
        private List<Riddle>? used_riddles;
        private List<Riddlewinner>? leaderboard;
        List<Riddle> filtered_riddles;
        private Riddle? active_riddle;
        private readonly ILogger<RiddleGame> _log;
        private readonly JsonSerializerOptions standardJsonOpt = new() { WriteIndented = true };

        public RiddleGame(ILogger<RiddleGame> log)
        {
            _log = log;
            filtered_riddles = new List<Riddle>();
        }

        // Startup and helper methods
        public void Startup()
        {
            if (!LoadAllRiddles()) { riddles = new List<Riddle>(); }
            if (!LoadAllUsedRiddles()) { used_riddles = new List<Riddle>(); }
            if (!LoadLeaderboard()) { leaderboard = new List<Riddlewinner>(); }
            if (!LoadActiveRiddle()) { active_riddle = null; }
        }

        public string LoadJsonFile(string file_name)
        {
            var json = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, file_name));
            return json;
        }

        public void WriteToJson(string file_name, string content)
        {
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, file_name), content);
        }

        public bool LoadActiveRiddle()
        {
            var json = LoadJsonFile("active_riddle.json");
            active_riddle = JsonSerializer.Deserialize<Riddle>(json);
            if (active_riddle != null)
            {
                return true;
            }
            return false;
        }

        public bool LoadAllRiddles()
        {
            var json = LoadJsonFile("riddles.json");
            riddles = JsonSerializer.Deserialize<List<Riddle>>(json);
            if (riddles != null) { return true; }
            return false;
        }

        public bool LoadAllUsedRiddles()
        {
            var json = LoadJsonFile("used_riddles.json");
            used_riddles = JsonSerializer.Deserialize<List<Riddle>>(json);
            if (used_riddles != null) { return true; }
            return false;
        }

        public bool LoadLeaderboard()
        {
            var json = LoadJsonFile("leaderboard.json");
            leaderboard = JsonSerializer.Deserialize<List<Riddlewinner>>(json);
            if (leaderboard != null) { return true; }
            return false;
        }


        public void SyncActive()
        {
            var json = JsonSerializer.Serialize<Riddle>(active_riddle ?? new Riddle(null, null), options: standardJsonOpt);
            WriteToJson("active_riddle.json", json);
            LoadActiveRiddle();
        }

        public void SyncUsed()
        {
            var json = JsonSerializer.Serialize<List<Riddle>>(used_riddles ?? new List<Riddle>(), options: standardJsonOpt);
            WriteToJson("used_riddles.json", json);
            LoadAllUsedRiddles();
        }


        public void MoveActiveToUsed()
        {
            if (active_riddle != null)
            {
                used_riddles?.Add(active_riddle);
            }
            active_riddle = null;
            SyncActive();
            SyncUsed();
        }

        public bool SetNextActive()
        {
            if (riddles != null && used_riddles != null) { filtered_riddles = riddles.Except(used_riddles).ToList(); }

            if (filtered_riddles.Count > 0)
            {
                active_riddle = filtered_riddles.First();
                SyncActive();
                return true;
            }
            else
            {
                active_riddle = null;
                return false;
            }
        }

        public bool Guess(string input)
        {
            if (active_riddle != null)
            {
                if (active_riddle.Answer?.ToUpper() == input.ToUpper()) { return true; }
            }
            return false;
        }

        public bool StartGame()
        {
           return SetNextActive();
        }

        public void StopGame()
        {
            active_riddle = null;
        }

        public void ReloadRiddles()
        {
            var json = LoadJsonFile("riddles.json");
            riddles = JsonSerializer.Deserialize<List<Riddle>>(json);
            var used = LoadJsonFile("used_riddles.json");
            used_riddles = JsonSerializer.Deserialize<List<Riddle>>(used);
        }

        public int Remaining()
        {
            if (active_riddle == null) { return 0; }
            return filtered_riddles.Count;
        }

        public void ClearUsedRiddles()
        {
            if (used_riddles != null && riddles != null)
            {
                foreach (Riddle r in used_riddles)
                {
                    riddles.Add(r);
                }
                string json = "[\r\n  {\r\n    \"Text\": \"\",\r\n    \"Answer\": \"\"\r\n  }\r\n]";
                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "used_riddles.json"), json);
                var used = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "used_riddles.json"));
                used_riddles = JsonSerializer.Deserialize<List<Riddle>>(used);
            }
        }

        public List<Riddlewinner> LeaderboardRead()
        {
            _log.LogInformation("Attempting to read leaderboard");
            try
            {
                LoadLeaderboard();
                if (leaderboard != null)
                {
                    leaderboard = leaderboard.OrderByDescending(o => o.Points).ToList();
                    return leaderboard.Take(10).ToList();
                }
                return new List<Riddlewinner>();
            }
            catch (Exception e)
            {
                Console.WriteLine("Fail on read");
                Console.WriteLine(e.Message);
                return new List<Riddlewinner>();
            }
        }

        public void LeaderboardWrite(string winner)
        {
            _log.LogInformation("Attempting to write leaderboard");
            if (leaderboard == null) { LeaderboardRead(); }
            if (leaderboard != null)
            {
                _log.LogInformation("Lock");
                try
                {
                    _log.LogInformation("Try");
                    if (leaderboard.Where(m => m.Mention == winner).ToList().Count > 0)
                    {
                        var index = leaderboard.FindIndex(o => o.Mention == winner);
                        var win = leaderboard[index];
                        leaderboard[index] = new Riddlewinner(win.Mention, win.Points + 1);
                    }
                    else
                    {
                        leaderboard.Add(new Riddlewinner(winner, 1));
                    }
                    var jsonstring = JsonSerializer.Serialize<List<Riddlewinner>>(leaderboard, options: new JsonSerializerOptions { WriteIndented = true });
                    WriteToJson("leaderboard.json", jsonstring);
                }
                catch (Exception e)
                {
                    _log.LogError("catch {}",e.Message);
                }
            }
        }

    }
}
