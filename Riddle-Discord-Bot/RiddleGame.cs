

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Riddle_Discord_Bot
{
    public class Riddle
    {
        public string? Answer { get; set; }
        public string? Text { get; set; }
    }

    public class RiddleGame : IRiddleGame
    {
        public string? CorrectAnswer { get; set; }
        public string? RiddleText { get; set; }
        public List<Riddle>? riddles;

        public bool Guess(string input)
        {
            // This is for future me
            //var res = CorrectAnswer == null ? false : CorrectAnswer == input;
            //return res;
            if (CorrectAnswer == null) { return false; }
            if (CorrectAnswer.ToLower() == input) { return true; }
            return false;
        }

        public void NextRiddle()
        {
            if (riddles != null)
            {
                if (riddles.Count > 0)
                {
                    CorrectAnswer = riddles.First().Answer;
                    RiddleText = riddles.First().Text;
                    riddles.RemoveAt(0);
                }
            }
        }

        public void StartGame()
        {
            var json = File.ReadAllText("riddles.json");
            riddles = JsonSerializer.Deserialize<List<Riddle>>(json);
            NextRiddle();
        }

        public void StopGame()
        {
            CorrectAnswer = null;
            RiddleText = null;
        }
    }
}
